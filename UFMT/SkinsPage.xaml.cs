#pragma warning disable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UAssetAPI;

namespace UFMT
{
    public sealed partial class SkinsPage : Page, INotifyPropertyChanged
    {
        public static FnVersion CurrentFnVersion = FnVersionsData.FnVersions.GetValueOrDefault(App.Settings.FnVersion);
        public static UeVersion CurrentUeVersion = UeVersionsData.UeVersions.GetValueOrDefault(App.Settings.UeVersion);
        private static string PhysicsImporterPath;
        private string CookedAssetsPath;
        private static string ValidCodenameCharacters = "abcdefghijklmnopqrstuvwxyz1234567890_";
        private CancellationTokenSource _currentSkinPathDebounce;
        private string OutputFnGamePath = string.Empty;
        private bool IsUpdatingFromCode = false;
        private bool IsLoadingDropdowns = false;
        CharacterPart Body;
        CharacterPart Head;
        CharacterPart FaceAcc;
        CharacterPart Hat;
        public event PropertyChangedEventHandler PropertyChanged;

        private SkinData _currentSkin;
        public SkinData CurrentSkin
        {
            get => _currentSkin;
            set
            {
                if (_currentSkin != value)
                {
                    _currentSkin = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentSkin)));
                }
            }
        }

        private bool _allSwizzleCheckBoxValue;
        public bool AllSwizzleCheckBoxValue
        {
            get => _allSwizzleCheckBoxValue;
            set
            {
                if (_allSwizzleCheckBoxValue != value)
                {
                    _allSwizzleCheckBoxValue = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SkinsPage()
        {
            InitializeComponent();
            SkinsPathBox.Text = AppSettings.GetValue("SkinsPath", "");
            CurrentSkinPathBox.Text = AppSettings.GetValue("CurrentSkinPath", "");
            ((FrameworkElement)this.Content).Loaded += (s, e) =>
            {
                LoadContent();
            };
        }

        private void SkinsPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SetValue("SkinsPath", SkinsPathBox.Text);
        }

        private async void CurrentSkinPathBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SetValue("CurrentSkinPath", CurrentSkinPathBox.Text);

            _currentSkinPathDebounce?.Cancel();
            _currentSkinPathDebounce = new CancellationTokenSource();
            var token = _currentSkinPathDebounce.Token;

            if (sender as string != "NoDelay")
            {
                try
                {
                    //Wait 250ms, if the user is still typing, this gets cancelled
                    await Task.Delay(250, token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
            ResetCpData();
            if (!CheckCurrentSkinPathValidation(CurrentSkinPathBox.Text)) return;

            OutputFnGamePath = Path.Combine(CurrentSkin.Path, "Output", App.Settings.FnVersion, "FortniteGame");
            CurrentSkin.Codename = new DirectoryInfo(CurrentSkin.Path).Name;
            CurrentSkin.CID = $"CID_{CurrentSkin.Codename}";


            SkinData loadedJson = LoadSkinConfig(Path.Combine(CurrentSkin.Path, $"{CurrentSkin.Codename}_Settings.json"));

            if (loadedJson != null)
            {
                CurrentSkin = loadedJson;
                CurrentSkin.Path = CurrentSkinPathBox.Text;
                if (!CheckCurrentSkinPathValidation(CurrentSkinPathBox.Text)) return;
                CurrentSkin.LobbyAnimationFolderPath = Path.Combine(CurrentSkin.SourcePath, "Lobby_Animation");
                foreach (CharacterPart cp in CurrentSkin.CharacterParts)
                {
                    cp.PskPath = Path.Combine(CurrentSkin.MeshesPath, cp.Type, $"{cp.Psk}.psk");
                    cp.PhysicsAssetJsonPaths = cp.PhysicsAssets.Select(phys => Path.Combine(CurrentSkin.PhysicsPath, cp.Type, $"{phys}.json")).ToList();
                }
            }
            else
            {
                List<CharacterPart> characterParts = 
                SkinFolderScanner.FindCharacterParts(CurrentSkin.MeshesPath, CurrentSkin.PhysicsPath, new List<CharacterPart>() { Body, Head, FaceAcc, Hat });
                if (characterParts == null) return;
                CurrentSkin.CharacterParts = characterParts;

                (bool isValid, string lobbyAnimationPsa, string lobbyAnimationJson) = SkinFolderScanner.FindLobbyAnimationFiles(CurrentSkin.LobbyAnimationFolderPath);
                if (!isValid) return;
                CurrentSkin.LobbyAnimationPsa = lobbyAnimationPsa;
                CurrentSkin.LobbyAnimationJson = lobbyAnimationJson;

                List<Material> materials = 
                PskReader.GetMaterialData(CurrentSkin.CharacterParts.Select(cp => cp.PskPath).ToList(), 
                CurrentSkin.CharacterParts, allSwizzleCheckBox.IsChecked.Value, this);

                if (materials == null) return;
                CurrentSkin.Materials = new ObservableCollection<Material>(materials);

                DefaultTextureSetup.CreateDefaultTextures(DefaultTextureSetup.FindMissingDefaultTextures(CurrentSkin.TexturesPath), CurrentSkin.TexturesPath);
                if (CurrentFnVersion.ManuallySwizzleMaterials) TextureSwizzler.SwizzleSpecularTextures(CurrentSkin.TexturesPath);
                (CurrentSkin.LargeIcon, CurrentSkin.SmallIcon) = TextureCategorizer.GetIconTextures(CurrentSkin.TexturesPath);
                CurrentSkin.Textures = TextureCategorizer.GetAllTextures(CurrentSkin.TexturesPath);
                MaterialTextureAssigner.AssignTexturesToAllMaterials(CurrentSkin.TexturesPath, CurrentSkin.Codename, CurrentSkin.Materials);
            }

            characterCIDTextBox.Text = CurrentSkin.CID;
            CurrentSkin.PropertyChanged += (s, e) => SaveSkinConfig();
            UpdateDropdowns();
        }

        private bool CheckCurrentSkinPathValidation(string currentSkinFolderPath)
        {
            if (currentSkinFolderPath == string.Empty)
            {
                Log.Error("The Current skin path is empty!");
                return false;
            }
            if (!Directory.Exists(currentSkinFolderPath))
            {
                Log.Error($"\"{currentSkinFolderPath}\" doesn't exist!");
                return false;
            }
            string sourcePath = Path.Combine(currentSkinFolderPath, "Source");
            if (!Directory.Exists(sourcePath))
            {
                Log.Error($"Cannot find the Source folder inside \"{currentSkinFolderPath}\"");
                return false;
            }
            string meshesPath = Path.Combine(sourcePath, "Meshes");
            if (!Directory.Exists(meshesPath))
            {
                Log.Error($"Cannot find the Meshes folder inside \"{sourcePath}\"");
                return false;
            }
            string texturesPath = Path.Combine(sourcePath, "Textures");
            if (!Directory.Exists(texturesPath))
            {
                Log.Error($"Cannot find the Textures folder inside \"{sourcePath}\"");
                return false;
            }
            string lobbyAnimationFolderPath = Path.Combine(sourcePath, "Lobby_Animation");
            if (!Directory.Exists(lobbyAnimationFolderPath))
            {
                Log.Error($"Cannot find the Lobby_Animation folder inside \"{sourcePath}\"");
                return false;
            }
            string physicsPath = Path.Combine(sourcePath, "Physics");
            if (!Directory.Exists(physicsPath))
            {
                Log.Error($"Cannot find the Physics folder inside \"{sourcePath}\"");
                return false;
            }

            CurrentSkin.Path = currentSkinFolderPath;
            CurrentSkin.SourcePath = sourcePath;
            CurrentSkin.MeshesPath = meshesPath;
            CurrentSkin.TexturesPath = texturesPath;
            CurrentSkin.LobbyAnimationFolderPath = lobbyAnimationFolderPath;
            CurrentSkin.PhysicsPath = physicsPath;
            return true;
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                var picker = new Windows.Storage.Pickers.FolderPicker();
                IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.m_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add("*");
                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    if (button.Name == "SkinsPathBrowse")
                    {
                        SkinsPathBox.Text = folder.Path;
                    }
                    else if (button.Name == "CurrentSkinPathBrowse")
                    {
                        CurrentSkinPathBox.Text = folder.Path;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private async void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(App.Settings.UeVersion))
            {
                Log.Error($"No unreal engine selected! Make sure you selected the correct ue version in setting!");
                return;
            }

            if (string.IsNullOrEmpty(CurrentSkin.Gender))
            {
                Log.Error($"The skin's gender is unspecified");
                return;
            }
            try
            {
                await BlenderPreviewRenderer.RenderSkinPreviewImage(CurrentSkin.CharacterParts, CurrentSkin.Gender, CurrentSkin.LobbyAnimationFolderPath, CurrentSkin.LobbyAnimationPsa,
                CurrentSkin.Materials, CurrentSkin.Path, CurrentSkin.Codename, CurrentSkin.TexturesPath, App.Settings.FnVersion);
                await UpdateSkinPreviewImage(CurrentSkin.SourcePath, CurrentSkin.Codename, CurrentSkin.LargeIcon);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SkinValidator.ValidateBeforeExport(App.Settings.UeVersion, CurrentSkin.Gender, CurrentSkin.Name, CurrentSkin.Description, CurrentSkin.CID)) return;
            if (!await FbxConverter.ConvertPskToFbx(CurrentSkin.CharacterParts, CurrentSkin.SourcePath, CurrentSkin.Codename)) return;

            var (isAnimValid, lobbyAnimationFbx, lobbyAnimationLength) = 
            await FbxConverter.ConvertPsaToFbx(CurrentSkin.SourcePath, CurrentSkin.Codename, CurrentSkin.LobbyAnimationFolderPath, CurrentSkin.LobbyAnimationPsa);
            if (!isAnimValid) return;
            CurrentSkin.LobbyAnimationFbx = lobbyAnimationFbx;
            CurrentSkin.LobbyAnimationLength = lobbyAnimationLength;
            string cookedCodenamePath = Path.Combine(CookedAssetsPath, "CustomSkins", CurrentSkin.Codename);

            UnrealDependencySetup.CreateMissingFiles(App.Settings.UeProjectPath, CurrentSkin.Codename, CurrentUeVersion.BaseHeadPath, cookedCodenamePath, CurrentUeVersion.Name, 
            CurrentUeVersion.BaseHeadFileNames);

            UnrealExportData unrealData = UnrealExportDataCollector.CollectData(CurrentSkin.SmallIcon, CurrentSkin.LargeIcon, CurrentSkin.Materials, CurrentSkin.TexturesPath,
            CurrentFnVersion.ManuallySwizzleMaterials, CurrentSkin.SourcePath, CurrentSkin.LobbyAnimationFbx, CurrentSkin.LobbyAnimationJson, CurrentSkin.CharacterParts,
            CurrentSkin.Gender, CurrentSkin.Codename, CurrentSkin.CID);


            await UnrealProcessRunner.LaunchUnreal(unrealData);
            await UnrealProcessRunner.CookFiles();

            CurrentUeVersion.FixRequiredFiles(Path.Combine
            (cookedCodenamePath, "Animations", $"{CurrentSkin.Codename}_Lobby_Animation.uasset"), CurrentSkin.CharacterParts.Select
            (cp => Path.Combine(cookedCodenamePath, "Meshes", $"{Path.GetFileNameWithoutExtension(cp.FbxPath)}.uasset")).ToArray());

            AssetRegistryBuilder.CreateAssetRegistry(CookedAssetsPath, CurrentUeVersion.Name, CurrentSkin.Path, OutputFnGamePath);
           
            DirectoryInfo cookedCharacterDirectory = new DirectoryInfo(Path.Combine(CookedAssetsPath, "CustomSkins", CurrentSkin.Codename));
            string contentFolderPath = Path.Combine(OutputFnGamePath, "Content", "CustomSkins", CurrentSkin.Codename);

            SkinAssetCreator.CopyFilesFromUe(contentFolderPath, cookedCharacterDirectory, CookedAssetsPath, OutputFnGamePath, CurrentUeVersion.BaseHeadPath, CurrentUeVersion.ReplaceCookedBaseHead, 
            App.Settings.FnVersion, CurrentUeVersion.Name, CurrentUeVersion.BaseHeadFileNames);

            SkinAssetCreator.CreateCharacterParts(contentFolderPath, CurrentSkin.Gender, CurrentSkin.Codename, CurrentSkin.CharacterParts, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            SkinAssetCreator.CreateMaterials(contentFolderPath, CurrentSkin.Codename, CurrentSkin.Materials, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            SkinAssetCreator.CreateHeroSpecialization(contentFolderPath, CurrentSkin.Codename, CurrentSkin.CharacterParts, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            SkinAssetCreator.CreateLobbyAnimationMontage(contentFolderPath, CurrentSkin.Codename, CurrentSkin.LobbyAnimationPsa, CurrentSkin.LobbyAnimationJson,
            CurrentSkin.LobbyAnimationLength, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            SkinAssetCreator.CreateHero(contentFolderPath, CurrentSkin.Codename, CurrentSkin.Gender, CurrentSkin.SmallIcon, CurrentSkin.LargeIcon, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            SkinAssetCreator.CreateCharacter(OutputFnGamePath, CurrentSkin.CID, CurrentSkin.Codename, CurrentSkin.Name, CurrentSkin.Description, CurrentSkin.Rarity,
            CurrentSkin.Series, CurrentFnVersion, CurrentUeVersion.UassetApiEngineVer);

            U4Pak.Pack(OutputFnGamePath, Path.Combine(Path.GetDirectoryName(OutputFnGamePath), $"z_{CurrentSkin.Codename}.pak"));
            Log.Success("\nYour custom skin is ready! Check the output folder");
        }

        private async void CreateSkinFolder_Click
        (object sender, RoutedEventArgs e)
        {
            CreateFolderDialog.XamlRoot = this.Content.XamlRoot;

            if (SkinsPathBox.Text == null || SkinsPathBox.Text == "")
            {
                Log.Error("The skins path cannot be empty!");
                return;
            }
            else if (!Directory.Exists(SkinsPathBox.Text))
            {
                Log.Error($"\"{SkinsPathBox.Text}\" doesn't exist!");
                return;
            }

            CodenameFolderCreateTextBox.Text = "";
            var result = await CreateFolderDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string newName = CodenameFolderCreateTextBox.Text;
                string rootPath = SkinsPathBox.Text;

            }
        }

        private async void CreateFolderDialog_SecondaryButtonClick
        (ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            bool invalidCodename = false;
            if (SkinsPathBox.Text == null || SkinsPathBox.Text == "")
            {
                Log.Error("The skins path cannot be empty!");
                return;
            }
            else if (!Directory.Exists(SkinsPathBox.Text))
            {
                Log.Error($"\"{SkinsPathBox.Text}\" doesn't exist!");
                return;
            }

            if (CodenameFolderCreateTextBox.Text.Length > 30)
            {
                Log.Error("The codename cannot be longer than 30 characters!");
                return;
            }

            foreach (char c in CodenameFolderCreateTextBox.Text)
            {
                if (!ValidCodenameCharacters.Contains(c.ToString().ToLower()))
                {
                    Log.Error("The codename can only contain alphabetical characters, " +
                    "numbers and _");
                    invalidCodename = true;
                    return;
                }
            }

            Directory.CreateDirectory(Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text));
            Log.Success($"Successfully created {CodenameFolderCreateTextBox.Text} folder at " +
            $"{SkinsPathBox.Text}");
            
            string[] cpTypes = {"Body", "Head", "Faceacc", "Hat" };
            string[] cpTypeFolders = {"Meshes", "Physics" };
            foreach (string cpType in cpTypes)
            {
                foreach (string cpTypeFolder in cpTypeFolders)
                {
                    Directory.CreateDirectory(Path.Combine
                    (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", cpTypeFolder, cpType));
                    Log.Success($"Successfully created {cpType} folder at " +
                    $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", cpTypeFolder)}");
                }
            }

            Directory.CreateDirectory(Path.Combine
            (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Textures"));
            Log.Success($"Successfully created Textures folder at " +
            $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source")}");

            Directory.CreateDirectory(Path.Combine
            (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Lobby_Animation"));
            Log.Success($"Successfully created Lobby_Animation folder at " +
            $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source")}");

            Directory.CreateDirectory(Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Fbx"));
            Directory.CreateDirectory(Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Fbx", "Body"));
            Directory.CreateDirectory(Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Fbx", "Head"));

            args.Cancel = false;
        }

        private void AllSwizzleChecked(object sender, RoutedEventArgs e)
        {
            if (IsUpdatingFromCode) return;

            IsUpdatingFromCode = true;
            foreach (Material mat in CurrentSkin.Materials)
            {
                mat.Swizzle = true;
            }
            IsUpdatingFromCode = false;
        }

        private void AllSwizzleUnchecked(object sender, RoutedEventArgs e)
        {
            if (IsUpdatingFromCode) return;

            IsUpdatingFromCode = true;
            foreach (Material mat in CurrentSkin.Materials)
            {
                mat.Swizzle = false;
            }
            IsUpdatingFromCode = false;
        }

        public void UpdateAllSwizzleCheckBoxState()
        {
            if (CurrentSkin.Materials == null || CurrentSkin.Materials.Count == 0) return;
            if (IsUpdatingFromCode) return;

            IsUpdatingFromCode = true;

            bool allChecked = CurrentSkin.Materials.All(m => m.Swizzle);
            AllSwizzleCheckBoxValue = allChecked;

            IsUpdatingFromCode = false;
        }

        private async void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox c = sender as ComboBox;
            if (IsUpdatingFromCode || IsLoadingDropdowns) return;
            if (c.Tag != null)
            {
                if (c.Tag.ToString() == "gender")
                {
                    if (CurrentSkin.Gender != null || c?.SelectedItem == null) return;
                    CurrentSkin.Gender = c.SelectedItem.ToString();
                }

                if (App.Settings.FnVersion == "8.51-9.10" || App.Settings.FnVersion == "9.41")
                {
                    if (c.Tag.ToString() == "series")
                    {
                        string fullPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Icon_Background.png";

                        if (c.SelectedItem.ToString() == "None")
                        {
                            fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}.png";
                            iconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));

                            fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}_Icon_Overlay.png";
                            RarityIconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));

                            fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Text.png";
                        }

                        else
                        {
                            fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Series}.png";
                            iconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));

                            fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Series}_Icon_Overlay.png";
                            RarityIconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));


                            fullPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Text.png";
                        }

                    }
                    else if (c?.Tag.ToString() == "rarity" && c?.SelectedItem != null && seriesComboBox?.SelectedItem != null)
                    {
                        if (CurrentSkin.Series != "None") return;
                        string fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}.png";
                        iconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));

                        fullPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}_Icon_Overlay.png";
                        RarityIconOverlayCh1.Source = new BitmapImage(new Uri(fullPath));

                        fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Text.png";
                    }
                }
                else
                {
                    Ch1PreviewViewBox.Visibility = Visibility.Collapsed;
                    Ch2PreviewViewBox.Visibility = Visibility.Visible;
                    if (c.Tag.ToString() == "series")
                    {
                        string fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Series}_Icon_Background.png";
                        iconBackgroundOverlay.Source = new BitmapImage(new Uri(fullPath));

                        if (c.SelectedItem.ToString() == "None")
                        {
                            fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Icon.png";
                            iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                            fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Text.png";
                            textOverlay.Source = new BitmapImage(new Uri(fullPath));
                        }

                        else
                        {
                            fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Series}_Icon.png";
                            iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                            fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Series}_Text.png";
                            textOverlay.Source = new BitmapImage(new Uri(fullPath));
                        }

                    }
                    else if (c?.Tag.ToString() == "rarity" && c?.SelectedItem != null && seriesComboBox?.SelectedItem != null)
                    {
                        if (CurrentSkin.Series != "None") return;
                        string fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Icon.png";
                        iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                        fullPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Text.png";
                        textOverlay.Source = new BitmapImage(new Uri(fullPath));
                    }
                }
            }
        }

        private async void CharacterTextBoxChanged(object sender, RoutedEventArgs e)
        {
            var c = sender as TextBox;
            if (c.Tag.ToString() == "characterName")
            {
                CurrentSkin.Name = c.Text;
                characterNameText.Text = CurrentSkin.Name.ToUpper();
                characterNameTextCh1.Text = CurrentSkin.Name.ToUpper();
            }
            else if (c.Tag.ToString() == "characterDescription")
            {
                CurrentSkin.Description = c.Text;
            }
            else if (c.Tag.ToString() == "characterCID")
            {
                CurrentSkin.CID = c.Text;
            }
        }

        private void UpdateDropdowns()
        {
            try
            {
                IsLoadingDropdowns = true;

                foreach (Material mat in CurrentSkin.Materials)
                {
                    mat.TextureOptions = CurrentSkin.Textures;
                }

                DynamicExpanderList.ItemsSource = CurrentSkin.Materials;

                void OnLayoutUpdated(object s, object e)
                {
                    DynamicExpanderList.LayoutUpdated -= OnLayoutUpdated;
                    IsLoadingDropdowns = false;
                    string imgPath;
                    if (App.Settings.FnVersion == "8.51-9.10" || App.Settings.FnVersion == "9.41")
                    {
                        if (CurrentSkin.Series == "None")
                        {
                            imgPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}.png";
                            iconOverlayCh1.Source = new BitmapImage(new Uri(imgPath));

                            imgPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Rarity}_Icon_Overlay.png";
                            RarityIconOverlayCh1.Source = new BitmapImage(new Uri(imgPath));
                        }
                        else
                        {
                            imgPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Series}.png";
                            iconOverlayCh1.Source = new BitmapImage(new Uri(imgPath));

                            imgPath = $"ms-appx:///Assets/Chapter1/{CurrentSkin.Series}_Icon_Overlay.png";
                            RarityIconOverlayCh1.Source = new BitmapImage(new Uri(imgPath));
                        }
                    }
                    else
                    {
                        if (CurrentSkin.Series == "None")
                        {
                            imgPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Icon.png";
                            iconOverlay.Source = new BitmapImage(new Uri(imgPath));

                            imgPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Rarity}_Text.png";
                            textOverlay.Source = new BitmapImage(new Uri(imgPath));
                        }

                        else
                        {
                            imgPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Series}_Icon.png";
                            iconOverlay.Source = new BitmapImage(new Uri(imgPath));

                            imgPath = $"ms-appx:///Assets/Chapter2/{CurrentSkin.Series}_Text.png";
                            textOverlay.Source = new BitmapImage(new Uri(imgPath));
                        }
                    }

                    if (!CurrentSkin.Materials.Any(mat => !mat.Swizzle) && CurrentSkin.Materials.Count > 0) AllSwizzleCheckBoxValue = true;
                    else { IsUpdatingFromCode = true; AllSwizzleCheckBoxValue = false; IsUpdatingFromCode = false; };
                }
                DynamicExpanderList.LayoutUpdated += OnLayoutUpdated;
                Log.Success("Updated the dropdowns!");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public void LoadContent()
        {
            CurrentFnVersion = FnVersionsData.FnVersions.GetValueOrDefault(App.Settings.FnVersion);
            CurrentUeVersion = UeVersionsData.UeVersions.GetValueOrDefault(App.Settings.UeVersion);
            if (App.Settings.FnVersion == "8.51-9.10" || App.Settings.FnVersion == "9.41")
            {
                Ch1PreviewViewBox.Visibility = Visibility.Visible;
                Ch2PreviewViewBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                Ch1PreviewViewBox.Visibility = Visibility.Collapsed;
                Ch2PreviewViewBox.Visibility = Visibility.Visible;
            }
            PhysicsImporterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", $"PhysicsImporter_{CurrentUeVersion.Name}.zip");
            Body = new CharacterPart
            {
                Type = "Body",
            };
            Head = new CharacterPart
            {
                Type = "Head",
            };
            FaceAcc = new CharacterPart
            {
                Type = "Faceacc",
            };
            Hat = new CharacterPart
            {
                Type = "Hat",
            };

            if (string.IsNullOrEmpty(App.Settings.UeProjectPath)) { Log.Error("Unreal Engine Project path is empty!"); return; }
            if (!Path.Exists(App.Settings.UeProjectPath)) { Log.Error($"{App.Settings.UeProjectPath} doesn't exist!"); return; }

            CookedAssetsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath),
            "Saved", "Cooked", "WindowsNoEditor", Path.GetFileNameWithoutExtension(App.Settings.UeProjectPath), "Content");

            string pluginsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Plugins", "PhysicsImporter");
            if (!Path.Exists(pluginsPath)) ZipFile.ExtractToDirectory(PhysicsImporterPath, pluginsPath);

            if (CurrentUeVersion.ReplaceDefaultEngineIni)
            {
                string defaultEngineIniPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath),
                "Config", "DefaultEngine.ini");
                byte[] defaultEngineIniInBytes = TemplateLoader.GetEmbeddedFile(CurrentUeVersion.Name, "RawUeAssets", "DefaultEngine.ini");
                if (defaultEngineIniInBytes != null) File.WriteAllBytes(defaultEngineIniPath, defaultEngineIniInBytes);
            }

            CurrentSkinPathBox_TextChanged("NoDelay", null);
        }

        private async Task UpdateSkinPreviewImage(string sourcePath, string codename, string largeIcon)
        {
            var bitmap = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
            using (var fileStream = File.OpenRead(Path.Combine(sourcePath, $"{codename}.png")))
            {
                var inMemoryStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                await WindowsRuntimeStreamExtensions.AsStreamForWrite(inMemoryStream).WriteAsync(
                    await File.ReadAllBytesAsync(Path.Combine(sourcePath, $"{codename}.png"))
                );
                inMemoryStream.Seek(0);

                await bitmap.SetSourceAsync(inMemoryStream);
                characterPreview.Source = bitmap;
                characterPreviewCh1.Source = bitmap;
            }

            if (!string.IsNullOrEmpty(largeIcon))
            {
                var iconBitmap = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                string iconPath = Path.Combine(sourcePath, "Textures", $"{largeIcon}.png");

                var inMemoryStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                byte[] fileBytes = await File.ReadAllBytesAsync(iconPath);
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(inMemoryStream))
                {
                    dataWriter.WriteBytes(fileBytes);
                    await dataWriter.StoreAsync();
                    await dataWriter.FlushAsync();
                    dataWriter.DetachStream();
                }
                inMemoryStream.Seek(0);

                await iconBitmap.SetSourceAsync(inMemoryStream);
                iconPreview.Source = iconBitmap;
                iconPreviewCh1.Source = iconBitmap;
            }
            Console.WriteLine("Successfully updated the preview image!");
        }

        private void ResetCpData()
        {
            CurrentSkin = new SkinData();
            UpdateDropdowns();
        }

        private SkinData LoadSkinConfig(string jsonPath)
        {
            string filePath = jsonPath;

            if (!File.Exists(filePath)) return null;

            string jsonString = File.ReadAllText(filePath);

            var node = System.Text.Json.Nodes.JsonNode.Parse(jsonString)?.AsObject();
            // Change legacy "CodeName" to new "Codename"
            if (node != null && node.ContainsKey("CodeName") && !node.ContainsKey("Codename"))
            {
                var value = node["CodeName"];
                node.Remove("CodeName");
                node["Codename"] = value;
                jsonString = node.ToJsonString();
            }

            // To prevent legacy .json files crashing the program, I applied these changes:
            // Capitalize CharacterPart Type
            // Convert PskPath (full path) to Psk (filename only)
            // Convert PhysicsAssetJsonPaths to PhysicsAssets (filenames only)
            if (node.ContainsKey("CharacterParts") && node["CharacterParts"] is System.Text.Json.Nodes.JsonArray parts)
            {
                foreach (var part in parts)
                {
                    if (part is System.Text.Json.Nodes.JsonObject partObj)
                    {
                        if (partObj.ContainsKey("Type"))
                        {
                            string typeValue = partObj["Type"]?.ToString();
                            if (!string.IsNullOrEmpty(typeValue))
                            {
                                partObj["Type"] = char.ToUpper(typeValue[0]) + typeValue.Substring(1);
                            }
                        }

                        if (partObj.ContainsKey("PskPath"))
                        {
                            string pathValue = partObj["PskPath"]?.ToString();
                            partObj.Remove("PskPath");
                            partObj["Psk"] = !string.IsNullOrEmpty(pathValue) ? System.IO.Path.GetFileNameWithoutExtension(pathValue) : "";
                        }

                        if (partObj.ContainsKey("PhysicsAssetJsonPaths"))
                        {
                            string[] jsonNames = partObj["PhysicsAssetJsonPaths"]?.AsArray().Select(json => Path.GetFileNameWithoutExtension(json.ToString())).ToArray();
                            partObj.Remove("PhysicsAssetJsonPaths");
                            partObj["PhysicsAssets"] = System.Text.Json.JsonSerializer.SerializeToNode(jsonNames);
                        }
                    }
                }
                jsonString = node.ToJsonString();
            }

            SkinData loadedSkin = System.Text.Json.JsonSerializer.Deserialize<SkinData>(jsonString);

            try
            {
                // Reconstruct the ignored ParentPage and Cp properties for the UI
                foreach (var mat in loadedSkin.Materials)
                {
                    mat.ParentPage = this;
                    mat.Cp = loadedSkin.CharacterParts.FirstOrDefault(cp => cp.Type == mat.Cp?.Type);
                }

                if (CurrentFnVersion.ManuallySwizzleMaterials) TextureSwizzler.SwizzleSpecularTextures(CurrentSkin.TexturesPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            return loadedSkin;
        } // TODO: This method should only load the data that doesn't have JsonIgnore, right now it loads everything from the .json 
                                                            // as a new SkinData object, so everything that had JsonIgnore has the default value assigned in the class, so to avoid 
                                                            // getting null objects, reassigning the variables that had JsonIgnore is mandatory at the moment.
        public void SaveSkinConfig()
        {
            if (CurrentSkin == null || string.IsNullOrEmpty(CurrentSkin.Path)) return;

            string jsonPath = Path.Combine(CurrentSkin.Path, $"{CurrentSkin.Codename}_Settings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(CurrentSkin, options);

            File.WriteAllText(jsonPath, jsonString);
        } 
    }

    public class SkinData : INotifyPropertyChanged
    {
        public string Codename { get; set; } = string.Empty;
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _rarity = "Common";
        public string Rarity
        {
            get => _rarity;
            set
            {
                if (_rarity != value)
                {
                    _rarity = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _series = "None";
        public string Series
        {
            get => _series;
            set
            {
                if (_series != value)
                {
                    _series = value;
                    OnPropertyChanged();
                }
            }

        }
        private string _gender;
        public string Gender
        {
            get => _gender;
            set
            {
                if (_gender != value)
                {
                    _gender = value;
                    OnPropertyChanged();
                }
            }

        }
        public string SmallIcon { get; set; } = string.Empty;
        public string LargeIcon { get; set; } = string.Empty;
        private string _cid = string.Empty;
        public string CID
        {
            get => _cid;
            set
            {
                if (_cid != value)
                {
                    _cid = value;
                    OnPropertyChanged();
                }
            }

        }
        public string LobbyAnimationPsa { get; set; } = string.Empty;
        public string LobbyAnimationJson { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string LobbyAnimationFbx { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string OutputContentPath { get; set; } = string.Empty;
        public ObservableCollection<Material> Materials { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public string Path = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourcePath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string MeshesPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string TexturesPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string PhysicsPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore] 
        public string LobbyAnimationFolderPath { get; set; } = string.Empty;
        public float LobbyAnimationLength { get; set; } = 0;
        public List<CharacterPart> CharacterParts { get; set; } = new();
        public List<string> Textures { get; set; } = new();
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Material : INotifyPropertyChanged
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public Windows.Globalization.NumberFormatting.DecimalFormatter DotFormatter { get; } =
            new Windows.Globalization.NumberFormatting.DecimalFormatter(new[] { "en-US" }, "US");
        public string Name { get; set; }
        public List<string> TextureOptions { get; set; }
        private string _selectedDiffuse = "Default_Diffuse";
        public string SelectedDiffuse
        {
            get => _selectedDiffuse;
            set
            {
                if (_selectedDiffuse != value)
                {
                    _selectedDiffuse = value;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(value) && value.Length > 0)
                    {
                        string baseName = value.Substring(0, value.Length - 1);
                        string mask = baseName + "M";
                        string normal = baseName + "N";
                        string spec = baseName + "S";

                        if (TextureOptions?.Contains(mask) == true) SelectedMask = mask;
                        if (TextureOptions?.Contains(normal) == true) SelectedNormal = normal;
                        if (TextureOptions?.Contains(spec) == true) SelectedSpecular = spec;
                    }
                }
            }
        }

        private string _selectedMask = "Default_Mask";
        public string SelectedMask
        {
            get => _selectedMask;
            set
            {
                if (_selectedMask != value)
                {
                    _selectedMask = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _selectedNormal = "Default_Normal";
        public string SelectedNormal
        {
            get => _selectedNormal;
            set
            {
                if (_selectedNormal != value)
                {
                    _selectedNormal = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _selectedSpecular = "Default_Specular";
        public string SelectedSpecular
        {
            get => _selectedSpecular;
            set
            {
                if (_selectedSpecular != value)
                {
                    _selectedSpecular = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _useSkinBoostColor = false;
        public bool UseSkinBoostColor
        {
            get => _useSkinBoostColor;

            set
            {
                if (_useSkinBoostColor != value)
                {
                    _useSkinBoostColor = value;
                    OnPropertyChanged();
                }
            }
        }

        private float _sbcRed = 0f;
        public float SbcRed
        {
            get => _sbcRed;

            set
            {
                _sbcRed = value;

                if (ParentPage?.CurrentSkin?.Materials == null)
                {
                    OnPropertyChanged();
                    return;
                }

                foreach (Material mat in ParentPage?.CurrentSkin.Materials)
                {
                    if (!mat.UseSkinBoostColor && value != 0 && Math.Abs(mat._sbcRed - value) > 0.0001f)
                    {
                        mat.SbcRed = value;
                    }
                }

                if (UseSkinBoostColor) Console.WriteLine($"Changed Skin Boost Color And Exponent's red to {value} on {Name}");
                OnPropertyChanged();
            }
        }

        private float _sbcGreen = 0f;
        public float SbcGreen
        {
            get => _sbcGreen;

            set
            {
                _sbcGreen = value;

                if (ParentPage?.CurrentSkin?.Materials == null)
                {
                    OnPropertyChanged();
                    return;
                }

                foreach (Material mat in ParentPage?.CurrentSkin.Materials)
                {
                    if (!mat.UseSkinBoostColor && value != 0 && Math.Abs(mat._sbcGreen - value) > 0.0001f)
                    {
                        mat.SbcGreen = value;
                    }
                }

                if (UseSkinBoostColor) Console.WriteLine($"Changed Skin Boost Color And Exponent's green to {value} on {Name}");
                OnPropertyChanged();
            }
        }

        private float _sbcBlue = 0f;
        public float SbcBlue
        {
            get => _sbcBlue;

            set
            {
                _sbcBlue = value;

                if (ParentPage?.CurrentSkin?.Materials == null)
                {
                    OnPropertyChanged();
                    return;
                }

                foreach (Material mat in ParentPage?.CurrentSkin.Materials)
                {
                    if (!mat.UseSkinBoostColor && value != 0 && Math.Abs(mat._sbcBlue - value) > 0.0001f)
                    {
                        mat.SbcBlue = value;
                    }
                }

                if (UseSkinBoostColor) Console.WriteLine($"Changed Skin Boost Color And Exponent's blue to {value} on {Name}");
                OnPropertyChanged();
            }
        }

        private float _sbcAlpha = 0f;
        public float SbcAlpha
        {
            get => _sbcAlpha;

            set
            {
                _sbcAlpha = value;

                if (ParentPage?.CurrentSkin?.Materials == null)
                {
                    OnPropertyChanged();
                    return;
                }

                foreach (Material mat in ParentPage?.CurrentSkin.Materials)
                {
                    if (!mat.UseSkinBoostColor && value != 0 && Math.Abs(mat._sbcAlpha - value) > 0.0001f)
                    {
                        mat.SbcAlpha = value;
                    }
                }

                if (UseSkinBoostColor) Console.WriteLine($"Changed Skin Boost Color And Exponent's alpha to {value} on {Name}");
                OnPropertyChanged();
            }
        }
        [System.Text.Json.Serialization.JsonIgnore]
        public SkinsPage ParentPage { get; set; }

        private bool _swizzle = false;
        public bool Swizzle
        {
            get => _swizzle;
            set
            {
                if (_swizzle != value)
                {
                    _swizzle = value;
                    OnPropertyChanged();
                    ParentPage?.UpdateAllSwizzleCheckBoxState();
                }
            }
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public CharacterPart Cp { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ParentPage?.SaveSkinConfig();
        }
    }

    [JsonSerializable(typeof(BlenderExportData))]
    [JsonSerializable(typeof(UnrealExportData))]
    internal partial class AppJsonContext : JsonSerializerContext { }
}