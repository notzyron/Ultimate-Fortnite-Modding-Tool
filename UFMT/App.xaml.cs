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
        public static ApplicationDataContainer AppSettings;
        public static Window m_window;
        public static SettingsData Settings { get; } = new SettingsData();

        public App()
        {
            InitializeComponent();

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