#pragma warning disable
using ABI.Windows.ApplicationModel.Activation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.FieldTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;
using UAssetAPI.UnrealTypes.EngineEnums;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Geolocation;
using Windows.Devices.Perception;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.AppBroadcasting;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using UFMT.Helper;
using Windows.ApplicationModel.Calls;

namespace UFMT
{
    public sealed partial class SkinsPage : Page, INotifyPropertyChanged
    {
        public static FnVersion CurrentFnVersion = FnVersionsData.FnVersions.GetValueOrDefault(App.Settings.FnVersion);
        public static UeVersion CurrentUeVersion = UeVersionsData.UeVersions.GetValueOrDefault(App.Settings.UeVersion);
        private static string RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh1.py");
        private static string PhysicsImporterPath;
        private string CookedAssetsPath;
        private static string ValidCodenameCharacters = "abcdefghijklmnopqrstuvwxyz1234567890_";
        private static string MaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Male_Commando_Idle_01.psa");
        private static string FemaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Female_Commando_Idle_01.psa");
        private CancellationTokenSource _currentSkinPathDebounce;
        public Dictionary<string, string> SeriesCodenames = new(){ {"Dark Series", "CUBESeries"}, { "Star Wars Series", "ColumbusSeries" },
        {"Icon Series", "CreatorCollabSeries"}, {"DC Series", "DCUSeries"}, {"Frozen Series", "FrozenSeries" }, {"Lava Series", "LavaSeries"},
        {"Marvel Series", "MarvelSeries"}, {"Shadow Series", "ShadowSeries"},  {"Slurp Series", "SlurpSeries"},  
        {"Test Series", "FakeToken_FDS_Series"}, {"Anual Pass Series", "2020AnnualPassSeries"}};
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
            CurrentSkin.CodeName = new DirectoryInfo(CurrentSkin.Path).Name;
            CurrentSkin.CID = $"CID_{CurrentSkin.CodeName}";


            SkinData loadedJson = LoadSkinConfig(Path.Combine(CurrentSkin.Path, $"{CurrentSkin.CodeName}_Settings.json"));

            if (loadedJson != null)
            {
                CurrentSkin = loadedJson;
                CurrentSkin.Path = CurrentSkinPathBox.Text;
                CurrentSkin.LobbyAnimationFolderPath = Path.Combine(CurrentSkin.SourcePath, "Lobby_Animation");
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
                PskReader.GetMaterialData(CurrentSkin.CharacterParts.Select(cp => cp.PskPath).ToList(), CurrentSkin.CharacterParts, allSwizzleCheckBox.IsChecked.Value, this);

                if (materials == null) return;
                CurrentSkin.Materials = new ObservableCollection<Material>(materials);

                DefaultTextureSetup.CreateDefaultTextures(DefaultTextureSetup.FindMissingDefaultTextures(CurrentSkin.TexturesPath), CurrentSkin.TexturesPath);
                if (CurrentFnVersion.ManuallySwizzleMaterials) TextureSwizzler.SwizzleSpecularTextures(CurrentSkin.TexturesPath);
                (CurrentSkin.LargeIcon, CurrentSkin.SmallIcon) = TextureCategorizer.GetIconTextures(CurrentSkin.TexturesPath);
                CurrentSkin.Textures = TextureCategorizer.GetAllTextures(CurrentSkin.TexturesPath);
                MaterialTextureAssigner.AssignTexturesToAllMaterials(CurrentSkin.TexturesPath, CurrentSkin.CodeName, CurrentSkin.Materials);
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
            RenderPreviewImage();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SkinValidator.ValidateBeforeExport(App.Settings.UeVersion, CurrentSkin.Gender, CurrentSkin.Name, CurrentSkin.Description, CurrentSkin.CID)) return;
            if (!await FbxConverter.ConvertPskToFbx(CurrentSkin.CharacterParts, CurrentSkin.SourcePath, CurrentSkin.CodeName)) return;

            var (isAnimValid, lobbyAnimationFbx, lobbyAnimationLength) = 
            await FbxConverter.ConvertPsaToFbx(CurrentSkin.SourcePath, CurrentSkin.CodeName, CurrentSkin.LobbyAnimationFolderPath, CurrentSkin.LobbyAnimationPsa);
            if (!isAnimValid) return;
            CurrentSkin.LobbyAnimationFbx = lobbyAnimationFbx;
            CurrentSkin.LobbyAnimationLength = lobbyAnimationLength;
            string cookedCodeNamePath = Path.Combine(CookedAssetsPath, "CustomSkins", CurrentSkin.CodeName);

            UnrealDependencySetup.CreateMissingFiles(CookedAssetsPath, CurrentSkin.CodeName, CurrentUeVersion.BaseHeadPath, CurrentUeVersion.FakeCIDBase64,
            CurrentUeVersion.BaseMeshSkeletonBase64, CurrentUeVersion.BaseMeshBase64, CurrentUeVersion.BaseHeadBase64Strings, cookedCodeNamePath);

