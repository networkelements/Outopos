using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Outopos.Properties;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;

namespace Outopos.Windows
{
    /// <summary>
    /// NameWindow.xaml の相互作用ロジック
    /// </summary>
    partial class NameWindow : Window
    {
        private string _text;

        public NameWindow()
        {
            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }
        }

        public NameWindow(string text)
            : this()
        {
            _text = text;

            _textBox.Text = _text;
        }

        public NameWindow(int maxLength)
            : this()
        {
            _textBox.MaxLength = maxLength;
        }

        public NameWindow(string text, int maxLength)
            : this()
        {
            _text = text;

            _textBox.Text = _text;
            _textBox.MaxLength = maxLength;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        public string Text
        {
            get
            {
                return _text;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.MaxHeight = this.RenderSize.Height;
            this.MinHeight = this.RenderSize.Height;

            WindowPosition.Move(this);
        }

        private void _textBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _okButton.IsEnabled = !string.IsNullOrWhiteSpace(_textBox.Text);
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            _text = _textBox.Text;

            this.DialogResult = true;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
