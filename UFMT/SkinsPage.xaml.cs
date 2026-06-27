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

namespace UFMT
{
    public sealed partial class SkinsPage : Page, INotifyPropertyChanged
    {
        public static FnVersion CurrentFnVersion = FnVersionsData.FnVersions.GetValueOrDefault(App.Settings.FnVersion);
        public static UeVersion CurrentUeVersion = UeVersionsData.UeVersions.GetValueOrDefault(App.Settings.UeVersion);
        private static string PskConvertScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_ConvertPsk.py");
        private static string PsaConvertScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_ConvertPsa.py");
        private static string RenderScript = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", "Blender_RenderPreview.py");
        private static string PhysicsImporterPath;
        private static string CookedAssetsPath;
        private static string ValidCodenameCharacters = "abcdefghijklmnopqrstuvwxyz1234567890_";
        private static string CurrentSkinPath = string.Empty;
        private static string MaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Male_Commando_Idle_01.psa");
        private static string FemaleLobbyAnimPath = Path.Combine
        (AppDomain.CurrentDomain.BaseDirectory, "Assets", "LobbyAnimations", "Female_Commando_Idle_01.psa");
        private CancellationTokenSource _currentSkinPathDebounce;
        public Dictionary<string, string> SeriesCodenames = new(){ {"Dark Series", "CUBESeries"}, { "Star Wars Series", "ColumbusSeries" },
        {"Icon Series", "CreatorCollabSeries"}, {"DC Series", "DCUSeries"}, {"Frozen Series", "FrozenSeries" }, {"Lava Series", "LavaSeries"},
        {"Marvel Series", "MarvelSeries"}, {"Shadow Series", "ShadowSeries"},  {"Slurp Series", "SlurpSeries"},  
        {"Test Series", "FakeToken_FDS_Series"}, {"Anual Pass Series", "2020AnnualPassSeries"}};
        private static string OutputFnGamePath = string.Empty;
        private static Dictionary<string, int[]> DefaultTexturesColors = new() //The colors are in ARGB
        {
            {"Default_Diffuse", [255, 228, 228, 228]},
            {"Default_Mask", [255, 252, 172, 0]},
            {"Default_Normal", [255, 124, 130, 254]},
            {"Default_Specular", [255, 0, 0, 0]},
        };
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
            try
            {
                CurrentSkinPath = CurrentSkinPathBox.Text;
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.ToString());
            }
            OutputFnGamePath = Path.Combine(CurrentSkinPath, "Output", App.Settings.FnVersion, "FortniteGame");
            ResetCpData();

            if (CurrentSkinPath == string.Empty)
            {   
                if (sender as string != "NoDelay") ConsoleWriteLineError("The Current skin path is empty!");
                return;
            }
            if (!Directory.Exists(CurrentSkinPath))
            {
                ConsoleWriteLineError($"{CurrentSkinPath} doesn't exist!");
                return;
            }
            CurrentSkin.SourcePath = Path.Combine(CurrentSkinPath, "Source");
            if (!Directory.Exists(CurrentSkin.SourcePath))
            {
                ConsoleWriteLineError($"Cannot find the Source folder inside {CurrentSkinPath}");
                return;
            }
            CurrentSkin.MeshesPath = Path.Combine(CurrentSkin.SourcePath, "Meshes");
            if (!Directory.Exists(CurrentSkin.MeshesPath))
            {
                ConsoleWriteLineError($"Cannot find the Meshes folder inside {CurrentSkin.SourcePath}");
                return;
            }

