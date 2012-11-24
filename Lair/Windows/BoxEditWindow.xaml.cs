﻿using System;
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
using Lair.Properties;
using Library;
using Library.Net;
using Library.Net.Amoeba;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// BoxEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class BoxEditWindow : Window
    {
        private List<Box> _boxes;

        public BoxEditWindow(params Box[] boxes)
        {
            _boxes = boxes.ToList();

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _nameTextBox.MaxLength = Box.MaxNameLength;
            _commentTextBox.MaxLength = Box.MaxCommentLength;

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_boxes[0].ThisLock)
            {
                _nameTextBox.Text = _boxes[0].Name;

                foreach (var box in _boxes)
                {
                    if (_nameTextBox.Text != box.Name)
                    {
                        _nameTextBox.Text = "";
                        _nameTextBox.IsReadOnly = true;

                        break;
                    }
                }

                _commentTextBox.Text = _boxes[0].Comment;
            }

            _signatureComboBox.ItemsSource = digitalSignatureCollection;
            
            var index = Settings.Instance.Global_DigitalSignatureCollection.IndexOf(Settings.Instance.Global_UploadDigitalSignature);
            _signatureComboBox.SelectedIndex = index + 1;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string name = _nameTextBox.Text;
            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            Settings.Instance.Global_UploadDigitalSignature = digitalSignature;

            var now = DateTime.UtcNow;

            foreach (var box in _boxes)
            {
                lock (box.ThisLock)
                {
                    if (!_nameTextBox.IsReadOnly)
                    {
                        box.Name = name;
                    }

                    box.Comment = comment;
                    box.CreationTime = now;

                    if (digitalSignature == null)
                    {
                        box.CreateCertificate(null);
                    }
                    else
                    {
                        box.CreateCertificate(digitalSignature);
                    }
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
