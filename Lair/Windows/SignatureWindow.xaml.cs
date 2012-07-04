﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;
using Lair.Properties;
using Library;
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// SignatureWindow.xaml の相互作用ロジック
    /// </summary>
    partial class SignatureWindow : Window
    {
        private BufferManager _bufferManager = new BufferManager();
     
        private List<SignatureListViewItem> _listViewItemCollection = new List<SignatureListViewItem>();

        public SignatureWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            _listViewItemCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.DeepClone())));

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _listView.ItemsSource = _listViewItemCollection;
        }

        private void _listViewUpdate()
        {
            _listView_SelectionChanged(this, null);
        }

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _listView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _upButton.IsEnabled = false;
                    _downButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _upButton.IsEnabled = false;
                    }
                    else
                    {
                        _upButton.IsEnabled = true;
                    }

                    if (selectIndex == _listViewItemCollection.Count - 1)
                    {
                        _downButton.IsEnabled = false;
                    }
                    else
                    {
                        _downButton.IsEnabled = true;
                    }
                }

                _listView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _listView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _importButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var fileName in dialog.FileNames)
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Open))
                        {
                            try
                            {
                                var signature = LairConverter.FromSignatureStream(stream);
                                if (_listViewItemCollection.Any(n => n.Value == signature)) continue;

                                _listViewItemCollection.Add(new SignatureListViewItem(signature));

                                _listView.Items.Refresh();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void _exportButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var signature = item.Value;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.FileName = MessageConverter.ToSignatureString(signature);
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream signatureStream = LairConverter.ToSignatureStream(signature))
                    {
                        int i = -1;
                        byte[] buffer = _bufferManager.TakeBuffer(1024);

                        while ((i = signatureStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, i);
                        }

                        _bufferManager.ReturnBuffer(buffer);
                    }
                }
            }
        }

        private void _upButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _listViewItemCollection.Remove(item);
            _listViewItemCollection.Insert(selectIndex - 1, item);
            _listView.Items.Refresh();

            _listViewUpdate();
        }

        private void _downButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _listView.SelectedIndex;
            if (selectIndex == -1) return;

            _listViewItemCollection.Remove(item);
            _listViewItemCollection.Insert(selectIndex + 1, item);
            _listView.Items.Refresh();

            _listViewUpdate();
        }

        private void _addButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_textBox.Text)) return;
         
            _listViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(_textBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512)));

            _listView.SelectedIndex = _listViewItemCollection.Count - 1;
            _listView.Items.Refresh();
        }

        private void _deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _listView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            int selectIndex = _listView.SelectedIndex;
            _listViewItemCollection.Remove(item);
            _listView.Items.Refresh();
            _listView.SelectedIndex = selectIndex;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            Settings.Instance.Global_DigitalSignatureCollection.Clear();
            Settings.Instance.Global_DigitalSignatureCollection.AddRange(_listViewItemCollection.Select(n => n.Value));
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private class SignatureListViewItem
        {
            private DigitalSignature _value;
            private string _text;

            public SignatureListViewItem(DigitalSignature signatureItem)
            {
                this.Value = signatureItem;
            }

            public void Update()
            {
                _text = MessageConverter.ToSignatureString(_value);
            }

            public DigitalSignature Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
      
                    this.Update();
                }
            }

            public string Text
            {
                get
                {
                    return _text;
                }
            }
        }
    }
}