            try
            {
                string codeName = new DirectoryInfo(CurrentSkinPath).Name;
                SkinData loadedJson = LoadSkinConfig(Path.Combine(CurrentSkinPath, $"{codeName}_Settings.json"));

                if (loadedJson != null)
                {
                    CurrentSkin = loadedJson;
                    characterCIDTextBox.Text = CurrentSkin.CID;
                }
                else
                {
                    var result = GetPskData();
                    if (!result.Success) { ConsoleWriteLineError(result.ErrorMsg); UpdateDropdowns(); return; }

                    result = GetValidTextures();
                    if (!result.Success) { ConsoleWriteLineError(result.ErrorMsg); UpdateDropdowns(); return; }
                }
                CurrentSkin.PropertyChanged += (s, e) => SaveSkinConfig();
                UpdateDropdowns();
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.ToString());
            }
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
                ConsoleWriteLineError(ex.Message);
            }
        }

        private async void RenderButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(App.Settings.UeVersion))
            {
                ConsoleWriteLineError($"No unreal engine selected! Make sure you selected the correct ue version in setting!");
                return;
            }

            if (string.IsNullOrEmpty(CurrentSkin.Gender))
            {
                ConsoleWriteLineError($"The skin's gender is unspecified");
                return;
            }
            RenderPreviewImage();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(App.Settings.UeVersion))
            {
                ConsoleWriteLineError($"No unreal engine selected! Make sure you selected the correct ue version in setting!");
                return;
            }
            if (string.IsNullOrEmpty(CurrentSkin.Gender))
            {
                ConsoleWriteLineError($"Skin's gender is unspecified!");
                return;
            }
            if (string.IsNullOrEmpty(CurrentSkin.Name))
            {
                ConsoleWriteLineError($"Skin's name cannot be empty!");
                return;
            }
            if (string.IsNullOrEmpty(CurrentSkin.Description))
            {
                ConsoleWriteLineError($"Skin's description cannot be empty!");
                return;
            }
            if (string.IsNullOrEmpty(CurrentSkin.CID))
            {
                ConsoleWriteLineError($"Skin's CID cannot be empty!");
                return;
            }
            try
            {
                ConvertPskToFbx();
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.Message);
            }
        }

        private async void CreateSkinFolder_Click
        (object sender, RoutedEventArgs e)
        {
            CreateFolderDialog.XamlRoot = this.Content.XamlRoot;

            if (SkinsPathBox.Text == null || SkinsPathBox.Text == "")
            {
                ConsoleWriteLineError("The skins path cannot be empty!");
                return;
            }
            else if (!Directory.Exists(SkinsPathBox.Text))
            {
                ConsoleWriteLineError($"\"{SkinsPathBox.Text}\" doesn't exist!");
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
                ConsoleWriteLineError("The skins path cannot be empty!");
                return;
            }
            else if (!Directory.Exists(SkinsPathBox.Text))
            {
                ConsoleWriteLineError($"\"{SkinsPathBox.Text}\" doesn't exist!");
                return;
            }

            if (CodenameFolderCreateTextBox.Text.Length > 30)
            {
                ConsoleWriteLineError("The codename cannot be longer than 30 characters!");
                return;
            }

            foreach (char c in CodenameFolderCreateTextBox.Text)
            {
                if (!ValidCodenameCharacters.Contains(c.ToString().ToLower()))
                {
                    ConsoleWriteLineError("The codename can only contain alphabetical characters, " +
                    "numbers and _");
                    invalidCodename = true;
                    return;
                }
            }

            Directory.CreateDirectory(Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text));
            ConsoleWriteLineSuccess($"Successfully created {CodenameFolderCreateTextBox.Text} folder at " +
            $"{SkinsPathBox.Text}");
            
            string[] cpTypes = {"Body", "Head", "Faceacc", "Hat" };
            string[] cpTypeFolders = {"Meshes", "Physics" };
            foreach (string cpType in cpTypes)
            {
                foreach (string cpTypeFolder in cpTypeFolders)
                {
                    Directory.CreateDirectory(Path.Combine
                    (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", cpTypeFolder, cpType));
                    ConsoleWriteLineSuccess($"Successfully created {cpType} folder at " +
                    $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", cpTypeFolder)}");
                }
            }

            Directory.CreateDirectory(Path.Combine
            (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Textures"));
            ConsoleWriteLineSuccess($"Successfully created Textures folder at " +
            $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source")}");

            Directory.CreateDirectory(Path.Combine
            (SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source", "Lobby_Animation"));
            ConsoleWriteLineSuccess($"Successfully created Lobby_Animation folder at " +
            $"{Path.Combine(SkinsPathBox.Text, CodenameFolderCreateTextBox.Text, "Source")}");

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
                else if (c.Tag.ToString() == "series")
                {
                    string fullPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Icon_Background.png";
                    iconBackgroundOverlay.Source = new BitmapImage(new Uri(fullPath));

                    if (c.SelectedItem.ToString() == "None")
                    {
                        fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Icon.png";
                        iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                        fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Text.png";
                        textOverlay.Source = new BitmapImage(new Uri(fullPath));
                    }

                    else
                    {
                        fullPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Icon.png";
                        iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                        fullPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Text.png";
                        textOverlay.Source = new BitmapImage(new Uri(fullPath));
                    }

                }
                else if (c?.Tag.ToString() == "rarity" && c?.SelectedItem != null && seriesComboBox?.SelectedItem != null)
                {
                    if (CurrentSkin.Series != "None") return;
                    string fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Icon.png";
                    iconOverlay.Source = new BitmapImage(new Uri(fullPath));

                    fullPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Text.png";
                    textOverlay.Source = new BitmapImage(new Uri(fullPath));
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
                    if (CurrentSkin.Series == "None")
                    {
                        imgPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Icon.png";
                        iconOverlay.Source = new BitmapImage(new Uri(imgPath));

                        imgPath = $"ms-appx:///Assets/{CurrentSkin.Rarity}_Text.png";
                        textOverlay.Source = new BitmapImage(new Uri(imgPath));
                    }

                    else
                    {
                        imgPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Icon.png";
                        iconOverlay.Source = new BitmapImage(new Uri(imgPath));

                        imgPath = $"ms-appx:///Assets/{CurrentSkin.Series}_Text.png";
                        textOverlay.Source = new BitmapImage(new Uri(imgPath));
                    }

                    if (!CurrentSkin.Materials.Any(mat => !mat.Swizzle) && CurrentSkin.Materials.Count > 0) AllSwizzleCheckBoxValue = true;
                    else { IsUpdatingFromCode = true; AllSwizzleCheckBoxValue = false; IsUpdatingFromCode = false; };
                }
                DynamicExpanderList.LayoutUpdated += OnLayoutUpdated;
                ConsoleWriteLineSuccess("Updated the dropdowns!");
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.ToString());
            }
        }

        public void ConsoleWriteLineError(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void ConsoleWriteLineSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void ConsoleWriteLineWarning(string message)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void ConsoleWriteLineTest(string message)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void LoadContent()
        {
            CurrentFnVersion = FnVersionsData.FnVersions.GetValueOrDefault(App.Settings.FnVersion);
            CurrentUeVersion = UeVersionsData.UeVersions.GetValueOrDefault(App.Settings.UeVersion);
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

            if (string.IsNullOrEmpty(App.Settings.UeProjectPath)) { ConsoleWriteLineError("Unreal Engine Project path is empty!"); return; }
            if (!Path.Exists(App.Settings.UeProjectPath)) { ConsoleWriteLineError($"{App.Settings.UeProjectPath} doesn't exist!"); return; }

            CookedAssetsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath),
            "Saved", "Cooked", "WindowsNoEditor", new DirectoryInfo(Path.GetDirectoryName(App.Settings.UeProjectPath)).Name, "Content");

            string pluginsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Plugins", "PhysicsImporter");
            if (!Path.Exists(pluginsPath)) ZipFile.ExtractToDirectory(PhysicsImporterPath, pluginsPath);

            CurrentSkinPathBox_TextChanged("NoDelay", null);
        }

        private (bool Success, string ErrorMsg) GetPskData()
        {
            if (string.IsNullOrEmpty(CurrentSkinPath)) return (false, "CurrentSkinPath is empty!");
            List<string> pskPaths = new();
            List<string> alreadyUsedMaterials = new List<string>();
            List<CharacterPart> allCharacterParts = new() { Body, Head, FaceAcc, Hat };
            CurrentSkin.CodeName = new DirectoryInfo(CurrentSkinPath).Name;
            CurrentSkin.CID = $"CID_{CurrentSkin.CodeName}";
            characterCIDTextBox.Text = CurrentSkin.CID;
            CurrentSkin.TexturesPath = Path.Combine(CurrentSkin.SourcePath, "Textures");
            CurrentSkin.LobbyAnimationPath = Path.Combine(CurrentSkin.SourcePath, "Lobby_Animation");
            CurrentSkin.PhysicsPath = Path.Combine(CurrentSkin.SourcePath, "Physics");

            foreach (string meshFolder in Directory.GetDirectories(CurrentSkin.MeshesPath))
            {
                string[] pskFiles = Directory.GetFiles(meshFolder, "*.psk");
                if (pskFiles.Length == 1)
                {
                    Console.WriteLine($"{Path.GetFileName(pskFiles[0])} is a {Path.GetFileName(meshFolder)} CharactePart Type!");
                    pskPaths.Add(pskFiles[0]);
                }
                else if (pskFiles.Length > 1)
                {
                    ConsoleWriteLineError($"{meshFolder} contains more than 1 .psk files! Cannot get the correct {Path.GetFileName(meshFolder)} " +
                    $"Character Part Type!");
                }
            }

            foreach (string pskPath in pskPaths)
            {
                CharacterPart currentCp = allCharacterParts.FirstOrDefault(cp => cp.Type ==
                Path.GetFileName(Path.GetDirectoryName(pskPath)).ToLower());
                List<string> currentPskMaterials = new();

                using var r = new BinaryReader(File.OpenRead(pskPath));

                while (r.BaseStream.Position < r.BaseStream.Length)
                {
                    string id = Encoding.ASCII.GetString(r.ReadBytes(24)).Trim();
                    int sz = r.ReadInt32();
                    int ct = r.ReadInt32();
                    if (id.Contains("MATT0000"))
                    {
                        for (int i = 0; i < ct; i++)
                        {
                            string mat = Encoding.ASCII.GetString(r.ReadBytes(64)).Trim('\0').Trim();
                            currentPskMaterials.Add(mat);
                            r.BaseStream.Seek(sz - 64, SeekOrigin.Current);
                        }
                    }
                    else
                    {
                        r.BaseStream.Seek((long)sz * ct, SeekOrigin.Current);
                    }
                }
                foreach (string mat in currentPskMaterials)
                {
                    if (!alreadyUsedMaterials.Contains(mat))
                    {
                        CurrentSkin.Materials.Add(new Material()
                        {
                            Name = mat,
                            ParentPage = this,
                            Cp = currentCp,
                            Swizzle = allSwizzleCheckBox.IsChecked ?? false
                        });
                        alreadyUsedMaterials.Add(mat);
                    }
                }
                currentCp.PskPath = pskPath;

                List<string> jsonFiles = Directory.GetFiles(Path.Combine(CurrentSkin.PhysicsPath, currentCp.Type[0].ToString().ToUpper() + currentCp.Type.Substring(1)), "*.json").ToList();
                jsonFiles.ForEach(json => { currentCp.PhysicsAssetJsonPaths.Add(json); Console.WriteLine
                ($"Added {Path.GetFileNameWithoutExtension(json)} to {Path.GetFileNameWithoutExtension(pskPath)}"); });
                CurrentSkin.CharacterParts.Add(currentCp);
                ConsoleWriteLineSuccess($"{Path.GetFileName(pskPath)} is a {currentCp.Type} character part type!");
            }

            if (string.IsNullOrEmpty(Body.PskPath))
            {
                return (false, $"Cannot find a body .psk file in {CurrentSkin.SourcePath}\nThe character must have at least a body and a head!");
            }
            if (string.IsNullOrEmpty(Head.PskPath))
            {
                return (false, $"Cannot find a head .psk file in {CurrentSkin.SourcePath}\nThe character must have at least a body and a head!");
            }

            try
            {
                string[] lobbyAnimationFiles = Directory.GetFiles(CurrentSkin.LobbyAnimationPath, "*.psa");
                if (lobbyAnimationFiles.Length > 1)
                    return (false, $"Multiple .psa files in {CurrentSkin.LobbyAnimationPath}!\nMake sure there is only 1 .psa lobby animation!");
                if (lobbyAnimationFiles.Length != 0)
                {
                    CurrentSkin.LobbyAnimationPsa = Path.GetFileNameWithoutExtension(lobbyAnimationFiles[0]);
                    ConsoleWriteLineSuccess($"The lobby animation is {CurrentSkin.LobbyAnimationPsa}.psa");
                }
            }
            catch (DirectoryNotFoundException)
            {
                return (true, string.Empty);
            }

            return (true, string.Empty);
        }

        private void ResetCpData()
        {
            CurrentSkin = new SkinData();
            UpdateDropdowns();
        }

        public async void ConvertPskToFbx()
        {
            Console.WriteLine("Converting .psk files to .fbx");

            foreach (CharacterPart cp in CurrentSkin.CharacterParts)
            {
                string exportFbxPath = Path.Combine(CurrentSkin.SourcePath, "Fbx", cp.Type[0].ToString().ToUpper() + cp.Type.Substring(1));
                if (!Directory.Exists(exportFbxPath))
                {
                    Directory.CreateDirectory(exportFbxPath);
                    Console.WriteLine($"Created {exportFbxPath}");
                }
                string exportName = $"{CurrentSkin.CodeName}_{cp.Type}";

                await Task.Run(() =>
                {
                    Process blender = Process.Start(App.Settings.BlenderPath, $"-b --python \"{PskConvertScript}\" -- \"{cp.PskPath}\" " +
                $"\"{Path.Combine(exportFbxPath, $"{exportName}.fbx")}\"");
                    blender.WaitForExit();
                });
                
                ConsoleWriteLineSuccess($"Succesfully converted " +
                $"{Path.Combine(CurrentSkin.SourcePath, Path.GetFileName(cp.PskPath))}" +
                $" to {Path.Combine(CurrentSkin.SourcePath, "Fbx", exportName)}.fbx!");
                cp.FbxPath = Path.Combine(exportFbxPath, exportName);
            }
            ConvertPsaToFbx();
        }

        public async void ConvertPsaToFbx()
        {
            Console.WriteLine("Converting .psa Lobby animation to .fbx");
            await Task.Run(() =>
            {
                string blendFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "proper_fn_skeleton.blend");
                string exportFbxPath = Path.Combine(CurrentSkin.SourcePath, "Fbx", "Lobby_Animation");
                if (!Directory.Exists(exportFbxPath)) Directory.CreateDirectory(exportFbxPath);
                string exportName = $"{CurrentSkin.CodeName}_Lobby_Animation";
                CurrentSkin.LobbyAnimationFbx = exportName;
                string psaPath = Path.Combine(CurrentSkin.LobbyAnimationPath, $"{CurrentSkin.LobbyAnimationPsa}.psa");
                string fbxFullPath = Path.Combine(exportFbxPath, $"{exportName}.fbx");
                string arguments = $"-b \"{blendFilePath}\" --python \"{PsaConvertScript}\" -- \"{psaPath}\" \"{fbxFullPath}\"";

                ProcessStartInfo psi = new ProcessStartInfo(App.Settings.BlenderPath, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process blender = Process.Start(psi))
                {
                    var stdoutTask = Task.Run(() => blender.StandardOutput.ReadToEnd());
                    var stderrTask = Task.Run(() => blender.StandardError.ReadToEnd());
                    blender.WaitForExit();
                    Task.WhenAll(stdoutTask, stderrTask).Wait();
                }

                string metaPath = fbxFullPath + ".meta";
                if (File.Exists(metaPath))
                {
                    string content = File.ReadAllText(metaPath).Trim();
                    if (int.TryParse(content, out int animLength))
                    {
                        CurrentSkin.LobbyAnimationLength = (float)animLength/30; //Divide the animation length by 30 since it's in 30fps
                    }
                    File.Delete(metaPath);
                }
            });
            ConsoleWriteLineSuccess($"Successfully converted the Lobby .psa animation to .fbx!");
            try
            {
                LaunchUnrealScript();
            }
            catch (Exception ex) { ConsoleWriteLineError(ex.ToString()); }
        }

        public (bool Success, string ErrorMsg) GetValidTextures()
        {
            List<KeyValuePair<string, int[]>> missingDefaultTextures = DefaultTexturesColors.Where
            (tex => !Path.Exists(Path.Combine(CurrentSkin.TexturesPath, $"{tex.Key}.png"))).ToList();

            foreach (KeyValuePair<string, int[]> missingDefTex in missingDefaultTextures)
            {
                CreateDefaultTexture(Path.Combine(CurrentSkin.TexturesPath, $"{missingDefTex.Key}.png"),
                Color.FromArgb(missingDefTex.Value[0], missingDefTex.Value[1], missingDefTex.Value[2], missingDefTex.Value[3]));
                Console.WriteLine($"Created a default {missingDefTex.Key} texture");
            }

            if (!Directory.Exists(CurrentSkin.TexturesPath))
            {
                ConsoleWriteLineWarning($"{CurrentSkin.SourcePath}" +
                $" doesn't exist, the skin won't have any textures.");
                return (true, string.Empty);
            }

            if (CurrentFnVersion.ManuallySwizzleMaterials)
            {
                List<string> specularTextures = Directory.GetFiles(CurrentSkin.TexturesPath, "*_S.png").ToList();
                specularTextures.Add(Path.Combine(CurrentSkin.TexturesPath, "Default_Specular.png"));
                var swizzledFolder = Directory.CreateDirectory(Path.Combine(CurrentSkin.TexturesPath, "Swizzled"));
                swizzledFolder.Attributes |= System.IO.FileAttributes.Hidden;
                Parallel.ForEach(specularTextures, t => SwizzleTextures(t));
            }

            Console.WriteLine($"Searching for valid textures inside {CurrentSkin.TexturesPath}");

            CurrentSkin.Textures = Directory.GetFiles(CurrentSkin.TexturesPath, "*.png").ToList().Select(tex => Path.GetFileNameWithoutExtension(tex)).ToList();

            if (CurrentSkin.Textures.Count == 0)
            {
                ConsoleWriteLineWarning($"No .png files found inside {CurrentSkin.TexturesPath}, " +
                $"the skin won't have any textures.");
                return (true, string.Empty);
            }

            List<string> texturesLower = CurrentSkin.Textures.Select(tex => tex.ToLower()).ToList();

            string largeIcon = CurrentSkin.Textures.FirstOrDefault(tex =>
            (tex.ToLower().Contains("t_soldier") || tex.ToLower().Contains("t-soldier")) && 
            (tex.ToLower().EndsWith("_l") || tex.ToLower().EndsWith("-l")));

            string smallIcon = CurrentSkin.Textures.FirstOrDefault(tex =>
            (tex.ToLower().Contains("t_soldier") || tex.ToLower().Contains("t-soldier")) &&
            (!tex.ToLower().EndsWith("_l") && !tex.ToLower().EndsWith("-l")));

            List<string> validMatTextures = CurrentSkin.Textures.Where(tex => tex.EndsWith("_D") || tex.EndsWith("_M") ||
            tex.EndsWith("_N") || tex.EndsWith("_S")).ToList();

            if (largeIcon != null) CurrentSkin.LargeIcon = largeIcon;
            if (smallIcon != null) CurrentSkin.SmallIcon = smallIcon;

            if (string.IsNullOrEmpty(CurrentSkin.LargeIcon) && !string.IsNullOrEmpty(CurrentSkin.SmallIcon))
            {
                CurrentSkin.LargeIcon = CurrentSkin.SmallIcon;
                ConsoleWriteLineWarning("Cannot find the large icon, the small icon will be used for " +
                "large icon as well.");
            }
            else if (!string.IsNullOrEmpty(CurrentSkin.LargeIcon) && string.IsNullOrEmpty(CurrentSkin.SmallIcon))
            {
                CurrentSkin.SmallIcon = CurrentSkin.LargeIcon;
                ConsoleWriteLineWarning("Cannot find the small icon, the large icon will be used for " +
                " small icon as well.");
            }

            foreach (string tex in validMatTextures) FindTexturesMaterial(tex, false);

            if (!CurrentSkin.Materials.Any(mat => validMatTextures.Contains(mat.SelectedDiffuse) || validMatTextures.Contains(mat.SelectedMask) ||
            validMatTextures.Contains(mat.SelectedNormal) || validMatTextures.Contains(mat.SelectedSpecular))) validMatTextures.ForEach(t => FindTexturesMaterial(t, true));

            List<Material> materialsWithMissingTextures = CurrentSkin.Materials.Where(mat =>
            mat.SelectedDiffuse == "Default_Diffuse" && mat.SelectedMask == "Default_Mask" &&
            mat.SelectedNormal == "Default_Normal" && mat.SelectedSpecular == "Default_Specular").ToList();

            materialsWithMissingTextures.ForEach(mat => GuessMaterialsTextures(mat));

            return (true, string.Empty);
        }

        private void FindTexturesMaterial(string texture, bool useFallbackKeywords)
        {
            int textureIndex = 0;
            string findFallbackKeyword(string input, string keyword)
            {
                string name = input.ToLower();
                int currentIndex = 0;
                int firstIndex = -1;
                int lastIndex = -1;
                int keywordIndex = 0;
                for (int i = 0; i < name.Length; i++)
                {
                    if (name[i] == keyword[keywordIndex])
                    {
                        if (keywordIndex == 0) firstIndex = i;
                        keywordIndex++;
                    }
                    else
                    {
                        firstIndex = -1;
                        keywordIndex = 0;
                    }
                    if (keywordIndex == keyword.Length)
                    {
                        lastIndex = i;
                        if ((firstIndex == 0 || name[firstIndex - 1] == '_') && (lastIndex == name.Length-1 || name[lastIndex + 1] == '_'))
                        {
                            return keyword;
                        }
                        else
                        {
                            firstIndex = -1;
                            lastIndex = -1;
                            keywordIndex = 0;
                        }
                    }
                }
                return null;
            }
            void applyTexture(Material material, string texture)
            {
                if (texture.EndsWith("_D")) material.SelectedDiffuse = texture;
                if (texture.EndsWith("_M")) material.SelectedMask = texture;
                if (texture.EndsWith("_N")) material.SelectedNormal = texture;
                if (texture.EndsWith("_S")) material.SelectedSpecular = texture;
            }

            if (useFallbackKeywords)
            {
                List<string> fallbackKeywords = new() { "body", "head", "faceacc", "eyes", "hair" };
                Dictionary<string, string> fallBackKeywordPairs = new() { {"head", "eyes" }, { "faceacc", "hair" }};
                string textureKeyword = fallbackKeywords.FirstOrDefault(keyword => findFallbackKeyword(texture, keyword) != null);
                if (textureKeyword == null) return;

                foreach (Material mat in CurrentSkin.Materials)
                {
                    string matName = mat.Name;
                    string matFallBackKeyword = fallbackKeywords.FirstOrDefault(keyword => findFallbackKeyword(matName, keyword) != null);
                    if (matFallBackKeyword != null && (matFallBackKeyword == textureKeyword || fallBackKeywordPairs.GetValueOrDefault(matFallBackKeyword) == textureKeyword))
                    {
                        applyTexture(mat, texture);
                        return;
                    }
                }
                return;
            }
            else
            {
                string textureKeyword = texture.Substring(0, texture.Length - 2).Replace(CurrentSkin.CodeName, "").ToLower();
                string previousSearchElement = textureKeyword + "|";

                while (previousSearchElement != textureKeyword)
                {
                    previousSearchElement = textureKeyword;
                    textureKeyword =
                    textureKeyword.StartsWith("t_") ? textureKeyword.Remove(0, 2) : textureKeyword;
                    textureKeyword = textureKeyword.StartsWith($"{CurrentSkin.CodeName.ToLower()}_") ?
                    textureKeyword.Remove(0, CurrentSkin.CodeName.Length + 1) : textureKeyword;
                    textureKeyword = textureKeyword.StartsWith($"f_med_") ||
                    textureKeyword.StartsWith("m_med_") ?
                    textureKeyword.Remove(0, 6) : textureKeyword;
                }
                foreach (string splitName in Regex.Split(CurrentSkin.CodeName, @"(?<!^)(?=[A-Z])"))
                {
                    textureKeyword = textureKeyword.Replace(splitName.ToLower(), "");
                }

                foreach (Material material in CurrentSkin.Materials)
                {
                    string matForSearching = material.Name.Replace(CurrentSkin.CodeName, "").ToLower();
                    if (matForSearching.EndsWith(textureKeyword))
                    {
                        applyTexture(material, texture);
                        return;
                    }
                }
            }
        }

        private void GuessMaterialsTextures(Material mat)
        {
            Material workingMat = CurrentSkin.Materials.FirstOrDefault
            (m => m.SelectedDiffuse != "Default_Diffuse" && m.SelectedMask != "Default_Mask" && m.SelectedNormal != "Default_Normal" && 
            m.SelectedSpecular != "Default_Specular" && m.Cp == mat.Cp);
            //Get the first material that is the same character part type and has all the textures correctly assigned
            if (workingMat == null) return;

            mat.SelectedDiffuse = workingMat.SelectedDiffuse;
            mat.SelectedMask = workingMat.SelectedMask;
            mat.SelectedNormal = workingMat.SelectedNormal;
            mat.SelectedSpecular = workingMat.SelectedSpecular;
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
                Path.Combine(CurrentSkin.LobbyAnimationPath, $"{CurrentSkin.LobbyAnimationPsa}.psa");

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
                    RenderPath = Path.Combine(CurrentSkinPath, "Source", $"{CurrentSkin.CodeName}.png"),
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
                ConsoleWriteLineSuccess("Successfully Rendered the preview image!");

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
                }
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.Message);
            }

        }

        private async void LaunchUnrealScript()
        {
            try
            {
                List<string> meshNames = new();
                List<string> diffuseTexturePaths = new();
                List<string> maskTexturePaths = new();
                List<string> normalTexturePaths = new();
                List<string> specularTexturePaths = new();
                List<string> iconTexturePaths = new();

                if (CurrentSkin.SmallIcon != "")
                {
                    iconTexturePaths.Add(Path.Combine(CurrentSkin.SourcePath, "Textures", $"{CurrentSkin.SmallIcon}.png"));
                    iconTexturePaths.Add(Path.Combine(CurrentSkin.SourcePath, "Textures", $"{CurrentSkin.LargeIcon}.png"));
                }
                else iconTexturePaths = [CurrentSkin.SmallIcon, CurrentSkin.LargeIcon];
                string fakeCIDTemplatePath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
                "CID_Template.uasset");
                string BaseMeshSkeletonPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
                "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player_Skeleton.uasset");
                string BaseMeshPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath), "Content",
                "Characters", "Player", "Male", "Male_Avg_Base", "Fortnite_M_Avg_Player.uasset");
                string cookedCodeNamePath = Path.Combine(CookedAssetsPath, "CustomSkins", CurrentSkin.CodeName);

                if (!File.Exists(fakeCIDTemplatePath))
                {
                    File.WriteAllBytes(fakeCIDTemplatePath, Convert.FromBase64String(CurrentUeVersion.FakeCIDBase64));
                }
                if (!File.Exists(BaseMeshSkeletonPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(BaseMeshSkeletonPath));
                    File.WriteAllBytes(BaseMeshSkeletonPath, Convert.FromBase64String(CurrentUeVersion.BaseMeshSkeletonBase64));
                }
                if (!File.Exists(BaseMeshPath))
                {
                    File.WriteAllBytes(BaseMeshPath, Convert.FromBase64String(CurrentUeVersion.BaseMeshBase64));
                }

                if (Directory.Exists(cookedCodeNamePath))
                {
                    Directory.Delete(cookedCodeNamePath, true);
                }

                Console.WriteLine("Launching unreal engine...");

                foreach (Material mat in CurrentSkin.Materials)
                {
                    diffuseTexturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedDiffuse}.png"));
                    maskTexturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedMask}.png"));
                    normalTexturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedNormal}.png"));

                    if (CurrentFnVersion.ManuallySwizzleMaterials && mat.Swizzle)
                    {
                        specularTexturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, "Swizzled", $"{mat.SelectedSpecular}.png"));
                    }
                    else
                    {
                        specularTexturePaths.Add(Path.Combine(CurrentSkin.TexturesPath, $"{mat.SelectedSpecular}.png"));
                    }
                }
                Path.Combine(CurrentSkin.SourcePath, "Fbx", $"{CurrentSkin.LobbyAnimationFbx}.fbx");
                var unrealData = new UnrealExportData()
                {
                    FbxPaths = CurrentSkin.CharacterParts.Select(cp => $"{cp.FbxPath}.fbx").ToList(),
                    PhysicsMeshNames = CurrentSkin.CharacterParts.Where(cp => cp.PhysicsAssetJsonPaths.Count > 0).ToList().Select(cp => Path.GetFileNameWithoutExtension(cp.FbxPath)).ToList(),
                    PhysicsAssetsPaths = CurrentSkin.CharacterParts.Select(cp => cp.PhysicsAssetJsonPaths).ToList(),
                    DiffuseTextures = diffuseTexturePaths,
                    MaskTextures = maskTexturePaths,
                    NormalTextures = normalTexturePaths,
                    SpecularTextures = specularTexturePaths,
                    IconTextures = iconTexturePaths,
                    Materials = CurrentSkin.Materials.Select(mat => mat.Name).ToList(),
                    CodeName = CurrentSkin.CodeName,
                    MeshNames = CurrentSkin.CharacterParts.Select(cp => Path.GetFileNameWithoutExtension(cp.FbxPath)).ToList(),
                    CID = CurrentSkin.CID,
                    LobbyAnimationFbxPath = CurrentSkin.LobbyAnimationPsa == string.Empty ? string.Empty :
                    Path.Combine(CurrentSkin.SourcePath, "Fbx", "Lobby_Animation", $"{CurrentSkin.LobbyAnimationFbx}.fbx"),
                    RetargetSource = CurrentSkin.Gender == "Male" ? "MPR_SK_M_MALE_Base_Skeleton" : "SK_M_Female_Base_Skeleton"
                };

                string jsonString = System.Text.Json.JsonSerializer.Serialize(unrealData, AppJsonContext.Default.UnrealExportData);
                string tempJsonPath = Path.Combine(Path.GetTempPath(), "ue_import_data.json");
                File.WriteAllText(tempJsonPath, jsonString, new System.Text.UTF8Encoding(false));

                string scriptPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "PythonScripts", CurrentUeVersion.PythonScriptName).Replace("\\", "/");

                string arguments = $"\"{App.Settings.UeProjectPath}\" -run=PythonScriptCommandlet -script=\"{scriptPath}\" -NullRHI -NoWindow -Silent";

                Console.WriteLine($"Launching UE with args: {arguments}");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = App.Settings.UeExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = false, //This must be false for EnvironmentVariables to work
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = false
                };

                startInfo.EnvironmentVariables["UFMT_JSON_PATH"] = tempJsonPath;
                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                    await Task.WhenAll(stdoutTask, stderrTask);
                    await process.WaitForExitAsync();
                    Console.WriteLine(stdoutTask.Result);
                }

                Console.WriteLine("Done!");
                Console.WriteLine("Cooking the newly created assets...");
                await CookProject();
                Console.WriteLine("Done!");

                CurrentUeVersion.FixRequiredFiles(Path.Combine
                (cookedCodeNamePath, "Animations", $"{CurrentSkin.CodeName}_Lobby_Animation.uasset"), CurrentSkin.CharacterParts.Select
                (cp => Path.Combine(cookedCodeNamePath, "Meshes", $"{Path.GetFileNameWithoutExtension(cp.FbxPath)}.uasset")).ToArray());

                Console.WriteLine("Creating the CID.json for the AssetRegistry.bin");
                CreateAssetRegistry();
                CreateCharacterAssets();
                ConsoleWriteLineSuccess("\nYour custom skin is ready! Check the output folder");
            }
            catch (Exception ex)
            {
                ConsoleWriteLineError(ex.ToString());
            }
        }

        private void CreateDefaultTexture(string outputPath, Color color)
        {
            using (Bitmap bmp = new Bitmap(1, 1))
            {
                bmp.SetPixel(0, 0, color);
                bmp.Save(outputPath, ImageFormat.Png);
            }
        }

        private async Task CookProject()
        {
            string arguments = $"\"{App.Settings.UeProjectPath}\" -run=Cook -TargetPlatform=WindowsNoEditor -unversioned -iterate -NullRHI -NoWindow -Silent";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = App.Settings.UeExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();
                Console.WriteLine(stdoutTask.Result);
            }

            Console.WriteLine("Cook done!");
        }

        private void CreateAssetRegistry()
        {
            if (!Path.Exists(CookedAssetsPath))
            {
                CookedAssetsPath = Path.Combine(Path.GetDirectoryName(App.Settings.UeProjectPath),
                "Saved", "Cooked", "WindowsNoEditor", Path.GetFileNameWithoutExtension(App.Settings.UeProjectPath), "Content");
            }
            //Just in case the user has the project folder named differently than the .uproject

            string[] customSkinFolders = Directory.GetDirectories(Path.Combine(CookedAssetsPath, "CustomSkins"));
            List<string> cookedFakeCids = new();
            List<string> jsonCids = new();

            foreach (string customSkinFolder in customSkinFolders)
            {
                string[] cid = Directory.GetFiles(customSkinFolder, "*.uasset");
                if (cid.Length > 0)
                {
                    string currentFoundCid = Path.GetFileNameWithoutExtension(cid[0]);

                    string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(CurrentUeVersion.CidJsonBase64));
                    var root = JObject.Parse(json);
                    root["ObjectPath"] = root["ObjectPath"]!.Value<string>()!.Replace(
                        "/Game/Athena/Items/Cosmetics/Characters/CID_Template.CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}.{currentFoundCid}");
                    root["PackageName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "/Game/Athena/Items/Cosmetics/Characters/CID_Template",
                        $"/Game/Athena/Items/Cosmetics/Characters/{currentFoundCid}");
                    root["AssetName"] = root["PackageName"]!.Value<string>()!.Replace(
                        "CID_Template", currentFoundCid);
                    var tagAndValue = root["TagAndValue"]!.ToArray();
                    foreach (var tag in tagAndValue)
                    {
                        if (tag["Item1"]!.Value<string>() == "PrimaryAssetName")
                        {
                            tag["Item2"] = currentFoundCid;
                            break;
                        }
                    }
                    jsonCids.Add(root.ToString(Formatting.Indented));
                }
                else
                {
                    ConsoleWriteLineWarning($"no uasset files found inside {customSkinFolder}");
                }
            }

            string oldOutputPath = Path.Combine(CurrentSkinPath, "Output", "FortniteGame");
            if (Directory.Exists(oldOutputPath)) Directory.Delete(oldOutputPath, true);

            if (!Directory.Exists(OutputFnGamePath))
            {
                Directory.CreateDirectory(OutputFnGamePath);
                Console.WriteLine($"Created {OutputFnGamePath}!");
            }

            AssetRegistryHelper.Inject(CurrentUeVersion.AssetRegistryBinBase64, jsonCids.ToArray(), Path.Combine(OutputFnGamePath, "AssetRegistry312398E80AB6209B22CAA2EBAB2DB35B.bin"));
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
            ConsoleWriteLineSuccess($"Coppied files from {cookedCharacterDirectory} to {contentFolderPath}");
            if (!Path.Exists(characterPartsPath)) Directory.CreateDirectory(characterPartsPath);

            if (CurrentSkin.Gender == "Female")
            {
                body.uassetFileBase64 = CurrentFnVersion.BodyCpFemaleUassetBase64;
                body.uexpFileBase64 = CurrentFnVersion.BodyCpFemaleUexpBase64;
                head.uassetFileBase64 = CurrentFnVersion.HeadCpFemaleUassetBase64;
                head.uexpFileBase64 = CurrentFnVersion.HeadCpFemaleUexpBase64;
                faceacc.uassetFileBase64 = CurrentFnVersion.FaceAccCpFemaleUassetBase64;
                faceacc.uexpFileBase64 = CurrentFnVersion.FaceAccCpFemaleUexpBase64;
            }
            else if (CurrentSkin.Gender == "Male")
            {
                Body.uassetFileBase64 = CurrentFnVersion.BodyCpMaleUassetBase64;
                Body.uexpFileBase64 = CurrentFnVersion.BodyCpMaleUexpBase64;
                Head.uassetFileBase64 = CurrentFnVersion.HeadCpMaleUassetBase64;
                Head.uexpFileBase64 = CurrentFnVersion.HeadCpMaleUexpBase64;
                FaceAcc.uassetFileBase64 = CurrentFnVersion.FaceAccCpMaleUassetBase64;
                FaceAcc.uexpFileBase64 = CurrentFnVersion.FaceAccCpMaleUexpBase64;
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

                var currentCp = new UAsset(uassetPath, EngineVersion.VER_UE4_26);
                var cpExport0 = (NormalExport)currentCp.Exports[0];
                var cpExport1 = (NormalExport)currentCp.Exports[1];
                cpExport1.ObjectName.Value.Value = $"CP_{cp.Type}_{CurrentSkin.CodeName}";
                if (cp.Type != "hat")
                {
                    var animBpData = (SoftObjectPropertyData)cpExport0["AnimClass"];
                    animBpData.Value.AssetPath.AssetName.Value.Value =
                    $"/Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                    $"{CurrentSkin.CodeName}_{cp.Type}_AnimBP.{CurrentSkin.CodeName}_{cp.Type}_AnimBP_C";

                    Console.WriteLine($"Changed the Animation Blueprint in CP_{cp.Type}_{CurrentSkin.CodeName} to /Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                    $"{CurrentSkin.CodeName}_{cp.Type}_AnimBP.{CurrentSkin.CodeName}_{cp.Type}_AnimBP_C");
                }
                var mesh = (SoftObjectPropertyData)cpExport1["SkeletalMesh"];
                mesh.Value.AssetPath.AssetName.Value.Value = $"/Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                $"{CurrentSkin.CodeName}_{cp.Type}.{CurrentSkin.CodeName}_{cp.Type}";
                Console.WriteLine($"Changed the Mesh in CP_{cp.Type}_{CurrentSkin.CodeName} to /Game/CustomSkins/{CurrentSkin.CodeName}/Meshes/" +
                $"{CurrentSkin.CodeName}_{cp.Type}.{CurrentSkin.CodeName}_{cp.Type}");

                Console.WriteLine(uassetPath);
                currentCp.Write(uassetPath);
                ConsoleWriteLineSuccess($"Successfully edited CP_{cp.Type}_{CurrentSkin.CodeName}.uasset and " +
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
                ConsoleWriteLineSuccess($"Created material instance {material.Name}");
                Console.WriteLine($"Editing {material.Name}");

                var currentMi = new UAsset(uassetMaterialPath, EngineVersion.VER_UE4_26);
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
                ConsoleWriteLineSuccess($"Successfully edited {material.Name}.uasset and {material.Name}.uexp");
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

            var currentHs = new UAsset(Path.Combine(contentFolderPath, $"HS_{CurrentSkin.CodeName}.uasset"), EngineVersion.VER_UE4_26);
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
            ConsoleWriteLineSuccess($"Successfuly edited HS_{CurrentSkin.CodeName}.uasset and HS_{CurrentSkin.CodeName}.uexp");

            //HID creation
            Console.WriteLine("Editing HID...");
            string hidUassetPath = Path.Combine(contentFolderPath, $"HID_{CurrentSkin.CodeName}.uasset");
            string hidUexpPath = Path.Combine(contentFolderPath, $"HID_{CurrentSkin.CodeName}.uexp");
            File.WriteAllBytes(hidUassetPath, Convert.FromBase64String
            (CurrentSkin.Gender == "Male" ? CurrentFnVersion.HidMaleUassetBase64 : CurrentFnVersion.HidFemaleUassetBase64));
            File.WriteAllBytes(hidUexpPath, Convert.FromBase64String
            (CurrentSkin.Gender == "Male" ? CurrentFnVersion.HidMaleUexpBase64 : CurrentFnVersion.HidFemaleUexpBase64));

            var currentHid = new UAsset(hidUassetPath, EngineVersion.VER_UE4_26);
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
            ConsoleWriteLineSuccess($"Successfuly edited HID_{CurrentSkin.CodeName}.uasset and HID_{CurrentSkin.CodeName}.uexp");

            //CID creation
            Console.WriteLine($"Editing {CurrentSkin.CID}.uasset");
            string cidPath = Path.Combine(OutputFnGamePath, "Content", "Athena", "Items",
            "Cosmetics", "Characters");
            if (!Path.Exists(cidPath)) Directory.CreateDirectory(cidPath);
            string cidUassetPath = Path.Combine(cidPath, $"{CurrentSkin.CID}.uasset");
            string cidUexpPath = Path.Combine(cidPath, $"{CurrentSkin.CID}.uexp");
            File.WriteAllBytes(cidUassetPath, Convert.FromBase64String(CurrentFnVersion.CidUassetBase64));
            File.WriteAllBytes(cidUexpPath, Convert.FromBase64String(CurrentFnVersion.CidUexpBase64));

            var currentCid = new UAsset(cidUassetPath, EngineVersion.VER_UE4_26);
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
            if (App.Settings.FnVersion == "8.51" && CurrentSkin.Rarity != "Uncommon")
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
            ConsoleWriteLineSuccess($"Successfuly edited {CurrentSkin.CID}.uasset");

            //Idle Montage creation
            if (string.IsNullOrEmpty(CurrentSkin.LobbyAnimationPsa)) return;
            string idleAnimationUassetPath = Path.Combine(contentFolderPath, "Animations", $"{CurrentSkin.CodeName}_Idle_Montage.uasset");
            string idleAnimationUexpPath = Path.Combine(contentFolderPath, "Animations", $"{CurrentSkin.CodeName}_Idle_Montage.uexp");

            File.WriteAllBytes(idleAnimationUassetPath, Convert.FromBase64String(CurrentFnVersion.IdleMontageUassetBase64));
            File.WriteAllBytes(idleAnimationUexpPath, Convert.FromBase64String(CurrentFnVersion.IdleMontageUexpBase64));

            var currentIdleAnimation = new UAsset(idleAnimationUassetPath, EngineVersion.VER_UE4_26);
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
            currentIdleAnimation.Write(idleAnimationUassetPath);
            ConsoleWriteLineSuccess($"Successfuly edited {CurrentSkin.CodeName}_Idle_Montage.uasset");
        }

        private void SwizzleTextures(string texturePath)
        {
            using Bitmap bmp = new Bitmap(texturePath);
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);

            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;
                int bytesPerPixel = System.Drawing.Image.GetPixelFormatSize(bmp.PixelFormat) / 8;
                int totalBytes = bmpData.Stride * bmp.Height;

                for (int i = 0; i < totalBytes; i += bytesPerPixel)
                {
                    byte blue = ptr[i];
                    ptr[i] = ptr[i + 1];
                    ptr[i + 1] = blue;
                }
            }

            bmp.UnlockBits(bmpData);
            bmp.Save(Path.Combine(CurrentSkin.TexturesPath, "Swizzled", Path.GetFileName(texturePath)));
            Console.WriteLine($"Swizzled {texturePath}");
        }

        private float sRGBToLinearRGB(float val)
        {
            val /= 255f;
            if (val <= 0.04045f) return val / 12.92f;
            else return MathF.Pow((val + 0.055f) / 1.055f, 2.4f);
        }

        private SkinData LoadSkinConfig(string jsonPath)
        {
            string filePath = jsonPath;

            if (!File.Exists(filePath)) return null;

            string jsonString = File.ReadAllText(filePath);
            SkinData loadedSkin = System.Text.Json.JsonSerializer.Deserialize<SkinData>(jsonString);

            // Reconstruct the ignored ParentPage and Cp properties for the UI
            foreach (var mat in loadedSkin.Materials)
            {
                mat.ParentPage = this;
                mat.Cp = loadedSkin.CharacterParts.FirstOrDefault(cp => cp.Type == mat.Cp?.Type);
            }

            return loadedSkin;
        }

        public void SaveSkinConfig()
        {
            if (CurrentSkin == null || string.IsNullOrEmpty(CurrentSkinPath)) return;

            string jsonPath = Path.Combine(CurrentSkinPath, $"{CurrentSkin.CodeName}_Settings.json");
            var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
            string jsonString = System.Text.Json.JsonSerializer.Serialize(CurrentSkin, options);

            File.WriteAllText(jsonPath, jsonString);
        }
    }

    public class CharacterPart
    {
        public string Type { get; set; }
        public string PskPath { get; set; } = string.Empty;
        public string FbxPath { get; set; } = string.Empty;
        public List<string> PhysicsAssetJsonPaths { get; set; } = new();
        public string uassetFileBase64 { get; set; } = string.Empty;
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
        public string LobbyAnimationFbx { get; set; } = string.Empty;
        public string OutputContentPath { get; set; } = string.Empty;
        public ObservableCollection<Material> Materials { get; set; } = new();
        public string SourcePath { get; set; } = string.Empty;
        public string MeshesPath { get; set; } = string.Empty;
        public string TexturesPath { get; set; } = string.Empty;
        public string PhysicsPath { get; set; } = string.Empty;
        public string LobbyAnimationPath { get; set; } = string.Empty;
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

    public class UnrealExportData
    {
        public List<string> FbxPaths { get; set; }
        public List<string> PhysicsMeshNames { get; set; }
        public List<List<string>> PhysicsAssetsPaths { get; set; }
        public List<string> DiffuseTextures { get; set; }
        public List<string> MaskTextures { get; set; }
        public List<string> NormalTextures { get; set; }
        public List<string> SpecularTextures { get; set; }
        public List<string> IconTextures { get; set; }
        public List<string> Materials { get; set; }
        public string CodeName { get; set; }
        public List<string> MeshNames { get; set; }
        public string CID { get; set; } = string.Empty;
        public string LobbyAnimationFbxPath { get; set; } = string.Empty;
        public string RetargetSource { get; set; }
    }

    [JsonSerializable(typeof(BlenderExportData))]
    [JsonSerializable(typeof(UnrealExportData))]
    internal partial class AppJsonContext : JsonSerializerContext { }
}