#pragma warning disable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.WindowsRuntime;
using UAssetAPI.UnrealTypes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.UFMTGenericHelpers;

namespace UFMT;
public sealed partial class SettingsPage : Page
{
    public SettingsData Settings => App.Settings;
    public SettingsPage()
    {
        InitializeComponent();
    }
    private void Remove_Quotes(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box && box.Text.Contains("\""))
        {
            box.Text = box.Text.Replace("\"", "");
        }
    }
}

public class SettingsData : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public SettingsData()
    {
        AvailableFnVersions = UeFnVersions.GetValueOrDefault(UeVersion);
        _fnVersion = AppSettings.GetValue($"{UeVersion}_FnVersion", "8.51-9.10");
        _ueExecutablePath = AppSettings.GetValue($"{UeVersion}_ExecutablePath", "");
        _ueProjectPath = AppSettings.GetValue($"{UeVersion}_ProjectPath", "");
    }

    private Dictionary<string, string[]> UeFnVersions = new()
    {
        {"UE_4.22", new string[] {"8.51-9.10" } },       
        {"UE_4.25", new string[] {"14.30", "13.40" } },
        {"UE_4.26", new string[] {"14.30", "13.40" } },
        {"UE_4.26_FnGameProj14.30", new string[] {"14.30", "13.40" } },
        {"UE_4.23_FnGameProj8.51", new string[] {"8.51-9.10"} },
		{"UE_4.23_FnGameProj9.10", new string[] {"8.51-9.10"} },
        {"UE_4.23_FnGameProj9.41", new string[] {"9.41"} },
    };
    private string _ueVersion = AppSettings.GetValue("UeVersion", "UE_4.22");
    public string UeVersion
    {
        get => _ueVersion;
        set
        {
            if (_ueVersion != value)
            {
                _ueVersion = value;
                AvailableFnVersions = UeFnVersions.GetValueOrDefault(value);
                OnPropertyChanged(nameof(AvailableFnVersions));

                _fnVersion = AppSettings.GetValue($"{value}_FnVersion", UeFnVersions.GetValueOrDefault(value)[0]);
                AppSettings.SetValue($"{value}_FnVersion", _fnVersion);
                _ueExecutablePath = AppSettings.GetValue($"{value}_ExecutablePath", "");
                _ueProjectPath = AppSettings.GetValue($"{value}_ProjectPath", "");

                AppSettings.SetValue("UeVersion", value);

                OnPropertyChanged();
                OnPropertyChanged(nameof(FnVersion));
                OnPropertyChanged(nameof(UeExecutablePath));
                OnPropertyChanged(nameof(UeProjectPath));
            }
        }
    }

    public string[] AvailableFnVersions { get; set; }

    private string _fnVersion;
    public string FnVersion
    {
        get => _fnVersion;
        set
        {
            if (!string.IsNullOrEmpty(value) && _fnVersion != value)
            {
                _fnVersion = value;
                AppSettings.SetValue($"{UeVersion}_FnVersion", value);
                OnPropertyChanged();
            }
        }
    }
    private string _blenderPath = AppSettings.GetValue("BlenderPath", "");
    public string BlenderPath
    {
        get => _blenderPath;
        set
        {
            if (_blenderPath != value)
            {
                _blenderPath = value;
                AppSettings.SetValue("BlenderPath", value);
                OnPropertyChanged();
            }
        }
    }

    private string _ueExecutablePath;
    public string UeExecutablePath
    {
        get => _ueExecutablePath;
        set
        {
            if (_ueExecutablePath != value)
            {
                _ueExecutablePath = value;
                AppSettings.SetValue($"{UeVersion}_ExecutablePath", value);
                OnPropertyChanged();
            }
        }
    }
    private string _ueProjectPath;
    public string UeProjectPath
    {
        get => _ueProjectPath;
        set
        {
            if (_ueProjectPath != value)
            {
                _ueProjectPath = value;
                AppSettings.SetValue($"{UeVersion}_ProjectPath", value);
                OnPropertyChanged();
            }
        }
    }
}
