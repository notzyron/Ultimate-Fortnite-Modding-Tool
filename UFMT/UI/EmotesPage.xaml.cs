using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using UFMT.Core;
using UFMT.FnAssets;
using UFMT.FnAssetsLogic;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UFMT.UI
{
    public sealed partial class EmotesPage : Page
    {
        private CancellationTokenSource _currentSkinPathDebounce;
        public event PropertyChangedEventHandler PropertyChanged;
        private EmoteData _currentEmote;
        public EmoteData CurrentEmote
        {
            get => _currentEmote;
            set
            {
                if (_currentEmote != value)
                {
                    _currentEmote = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentEmote)));
                }
            }
        }
        public EmotesPage()
        {
            InitializeComponent();
            CurrentEmote = new EmoteData();
        }

        private void EmotesPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            AppSettings.SetValue("EmotesPath", (sender as TextBox)?.Text);
        }

        private async void CurrentEmotePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _currentSkinPathDebounce?.Cancel();
            _currentSkinPathDebounce = new CancellationTokenSource();
            var token = _currentSkinPathDebounce.Token;
            try
            {
                await Task.Delay(250, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            AppSettings.SetValue("CurrentEmote", (sender as TextBox).Text);
            if (!EmoteValidator.ValidateAfterPathChange((sender as TextBox)?.Text, CurrentEmote)) return;
            Log.Test($"Current emote's icons folder path is {CurrentEmote.IconsPath}");
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e) { }
        private void CreateEmoteFolder_Click(object sender, RoutedEventArgs e) { }
        private void Reimport_Click(object sender, RoutedEventArgs e) { }
    }

    public class EmoteData : INotifyPropertyChanged
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
        private string _smallIcon = string.Empty;
        public string SmallIcon
        {
            get => _smallIcon;
            set
            {
                if (_smallIcon != value)
                {
                    _smallIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _largeIcon = string.Empty;
        public string LargeIcon
        {
            get => _largeIcon;
            set
            {
                if (_largeIcon != value)
                {
                    _largeIcon = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _eid = string.Empty;
        public string EID
        {
            get => _eid;
            set
            {
                if (_eid != value)
                {
                    _eid = value;
                    OnPropertyChanged();
                }
            }

        }
        public string MaleAnimationPsa { get; set; } = string.Empty;
        public string MaleAnimationFbx { get; set; } = string.Empty;
        public string MaleAnimationJson { get; set; } = string.Empty;
        public float MaleAnimationLength { get; set; } = 0;
        public string FemaleAnimationPsa { get; set; } = string.Empty;
        public string FemaleAnimationFbx { get; set; } = string.Empty;
        public string FemaleAnimationJson { get; set; } = string.Empty;
        public float FemaleAnimationLength { get; set; } = 0;
        public string OutputContentPath { get; set; } = string.Empty;

        public string Path = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string SourcePath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string AnimationsPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string IconsPath { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string SoundPath { get; set; } = string.Empty;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public EmoteData Clone()
        {
            EmoteData clone = (EmoteData)this.MemberwiseClone();
            return clone;
        }
    }
}
