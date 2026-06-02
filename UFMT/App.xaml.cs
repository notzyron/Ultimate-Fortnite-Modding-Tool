#pragma warning disable
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Storage;

namespace UFMT
{
    public partial class App : Application
    {
        public static ApplicationDataContainer Settings;
        public static Window m_window;
        public static string BlenderPath;
        public static string UeExecutablePath;
        public static string UeProjectPath;
        static public string UeVersion;
        static public string FnVersion;

        public App()
        {
            InitializeComponent();
            BlenderPath = AppSettings.GetValue("BlenderPath", "");
            UeExecutablePath = AppSettings.GetValue("UeExecutablePath", "");
            UeProjectPath = AppSettings.GetValue("UeProjectPath", "");
            UeVersion = AppSettings.GetValue("UeVersion", "");
            FnVersion = AppSettings.GetValue("FnVersion", "");
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}