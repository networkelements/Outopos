using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// NewChannelWindow.xaml の相互作用ロジック
    /// </summary>
    partial class NewChannelWindow : Window
    {
        private Channel _channel;

        public NewChannelWindow(out Channel channel)
        {
            channel = new Channel();

            _channel = channel;

            byte[] buffer = new byte[64];
            (new RNGCryptoServiceProvider()).GetBytes(buffer);

            _channel.Id = buffer;

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string name = _nameTextBox.Text;

            {
                _channel.Name = name;
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
