using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using Library.Security;
using Lair.Properties;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for CreateSignatureWindow.xaml
    /// </summary>
    partial class CreateSignatureWindow : Window
    {
        public CreateSignatureWindow()
        {
            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !_okButton.IsEnabled;
        }

        private void _nicknameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_nicknameTextBox.Text) || _nicknameTextBox.Text.Length > DigitalSignature.MaxNickNameLength)
            {
                _okButton.IsEnabled = false;
            }
            else
            {
                _okButton.IsEnabled = true;
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            var digitalSignature = new DigitalSignature(_nicknameTextBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512);
            Settings.Instance.Global_DigitalSignatureCollection.Add(digitalSignature);
        }
    }
}