            UnrealExportData unrealData = UnrealExportDataCollector.CollectData(CurrentSkin.SmallIcon, CurrentSkin.LargeIcon, CurrentSkin.Materials, CurrentSkin.TexturesPath,
            CurrentFnVersion.ManuallySwizzleMaterials, CurrentSkin.SourcePath, CurrentSkin.LobbyAnimationFbx, CurrentSkin.LobbyAnimationJson, CurrentSkin.CharacterParts,
            CurrentSkin.Gender, CurrentSkin.CodeName, CurrentSkin.CID);


            await UnrealProcessRunner.LaunchUnreal(unrealData);
            await UnrealProcessRunner.CookFiles();

            CurrentUeVersion.FixRequiredFiles(Path.Combine
            (cookedCodeNamePath, "Animations", $"{CurrentSkin.CodeName}_Lobby_Animation.uasset"), CurrentSkin.CharacterParts.Select
            (cp => Path.Combine(cookedCodeNamePath, "Meshes", $"{Path.GetFileNameWithoutExtension(cp.FbxPath)}.uasset")).ToArray());

            AssetRegistryBuilder.CreateAssetRegistry(CookedAssetsPath, CurrentUeVersion.CidJsonBase64, CurrentUeVersion.AssetRegistryBinBase64, CurrentSkin.Path, OutputFnGamePath);
            CreateCharacterAssets();
            U4Pak.Pack(OutputFnGamePath, Path.Combine(Path.GetDirectoryName(OutputFnGamePath), $"z_{CurrentSkin.CodeName}.pak"));
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
                Log.Error(ex.ToString());
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
                RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh1.py");
            }
            else
            {
                Ch1PreviewViewBox.Visibility = Visibility.Collapsed;
                Ch2PreviewViewBox.Visibility = Visibility.Visible;
                RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreviewCh2.py");
            }
            PhysicsImporterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", CurrentUeVersion.PhysicsImporterName);
            Body = new CharacterPart
            {
                Type = "body",
                uassetFileBase64 = CurrentFnVersion.BodyCpMaleUassetBase64,
                uexpFileBase64 = CurrentFnVersion.BodyCpMaleUexpBase64
            };
            Head = new CharacterPart
            {
                Type = "head",
                uassetFileBase64 = CurrentFnVersion.HeadCpMaleUassetBase64,
                uexpFileBase64 = CurrentFnVersion.HeadCpMaleUexpBase64
            };
            FaceAcc = new CharacterPart
            {
                Type = "faceacc",
                uassetFileBase64 = CurrentFnVersion.FaceAccCpMaleUassetBase64,
                uexpFileBase64 = CurrentFnVersion.FaceAccCpMaleUexpBase64
            };
            Hat = new CharacterPart
            {
                Type = "hat",
                uassetFileBase64 = CurrentFnVersion.HatCpUassetBase64,
                uexpFileBase64 = CurrentFnVersion.HatCpUexpBase64
            };

            if (string.IsNullOrEmpty(App.Settings.UeProjectPath)) { Log.Error("Unreal Engine Project path is empty!"); return; }
            if (!Path.Exists(App.Settings.UeProjectPath)) { Log.Error($"{App.Settings.UeProjectPath} doesn't exist!"); return; }

            CookedAssetsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath),
            "Saved", "Cooked", "WindowsNoEditor", Path.GetFileNameWithoutExtension(App.Settings.UeProjectPath), "Content");

            string pluginsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Plugins", "PhysicsImporter");
            if (!Path.Exists(pluginsPath)) ZipFile.ExtractToDirectory(PhysicsImporterPath, pluginsPath);

            CurrentSkinPathBox_TextChanged("NoDelay", null);
        }

        private void ResetCpData()
        {
            CurrentSkin = new SkinData();
            UpdateDropdowns();
        }

        private async void RenderPreviewImage()
        {
            try
            {
                string[] pskPaths = CurrentSkin.CharacterParts.Select(cp => cp.PskPath).ToArray();
                List<string> materials = new();
                List<string> texturePaths = new();
                List<bool> swizzleMaterials = new();
                string lobbyAnimPath = CurrentSkin.Gender == "Male" ? MaleLobbyAnimPath : FemaleLobbyAnimPath;
                lobbyAnimPath = CurrentSkin.LobbyAnimationPsa == string.Empty ? lobbyAnimPath :
                Path.Combine(CurrentSkin.LobbyAnimationFolderPath, $"{CurrentSkin.LobbyAnimationPsa}.psa");

                foreach (Material mat in CurrentSkin.Materials)
                {
                    materials.Add(mat.Name);
                    texturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedDiffuse}.png"));
                    texturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedMask}.png"));
                    texturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedNormal}.png"));
                    texturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedSpecular}.png"));

                    swizzleMaterials.Add(mat.Swizzle);
                }

                var exportData = new BlenderExportData
                {
                    Psks = pskPaths,
                    Textures = texturePaths,
                    Swizzle = swizzleMaterials,
                    Materials = materials,
                    RenderPath = Path.Combine(CurrentSkin.Path, "Source", $"{CurrentSkin.CodeName}.png"),
                    LobbyAnimPath = lobbyAnimPath,
                    HeadPsk = CurrentSkin.CharacterParts.FirstOrDefault(cp => cp.Type == "head").PskPath
                };

                string jsonString = System.Text.Json.JsonSerializer.Serialize(exportData, AppJsonContext.Default.BlenderExportData);

                // Base64 to prevent spaces or quotes 
                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(jsonString);
                string base64Json = System.Convert.ToBase64String(plainTextBytes);

                string arguments = $"-b --python \"{RenderScript}\" -- {base64Json}";

                Console.WriteLine("Rendering the preview...");

                await Task.Run(() =>
                {
                    Process blender = Process.Start(App.Settings.BlenderPath, arguments);
                    blender.WaitForExit();
                });
                Log.Success("Successfully Rendered the preview image!");

                await Task.Delay(10);

                var bitmap = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                using (var fileStream = System.IO.File.OpenRead(Path.Combine(CurrentSkin.SourcePath, $"{CurrentSkin.CodeName}.png")))
                {
                    var inMemoryStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    await System.IO.WindowsRuntimeStreamExtensions.AsStreamForWrite(inMemoryStream).WriteAsync(
                        await System.IO.File.ReadAllBytesAsync(Path.Combine(CurrentSkin.SourcePath, $"{CurrentSkin.CodeName}.png"))
                    );
                    inMemoryStream.Seek(0);

                    await bitmap.SetSourceAsync(inMemoryStream);
                    characterPreview.Source = bitmap;
                    characterPreviewCh1.Source = bitmap;
                }

                if (!string.IsNullOrEmpty(CurrentSkin.LargeIcon))
                {
                    var iconBitmap = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
                    string iconPath = Path.Combine(CurrentSkin.SourcePath, "Textures", $"{CurrentSkin.LargeIcon}.png");

                    var inMemoryStream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
                    byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(iconPath);
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
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

        }

        private void CreateCharacterAssets()
        {

            DirectoryInfo cookedCharacterDirectory = new DirectoryInfo(Path.Combine(CookedAssetsPath, "CustomSkins", CurrentSkin.CodeName));
            string contentFolderPath = Path.Combine(OutputFnGamePath, "Content", "CustomSkins", CurrentSkin.CodeName);
            string characterPartsPath = Path.Combine(contentFolderPath, "CharacterParts");
            string materialsPath = Path.Combine(contentFolderPath, "Materials");
            CharacterPart body = CurrentSkin.CharacterParts.FirstOrDefault(cp => cp.Type == "body");
            CharacterPart head = CurrentSkin.CharacterParts.FirstOrDefault(cp => cp.Type == "head");
            CharacterPart faceacc = CurrentSkin.CharacterParts.FirstOrDefault(cp => cp.Type == "faceacc");
            CharacterPart hat = CurrentSkin.CharacterParts.FirstOrDefault(cp => cp.Type == "hat");
            if (!Path.Exists(contentFolderPath)) Directory.CreateDirectory(contentFolderPath);

            foreach (DirectoryInfo subFolder in cookedCharacterDirectory.GetDirectories("*", SearchOption.AllDirectories))
            {
                string targetSubDir = subFolder.FullName.Replace(cookedCharacterDirectory.FullName, contentFolderPath);
                Directory.CreateDirectory(targetSubDir);

                foreach (FileInfo file in subFolder.GetFiles())
                {
                    file.CopyTo(Path.Combine(targetSubDir, file.Name), true);
                }
            }

            string cookedBaseHeadPath = Path.Combine(CookedAssetsPath, "Base", "Head", "Skeleton");
            if (App.Settings.FnVersion == "9.41") cookedBaseHeadPath = Path.Combine(CookedAssetsPath, "Modding", "Base_Head"); // 9.41 uses a different location for base head

            string outputBaseHeadPath = Path.Combine(OutputFnGamePath, CurrentUeVersion.BaseHeadPath);

            if (!Directory.Exists(outputBaseHeadPath)) Directory.CreateDirectory(outputBaseHeadPath);

            if (CurrentUeVersion.ReplaceCookedBaseHead)
            {
                foreach (var (fileName, base64String) in CurrentUeVersion.CookedBaseHeadBase64Strings)
                {
                    File.WriteAllBytes(Path.Combine(cookedBaseHeadPath, fileName), Convert.FromBase64String(base64String));
                }
            } // Replace the files inside cooked ue folders

            foreach (string file in Directory.GetFiles(cookedBaseHeadPath))
            {
                File.Copy(file, Path.Combine(outputBaseHeadPath, Path.GetFileName(file)), true);
            }

            Log.Success($"Copied files from {cookedCharacterDirectory} to {contentFolderPath}");
            if (!Path.Exists(characterPartsPath)) Directory.CreateDirectory(characterPartsPath);

            if (CurrentSkin.Gender == "Female")
            {
                body.uassetFileBase64 = CurrentFnVersion.BodyCpFemaleUassetBase64;
                body.uexpFileBase64 = CurrentFnVersion.BodyCpFemaleUexpBase64;
                head.uassetFileBase64 = CurrentFnVersion.HeadCpFemaleUassetBase64;
                head.uexpFileBase64 = CurrentFnVersion.HeadCpFemaleUexpBase64;
                if (faceacc != null)
                {
                    faceacc.uassetFileBase64 = CurrentFnVersion.FaceAccCpFemaleUassetBase64;
                    faceacc.uexpFileBase64 = CurrentFnVersion.FaceAccCpFemaleUexpBase64;
                }
            }
            else if (CurrentSkin.Gender == "Male")
            {
                body.uassetFileBase64 = CurrentFnVersion.BodyCpMaleUassetBase64;
                body.uexpFileBase64 = CurrentFnVersion.BodyCpMaleUexpBase64;
                head.uassetFileBase64 = CurrentFnVersion.HeadCpMaleUassetBase64;
                head.uexpFileBase64 = CurrentFnVersion.HeadCpMaleUexpBase64;
                if (faceacc != null)
                {
                    faceacc.uassetFileBase64 = CurrentFnVersion.FaceAccCpMaleUassetBase64;
                    faceacc.uexpFileBase64 = CurrentFnVersion.FaceAccCpMaleUexpBase64;
                }
            }

            //Character part creation
            foreach (CharacterPart cp in CurrentSkin.CharacterParts)
            {
                Console.WriteLine($"Currently editing the {cp.Type} of the skin");
                string uassetPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{CurrentSkin.CodeName}.uasset");
                string uexpPath = Path.Combine(characterPartsPath,
                $"CP_{cp.Type}_{CurrentSkin.CodeName}.uexp");

                File.WriteAllBytes(uassetPath, Convert.FromBase64String(cp.uassetFileBase64));
                File.WriteAllBytes(uexpPath, Convert.FromBase64String(cp.uexpFileBase64));

                var currentCp = new UAsset(uassetPath, CurrentUeVersion.UassetApiEngineVer);
                var cpExport0 = (NormalExport)currentCp.Exports[0];
                var cpExport1 = (NormalExport)currentCp.Exports[1];
                cpExport1.ObjectName.Value.Value = $"CP_{cp.Type}_{CurrentSkin.CodeName}";
                if (cp.Type != "hat")
                {
                    string animBpPath;
                    if (cp.Type == "head")
                    {
                        if (App.Settings.FnVersion == "9.41") animBpPath = "/Game/Modding/Base_Head/Base_Head_Modding_AnimBP.Base_Head_Modding_AnimBP_C";
                        else animBpPath = "/Game/Base/Head/Skeleton/Base_Head_AnimBP.Base_Head_AnimBP_C";
                    }
                    else animBpPath = $"/Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/{CurrentSkin.CodeName}_{cp.Type}_AnimBP.{CurrentSkin.CodeName}_{cp.Type}_AnimBP_C";

                    var animBpData = (SoftObjectPropertyData)cpExport0["AnimClass"];
                    animBpData.Value.AssetPath.AssetName.Value.Value = animBpPath;

                    Console.WriteLine($"Changed the Animation Blueprint in CP_{cp.Type}_{CurrentSkin.CodeName} to {animBpPath}");
                }
                var mesh = (SoftObjectPropertyData)cpExport1["SkeletalMesh"];
                mesh.Value.AssetPath.AssetName.Value.Value = $"/Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                $"{CurrentSkin.CodeName}_{cp.Type}.{CurrentSkin.CodeName}_{cp.Type}";
                Console.WriteLine($"Changed the Mesh in CP_{cp.Type}_{CurrentSkin.CodeName} to /Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                $"{CurrentSkin.CodeName}_{cp.Type}.{CurrentSkin.CodeName}_{cp.Type}");

                Console.WriteLine(uassetPath);
                currentCp.Write(uassetPath);
                Log.Success($"Successfully edited CP_{cp.Type}_{CurrentSkin.CodeName}.uasset and " +
                $"CP_{cp.Type}_{CurrentSkin.CodeName}.uexp");
            }

            //Material creation
            foreach (Material material in CurrentSkin.Materials)
            {
                string uassetMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uasset");
                string uexpMaterialPath = Path.Combine(materialsPath, $"{material.Name}.uexp");
                string materialUassetBase64;
                string materialUexpBase64;

                materialUassetBase64 = CurrentFnVersion.MiNoSwizzleUassetBase64;
                materialUexpBase64 = CurrentFnVersion.MiNoSwizzleUexpBase64;
                if (!CurrentFnVersion.ManuallySwizzleMaterials && material.Swizzle)
                {
                    materialUassetBase64 = CurrentFnVersion.MiSwizzleUassetBase64;
                    materialUexpBase64 = CurrentFnVersion.MiSwizzleUexpBase64;
                }

                File.WriteAllBytes(Path.Combine(uassetMaterialPath),
                Convert.FromBase64String(materialUassetBase64));
                File.WriteAllBytes(Path.Combine(uexpMaterialPath),
                Convert.FromBase64String(materialUexpBase64));
                Log.Success($"Created material instance {material.Name}");
                Console.WriteLine($"Editing {material.Name}");

                var currentMi = new UAsset(uassetMaterialPath, CurrentUeVersion.UassetApiEngineVer);
                var miImportData = currentMi.Imports;
                var miExportData = currentMi.Exports;
                var miExport0 = (NormalExport)currentMi.Exports[0];
                string fnTexturesPath = $"/Game/CustomSkins/{CurrentSkin.CodeName}/Textures/";
                miImportData[CurrentFnVersion.DiffusePathIndex].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedDiffuse);
                Console.WriteLine($"Changed the diffuse texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedDiffuse)}");
                miImportData[CurrentFnVersion.DiffusePathIndex + 1].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedMask);
                Console.WriteLine($"Changed the mask texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedMask)}");
                miImportData[CurrentFnVersion.DiffusePathIndex + 2].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedNormal);
                Console.WriteLine($"Changed the normal texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedNormal)}");
                miImportData[CurrentFnVersion.DiffusePathIndex + 3].ObjectName.Value.Value = Path.Combine(fnTexturesPath, material.SelectedSpecular);
                Console.WriteLine($"Changed the specular texture path in {material.Name} to {Path.Combine(fnTexturesPath, material.SelectedSpecular)}");
                miImportData[CurrentFnVersion.DiffuseNameIndex].ObjectName.Value.Value = material.SelectedDiffuse;
                Console.WriteLine($"Changed the diffuse texture in {material.Name} to {material.SelectedDiffuse}");
                miImportData[CurrentFnVersion.DiffuseNameIndex + 1].ObjectName.Value.Value = material.SelectedMask;
                Console.WriteLine($"Changed the mask texture in {material.Name} to {material.SelectedMask}");
                miImportData[CurrentFnVersion.DiffuseNameIndex + 2].ObjectName.Value.Value = material.SelectedNormal;
                Console.WriteLine($"Changed the normal texture in {material.Name} to {material.SelectedNormal}");
                miImportData[CurrentFnVersion.DiffuseNameIndex + 3].ObjectName.Value.Value = material.SelectedSpecular;
                Console.WriteLine($"Changed the specular texture in {material.Name} to {material.SelectedSpecular}");
                miExportData[0].ObjectName.Value.Value = material.Name;

                if (material.UseSkinBoostColor)
                {
                    var vectorParamaterValues = (ArrayPropertyData)miExport0["VectorParameterValues"];
                    var vectorParamaterValues2 = (StructPropertyData)vectorParamaterValues.Value[0];
                    var parameterValue = (StructPropertyData)vectorParamaterValues2.Value[1];
                    var colors = (LinearColorPropertyData)parameterValue.Value[0];

                    colors.Value = new FLinearColor(material.SbcRed, material.SbcGreen, material.SbcBlue, material.SbcAlpha);

                    Console.WriteLine($"Changed the skin boost color and exponent to {colors.Value.ToString()} in {material}");
                }

                currentMi.Write(uassetMaterialPath);
                Log.Success($"Successfully edited {material.Name}.uasset and {material.Name}.uexp");
            }

            //HS creation
            string hsUassetBase64;
            string hsUexpBase64;
            if (!string.IsNullOrEmpty(faceacc?.PskPath))
            {
                hsUassetBase64 = CurrentFnVersion.HsBodyHeadFaceAccUassetBase64;
                hsUexpBase64 = CurrentFnVersion.HsBodyHeadFaceAccUexpBase64;
            }
            else if (!string.IsNullOrEmpty(hat?.PskPath))
            {
                hsUassetBase64 = CurrentFnVersion.HsBodyHeadHatUassetBase64;
                hsUexpBase64 = CurrentFnVersion.HsBodyHeadHatUexpBase64;
            }
            else
            {
                hsUassetBase64 = CurrentFnVersion.HsBodyHeadUassetBase64;
                hsUexpBase64 = CurrentFnVersion.HsBodyHeadUexpBase64;
            }

            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{CurrentSkin.CodeName}.uasset"), Convert.FromBase64String(hsUassetBase64));
            File.WriteAllBytes(Path.Combine(contentFolderPath, $"HS_{CurrentSkin.CodeName}.uexp"), Convert.FromBase64String(hsUexpBase64));

            Console.WriteLine("Editing the HS");

            var currentHs = new UAsset(Path.Combine(contentFolderPath, $"HS_{CurrentSkin.CodeName}.uasset"), CurrentUeVersion.UassetApiEngineVer);
            var hsExport0 = (NormalExport)currentHs.Exports[0];
            var characterPartsArray = (ArrayPropertyData)hsExport0["CharacterParts"];
            var headCp = (SoftObjectPropertyData)characterPartsArray.Value[0];
            var bodyCp = (SoftObjectPropertyData)characterPartsArray.Value[1];
            headCp.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_head_{CurrentSkin.CodeName}.CP_head_{CurrentSkin.CodeName}";
            Console.WriteLine($"Changed the Head Character Part path in HS_{CurrentSkin.CodeName} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_head_{CurrentSkin.CodeName}.CP_head_{CurrentSkin.CodeName}");

            bodyCp.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_body_{CurrentSkin.CodeName}.CP_body_{CurrentSkin.CodeName}";
            Console.WriteLine($"Changed the Body Character Part path in HS_{CurrentSkin.CodeName} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_body_{CurrentSkin.CodeName}.CP_body_{CurrentSkin.CodeName}");


            if (!string.IsNullOrEmpty(faceacc?.PskPath))
            {
                var faceAccCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                faceAccCp.Value.AssetPath.AssetName.Value.Value =
                $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_faceacc_{CurrentSkin.CodeName}.CP_faceacc_{CurrentSkin.CodeName}";
                Console.WriteLine($"Changed the FaceAcc Character Part path in HS_{CurrentSkin.CodeName} to " +
                $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_faceacc_{CurrentSkin.CodeName}.CP_faceacc_{CurrentSkin.CodeName}");

            }
            else if (!string.IsNullOrEmpty(hat?.PskPath))
            {
                var hatCp = (SoftObjectPropertyData)characterPartsArray.Value[2];
                hatCp.Value.AssetPath.AssetName.Value.Value =
                $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_hat_{CurrentSkin.CodeName}.CP_hat_{CurrentSkin.CodeName}";
                Console.WriteLine($"Changed the Hat Character Part path in HS_{CurrentSkin.CodeName} to " +
                $"/Game/CustomSkins/{CurrentSkin.CodeName}/CharacterParts/CP_hat_{CurrentSkin.CodeName}.CP_hat_{CurrentSkin.CodeName}");
            }

            hsExport0.ObjectName.Value.Value = $"HS_{CurrentSkin.CodeName}";

            currentHs.Write(Path.Combine(contentFolderPath, $"HS_{CurrentSkin.CodeName}.uasset"));
            Log.Success($"Successfuly edited HS_{CurrentSkin.CodeName}.uasset and HS_{CurrentSkin.CodeName}.uexp");

            //HID creation
            Console.WriteLine("Editing HID...");
            string hidUassetPath = Path.Combine(contentFolderPath, $"HID_{CurrentSkin.CodeName}.uasset");
            string hidUexpPath = Path.Combine(contentFolderPath, $"HID_{CurrentSkin.CodeName}.uexp");
            File.WriteAllBytes(hidUassetPath, Convert.FromBase64String
            (CurrentSkin.Gender == "Male" ? CurrentFnVersion.HidMaleUassetBase64 : CurrentFnVersion.HidFemaleUassetBase64));
            File.WriteAllBytes(hidUexpPath, Convert.FromBase64String
            (CurrentSkin.Gender == "Male" ? CurrentFnVersion.HidMaleUexpBase64 : CurrentFnVersion.HidFemaleUexpBase64));

            var currentHid = new UAsset(hidUassetPath, CurrentUeVersion.UassetApiEngineVer);
            var hidExport0 = (NormalExport)currentHid.Exports[0];
            hidExport0.ObjectName.Value.Value = $"HID_{CurrentSkin.CodeName}";
            var hidSmallIcon = (SoftObjectPropertyData)hidExport0["SmallPreviewImage"];
            var hidLargeIcon = (SoftObjectPropertyData)hidExport0["LargePreviewImage"];
            hidSmallIcon.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/Textures/{CurrentSkin.SmallIcon}.{CurrentSkin.SmallIcon}";
            Console.WriteLine($"Changed the Small Icon path in HID_{CurrentSkin.CodeName} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/Textures/{CurrentSkin.SmallIcon}.{CurrentSkin.SmallIcon}");
            hidLargeIcon.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/Textures/{CurrentSkin.LargeIcon}.{CurrentSkin.LargeIcon}";
            Console.WriteLine($"Changed the Large Icon path in HID_{CurrentSkin.CodeName} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/Textures/{CurrentSkin.LargeIcon}.{CurrentSkin.LargeIcon}");
            var hidSpecializationsArray = (ArrayPropertyData)hidExport0["Specializations"];
            var hidSpecialization = (SoftObjectPropertyData)hidSpecializationsArray.Value[0];
            hidSpecialization.Value.AssetPath.AssetName.Value.Value =
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/HS_{CurrentSkin.CodeName}.HS_{CurrentSkin.CodeName}";
            Console.WriteLine($"Changed the Hero Specialization path in HID_{CurrentSkin.CodeName} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/HS_{CurrentSkin.CodeName}.HS_{CurrentSkin.CodeName}");
            var idleMontage = (SoftObjectPropertyData)hidExport0["FrontendAnimMontageIdleOverride"];
            idleMontage.Value.AssetPath.AssetName.Value.Value = 
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/Animations/{CurrentSkin.CodeName}_Idle_Montage.{CurrentSkin.CodeName}_Idle_Montage";

            currentHid.Write(hidUassetPath);
            Log.Success($"Successfuly edited HID_{CurrentSkin.CodeName}.uasset and HID_{CurrentSkin.CodeName}.uexp");

            //CID creation
            Console.WriteLine($"Editing {CurrentSkin.CID}.uasset");
            string cidPath = Path.Combine(OutputFnGamePath, "Content", "Athena", "Items",
            "Cosmetics", "Characters");
            if (!Path.Exists(cidPath)) Directory.CreateDirectory(cidPath);
            string cidUassetPath = Path.Combine(cidPath, $"{CurrentSkin.CID}.uasset");
            string cidUexpPath = Path.Combine(cidPath, $"{CurrentSkin.CID}.uexp");
            File.WriteAllBytes(cidUassetPath, Convert.FromBase64String(CurrentFnVersion.CidUassetBase64));
            File.WriteAllBytes(cidUexpPath, Convert.FromBase64String(CurrentFnVersion.CidUexpBase64));

            var currentCid = new UAsset(cidUassetPath, CurrentUeVersion.UassetApiEngineVer);
            var cidExport0 = (NormalExport)currentCid.Exports[0];
            var cidImport = currentCid.Imports;
            cidImport[CurrentFnVersion.HidNameIndex].ObjectName.Value.Value = $"HID_{CurrentSkin.CodeName}";
            Console.WriteLine($"Changed the Hero Id in {CurrentSkin.CID} to HID_{CurrentSkin.CodeName}");
            cidImport[CurrentFnVersion.HidPathIndex].ObjectName.Value.Value = $"/Game/CustomSkins/{CurrentSkin.CodeName}/HID_{CurrentSkin.CodeName}";
            Console.WriteLine($"Changed the Hero Id path in {CurrentSkin.CID} to " +
            $"/Game/CustomSkins/{CurrentSkin.CodeName}/HID_{CurrentSkin.CodeName}");

            cidExport0.ObjectName.Value.Value = CurrentSkin.CID;
            var rarity = (EnumPropertyData)cidExport0["Rarity"];
            rarity.Value.Value.Value = $"EFortRarity::{CurrentSkin.Rarity}";

            if (CurrentSkin.Rarity == "Uncommon") cidExport0.Data.RemoveAt(1); //Removes the rarity property since no rarity is equal to uncommon in fn
            else if (CurrentSkin.Rarity == "Unattainable (Impossible T7)") rarity.Value.Value.Value = $"EFortRarity::Unattainable";
            if ((App.Settings.FnVersion == "8.51-9.10" || App.Settings.FnVersion == "9.41") && CurrentSkin.Rarity != "Uncommon")
            {
                string rarityCodename = "";
                if (CurrentSkin.Rarity == "Common") rarityCodename = "Handmade";
                else if (CurrentSkin.Rarity == "Rare") rarityCodename = "Sturdy";
                else if (CurrentSkin.Rarity == "Epic") rarityCodename = "Quality";
                else if (CurrentSkin.Rarity == "Legendary") rarityCodename = "Fine";
                else if (CurrentSkin.Rarity == "Mythic") rarityCodename = "Elegant";
                else if (CurrentSkin.Rarity == "Transcendent") rarityCodename = "Masterwork";
                else if (CurrentSkin.Rarity == "Unattainable (Impossible T7)") rarityCodename = "Epic";
                rarity.Value.Value.Value = $"EFortRarity::{rarityCodename}";
            }

            Console.WriteLine($"Changed the Rarity in {CurrentSkin.CID} to {CurrentSkin.Rarity}");
            ((TextPropertyData)cidExport0["DisplayName"]).CultureInvariantString.Value = CurrentSkin.Name;
            Console.WriteLine($"Changed the DisplayName in {CurrentSkin.CID} to {CurrentSkin.Name}");
            ((TextPropertyData)cidExport0["Description"]).CultureInvariantString.Value = CurrentSkin.Description;
            Console.WriteLine($"Changed the Description in {CurrentSkin.CID} to {CurrentSkin.Description}");
            string displayNameKey = Guid.NewGuid().ToString("N").ToUpper(); //Generates a new key for the display name since multiple display names can't use the same key
            string descriptionKey = Guid.NewGuid().ToString("N").ToUpper();
            ((TextPropertyData)cidExport0["DisplayName"]).Value.Value = displayNameKey;
            ((TextPropertyData)cidExport0["Description"]).Value.Value = descriptionKey;
            cidExport0.Data.RemoveAt(CurrentSkin.Rarity == "Uncommon" ? 4 : 5); //Removes gameplay tags

            if (CurrentSkin.Series == "None") cidExport0.Data.RemoveAt(CurrentSkin.Rarity == "Uncommon" ? 5 : 6);
            else
            {
                cidImport[3].ObjectName.Value.Value = SeriesCodenames.GetValueOrDefault(CurrentSkin.Series);
                cidImport[5].ObjectName.Value.Value = $"/Game/Athena/Items/Cosmetics/Series/{SeriesCodenames.GetValueOrDefault(CurrentSkin.Series)}";
            }
            Console.WriteLine($"Changed the Series in {CurrentSkin.CID} to {CurrentSkin.Series}");

            currentCid.Write(cidUassetPath);
            Log.Success($"Successfuly edited {CurrentSkin.CID}.uasset");

            //Idle Montage creation
            if (string.IsNullOrEmpty(CurrentSkin.LobbyAnimationPsa)) return;
            string idleAnimationUassetPath = Path.Combine(contentFolderPath, "Animations", $"{CurrentSkin.CodeName}_Idle_Montage.uasset");
            string idleAnimationUexpPath = Path.Combine(contentFolderPath, "Animations", $"{CurrentSkin.CodeName}_Idle_Montage.uexp");

            File.WriteAllBytes(idleAnimationUassetPath, Convert.FromBase64String(CurrentFnVersion.IdleMontageUassetBase64));
            File.WriteAllBytes(idleAnimationUexpPath, Convert.FromBase64String(CurrentFnVersion.IdleMontageUexpBase64));

            var currentIdleAnimation = new UAsset(idleAnimationUassetPath, CurrentUeVersion.UassetApiEngineVer);
            Console.WriteLine($"Editing {CurrentSkin.CodeName}_Idle_Montage.uasset");

            var idleAnimationImport = currentIdleAnimation.Imports;
            var idleAnimationExport0 = (NormalExport)currentIdleAnimation.Exports[0];

            idleAnimationExport0.ObjectName.Value.Value = $"{CurrentSkin.CodeName}_Idle_Montage";
            idleAnimationImport[1].ObjectName.Value.Value = $"{CurrentSkin.CodeName}_Lobby_Animation";
            idleAnimationImport[1].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation name in {CurrentSkin.CodeName}_Idle_Montage to {CurrentSkin.CodeName}_Lobby_Animation");
            idleAnimationImport[3].ObjectName.Value.Value = $"/Game/CustomSkins/{CurrentSkin.CodeName}/Animations/{CurrentSkin.CodeName}_Lobby_Animation";
            idleAnimationImport[3].ObjectName.Number = 0;
            Console.WriteLine($"Changed the animation path in {CurrentSkin.CodeName}_Idle_Montage to /Game/CustomSkins/{CurrentSkin.CodeName}/Animations/{CurrentSkin.CodeName}_Lobby_Animation");

            var slotAnimTracks = (ArrayPropertyData)idleAnimationExport0["SlotAnimTracks"];
            var slotAnimTracks2 = (StructPropertyData)slotAnimTracks.Value[0];
            var AnimTrack = (StructPropertyData)slotAnimTracks2.Value[1];
            var AnimSegments = (ArrayPropertyData)AnimTrack.Value[0];
            var AnimSegments2 = (StructPropertyData)AnimSegments.Value[0];
            var AnimEndTime = (FloatPropertyData)AnimSegments2.Value[3];
            AnimEndTime.Value = (float)Math.Round(CurrentSkin.LobbyAnimationLength, 5);
            Console.WriteLine($"Changed the animation length in {CurrentSkin.CodeName}_Idle_Montage to {Math.Round(CurrentSkin.LobbyAnimationLength, 5)}");
            if (string.IsNullOrEmpty(CurrentSkin.LobbyAnimationJson)) idleAnimationExport0.Data.RemoveAt(5); // Remove DisableFaceOverride if no .json is provided
                                                                                                             // since there is no way to get the idle pose's facial animations
            currentIdleAnimation.Write(idleAnimationUassetPath);
            Log.Success($"Successfuly edited {CurrentSkin.CodeName}_Idle_Montage.uasset");
        }

        private SkinData LoadSkinConfig(string jsonPath)
        {
            string filePath = jsonPath;

            if (!File.Exists(filePath)) return null;

            string jsonString = File.ReadAllText(filePath);
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
                Log.Error(ex.ToString());
            }

            return loadedSkin;
        } // TODO: This method should only load the data that doesn't have JsonIgnore, right now it loads everything from the .json 
                                                            // as a new SkinData object, so everything that had JsonIgnore has the default value assigned in the class, so to avoid 
                                                            // getting null objects, reassigning the variables that had JsonIgnore is mandatory at the moment.

        public void SaveSkinConfig()
        {
            if (CurrentSkin == null || string.IsNullOrEmpty(CurrentSkin.Path)) return;

            string jsonPath = Path.Combine(CurrentSkin.Path, $"{CurrentSkin.CodeName}_Settings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(CurrentSkin, options);

            File.WriteAllText(jsonPath, jsonString);
        } 
    }

    public class CharacterPart
    {
        public string Type { get; set; }
        public string PskPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string FbxPath { get; set; } = string.Empty;
        public List<string> PhysicsAssetJsonPaths { get; set; } = new();

        [System.Text.Json.Serialization.JsonIgnore]
        public string uassetFileBase64 { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string uexpFileBase64 { get; set; } = string.Empty;
    }

    public class SkinData : INotifyPropertyChanged
    {
        public string CodeName { get; set; } = string.Empty;
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
        public string SourcePath { get; set; } = string.Empty;
        public string MeshesPath { get; set; } = string.Empty;
        public string TexturesPath { get; set; } = string.Empty;
        public string PhysicsPath { get; set; } = string.Empty;
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

                Console.WriteLine($"New sbc Red is {value}");
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

                Console.WriteLine($"New sbc Green is {value}");
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

                Console.WriteLine($"New sbc Blue is {value}");
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

                Console.WriteLine($"New sbc Alpha is {value}");
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

    public class BlenderExportData
    {
        public string[] Psks { get; set; }
        public List<string> Textures { get; set; }
        public List<bool> Swizzle { get; set; }
        public List<string> Materials { get; set; }
        public string RenderPath { get; set; }
        public string LobbyAnimPath { get; set; }
        public string HeadPsk { get; set; }
    }

    [JsonSerializable(typeof(BlenderExportData))]

    [JsonSerializable(typeof(UnrealExportData))]
    internal partial class AppJsonContext : JsonSerializerContext { }
}