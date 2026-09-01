using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace UFMT.UI
{
    public sealed partial class EmotesPage : Page
    {
        public EmotesPage()
        {
            InitializeComponent();
        }

        private void EmotesPathTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void BrowseButton_Click(object sender, RoutedEventArgs e) { }
        private void CreateEmoteFolder_Click(object sender, RoutedEventArgs e) { }
        private void CurrentEmotePathTextBox_TextChanged(object sender, TextChangedEventArgs e) { }
        private void Reimport_Click(object sender, RoutedEventArgs e) { }
    }
}
