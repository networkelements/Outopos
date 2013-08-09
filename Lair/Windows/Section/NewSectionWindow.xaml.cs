using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// NewSectionWindow.xaml の相互作用ロジック
    /// </summary>
    partial class NewSectionWindow : Window
    {
        private Section _channel;

        public NewSectionWindow()
        {
            InitializeComponent();

            _nameTextBox.MaxLength = Section.MaxNameLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;
        }

        public Section Section
        {
            get
            {
                return _channel;
            }
        }

        private void _nameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_nameTextBox.Text);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            byte[] buffer = new byte[64];
            (new RNGCryptoServiceProvider()).GetBytes(buffer);

            string name = _nameTextBox.Text;

            _channel = new Section(buffer, name);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
