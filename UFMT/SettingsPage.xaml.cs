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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.InteropServices.WindowsRuntime;
using UAssetAPI.UnrealTypes;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UFMT;
public sealed partial class SettingsPage : Page
{
    public string UeVersion
    {
        get => App.UeVersion;
        set
        {
            if (App.UeVersion != value)
            {
                App.UeVersion = value;
                AppSettings.SetValue("UeVersion", value);
                AvailableFnVersions = UesFnVersions.GetValueOrDefault(value.Replace("_FnGameProj", ""));
                FnVersionComboBox.ItemsSource = AvailableFnVersions;
            }
        }
    }
    public string FnVersion
    {
        get => App.FnVersion;
        set
        {
            if (App.FnVersion != value)
            {
                App.FnVersion = value;
                AppSettings.SetValue("FnVersion", value);
            }
        }
    }
    public Dictionary<string, string[]> UesFnVersions = new() { {"UE_4.26", new string[] { "13.40", "14.30" } }, 
    { "UE_4.23", new string[] { "8.51"} } };
    public string[] AvailableFnVersions;
    public SettingsPage()
    {
        InitializeComponent();
        BlenderPathBox.Text = App.BlenderPath ?? "";
        UeExecutablePathBox.Text = App.UeExecutablePath ?? "";
        UeProjectPathBox.Text = App.UeProjectPath ?? "";
        AvailableFnVersions = UesFnVersions.GetValueOrDefault(UeVersion.Replace("_FnGameProj", ""));
        FnVersionComboBox.ItemsSource = AvailableFnVersions;
    }
    #region base64
    string DefaultEngineIniBase64 = @"Wy9TY3JpcHQvRW5naW5lU2V0dGluZ3MuR2FtZU1hcHNTZXR0aW5nc10NCkdhbWVEZWZhdWx0TWFwPS9HYW1lL1N0YXJ0ZXJDb250ZW50L01hcHMvTWluaW1hbF9EZWZhdWx0DQoNCg0KRWRpdG9yU3RhcnR1cE1hcD0vR2FtZS9TdGFydGVyQ29udGVudC9NYXBzL01pbmltYWxfRGVmYXVsdA0KDQpbL1NjcmlwdC9IYXJkd2FyZVRhcmdldGluZy5IYXJkd2FyZVRhcmdldGluZ1NldHRpbmdzXQ0KVGFyZ2V0ZWRIYXJkd2FyZUNsYXNzPURlc2t0b3ANCkFwcGxpZWRUYXJnZXRlZEhhcmR3YXJlQ2xhc3M9RGVza3RvcA0KRGVmYXVsdEdyYXBoaWNzUGVyZm9ybWFuY2U9TWF4aW11bQ0KQXBwbGllZERlZmF1bHRHcmFwaGljc1BlcmZvcm1hbmNlPU1heGltdW0NCg0KW1N5c3RlbVNldHRpbmdzXQ0Kci5Pb2RsZURhdGFDb21wcmVzc2lvbkZvcm1hdD1LcmFrZW4NCnIuT29kbGVEYXRhQ29tcHJlc3Npb25MZXZlbD01DQpyLlN0YXRpY01lc2guU3RyaXBNaW5Mb2REYXRhRHVyaW5nQ29va2luZz0xDQpyLlNrZWxldGFsTWVzaC5TdHJpcE1pbkxvRERhdGFEdXJpbmdDb29raW5nPTENCnIuU2tlbGV0YWxNZXNoLlN0cmlwT3B0aW9uYWxMT0RzPTENCnIuU2tpbkNhY2hlLkNvbXBpbGVTaGFkZXJzPTANCnIuUmF5VHJhY2luZz0wDQpyLlNraW5DYWNoZS5Nb2RlPTANCnIuU2tlbGV0YWxNZXNoLktlZXBNb2JpbGVNaW5MT0REYXRhPTANCnIuRm9yY2VTdHJpcEFkamFjZW5jeURhdGFEdXJpbmdDb29raW5nPTENCkNvbXBhdC5NQVhfR1BVU0tJTl9CT05FUz03NQ0Kci5Ta2VsZXRhbE1lc2guU3RyaXBWZXJ0ZXhDb2xvcnM9MQ0Kci5Ta2VsZXRhbE1lc2guRGlzY2FyZEF0dHJpYnV0ZXM9MQ0Kci5Ta2VsZXRhbE1lc2guT3B0aW1pemVTZWN0aW9uRGF0YT0xDQoNCltDb3JlLlN5c3RlbV0NCkxlZ2FjeUJ1bGtEYXRhT2Zmc2V0cz1UcnVlDQoNCltPb2RsZUhhbmRsZXJDb21wb25lbnRdDQpiRW5hYmxlT29kbGU9dHJ1ZQ0KU2VydmVyRW5hYmxlTW9kZT1BbHdheXNFbmFibGVkDQpDbGllbnRFbmFibGVNb2RlPUFsd2F5c0VuYWJsZWQNCk1vZGU9UmVsZWFzZQ0KYlVzZURpY3Rpb25hcnlJZlByZXNlbnQ9dHJ1ZQ0KU2VydmVyRGljdGlvbmFyeT1Db250ZW50L09vZGxlL0ZvcnRuaXRlR2FtZU91dHB1dC51ZGljDQpDbGllbnREaWN0aW9uYXJ5PUNvbnRlbnQvT29kbGUvRm9ydG5pdGVHYW1lSW5wdXQudWRpYw==";
    #endregion
    
