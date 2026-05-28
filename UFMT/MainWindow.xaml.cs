#pragma warning disable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace UFMT
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        private const int STD_OUTPUT_HANDLE = -11;

        public MainWindow()
        {
            this.InitializeComponent();

            SetupPackagedConsole();

            ExtendsContentIntoTitleBar = true;
            this.Title = "UFMT";
        }

        private void SetupPackagedConsole()
        {
            try
            {
                if (AllocConsole())
                {
                    var stdOutputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                    if (stdOutputHandle != IntPtr.Zero && stdOutputHandle != new IntPtr(-1))
                    {
                        var stream = new FileStream(stdOutputHandle, FileAccess.Write);

                        var writer = new StreamWriter(stream, System.Console.OutputEncoding)
                        {
                            AutoFlush = true
                        };

                        Console.SetOut(writer);
                        Console.SetError(writer);
                    }

                    Console.Title = "UFMT Debug Console";
                    Console.WriteLine("[System] Packaged Debug Console Initialized.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Console Init Error: {ex.Message}");
            }
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
            ContentFrame.Navigate(typeof(SkinsPage));
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.InvokedItemContainer != null)
            {
                string tag = args.InvokedItemContainer.Tag.ToString();
                if (tag == "SkinsPage") ContentFrame.Navigate(typeof(SkinsPage));
            }
        }
    }
}