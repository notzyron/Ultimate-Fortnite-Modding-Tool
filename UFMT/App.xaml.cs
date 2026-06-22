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
            UeVersion = AppSettings.GetValue("UeVersion", "");
            FnVersion = AppSettings.GetValue("FnVersion", "");
            BlenderPath = AppSettings.GetValue("BlenderPath", "");
            UeExecutablePath = AppSettings.GetValue($"Ue{UeVersion}ExecutablePath", "");
            UeProjectPath = AppSettings.GetValue($"Ue{UeVersion}ProjectPath", "");

            this.UnhandledException += (sender, e) =>
            {
                string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
                string filePath = System.IO.Path.Combine(desktopPath, "crash_log.txt");
                string logContent = $"[{System.DateTime.Now}] Exception: {e.Message}\nStack Trace:\n{e.Exception?.StackTrace}\n\n";

                System.IO.File.AppendAllText(filePath, logContent);
            };
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
    }
}