    private void BlenderTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        BlenderPathBox.Text = BlenderPathBox.Text.Replace(@"""", "");
        AppSettings.SetValue("BlenderPath", BlenderPathBox.Text);
        App.BlenderPath = BlenderPathBox.Text;
    }

    private void UeExecutableTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        UeExecutablePathBox.Text = UeExecutablePathBox.Text.Replace(@"""", "");
        AppSettings.SetValue($"Ue{UeVersion}ExecutablePath", UeExecutablePathBox.Text);
        App.UeExecutablePath = UeExecutablePathBox.Text;
    }
    
    private void UeProjectTextBoxChanged(object sender, TextChangedEventArgs e)
    {
        UeProjectPathBox.Text = UeProjectPathBox.Text.Replace(@"""", "");
        AppSettings.SetValue($"Ue{UeVersion}ProjectPath", UeProjectPathBox.Text);
        App.UeProjectPath = UeProjectPathBox.Text;

        if (!string.IsNullOrEmpty(App.UeProjectPath))
        {
            string defaultEngineIniPath = Path.Combine(Path.GetDirectoryName(App.UeProjectPath), "Config", "DefaultEngine.ini");
            if (File.Exists(defaultEngineIniPath))
            {
                try
                {
                    File.WriteAllBytes(defaultEngineIniPath, Convert.FromBase64String(DefaultEngineIniBase64));
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Modified {defaultEngineIniPath}!");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[SettingsPage] Error modifying DefaultEngine.ini: {ex.Message}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }
    }

    private void UeVersionChanged(object sender, SelectionChangedEventArgs e)
    {
        var c = sender as ComboBox;
        UeVersion = c.SelectedItem.ToString();
        UeExecutablePathBox.Text = AppSettings.GetValue($"Ue{UeVersion}ExecutablePath", "");
        UeProjectPathBox.Text = AppSettings.GetValue($"Ue{UeVersion}ProjectPath", "");
        App.UeExecutablePath = UeExecutablePathBox.Text;
        App.UeProjectPath = UeProjectPathBox.Text;
    }

    private void FnVersionChanged(object sender, SelectionChangedEventArgs e)
    {
        var c = sender as ComboBox;
        FnVersion = c.SelectedItem.ToString();
    }
}
