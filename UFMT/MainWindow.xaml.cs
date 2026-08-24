#pragma warning disable
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
namespace UFMT
{
    public sealed partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        private const int STD_OUTPUT_HANDLE = -11;
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        const uint WM_SETICON = 0x0080;
        const int ICON_BIG = 1;
        const int ICON_SMALL = 0;
        private System.Drawing.Icon ico;
        public MainWindow()
        {
            this.InitializeComponent();
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            string exeDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = System.IO.Path.Combine(exeDir, "Assets", "UFMT.ico");
            ico = new System.Drawing.Icon(iconPath);
            SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_BIG, ico.Handle);
            SendMessage(hWnd, WM_SETICON, (IntPtr)ICON_SMALL, ico.Handle);
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
                    IntPtr consoleHwnd = GetConsoleWindow();
                    SendMessage(consoleHwnd, WM_SETICON, (IntPtr)ICON_BIG, ico.Handle);
                    SendMessage(consoleHwnd, WM_SETICON, (IntPtr)ICON_SMALL, ico.Handle);
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
            // Forces focus away from any focused TextBox before navigating, so LostFocus
            // fires and the x:Bind value commits before the new page reads settings
            var options = new FindNextElementOptions { SearchRoot = this.Content };
            FocusManager.TryMoveFocus(FocusNavigationDirection.Next, options);
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