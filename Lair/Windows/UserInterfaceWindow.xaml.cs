﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// UserInterfaceWindow.xaml の相互作用ロジック
    /// </summary>
    partial class UserInterfaceWindow : Window
    {
        private BufferManager _bufferManager = new BufferManager();
        private List<SignatureListViewItem> _signatureListViewItemCollection = new List<SignatureListViewItem>();

        public UserInterfaceWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _signatureListViewItemCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.DeepClone())));

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _signatureListView.ItemsSource = _signatureListViewItemCollection;

            _updateUrlTextBox.Text = Settings.Instance.Global_Update_Url;
            _updateProxyUriTextBox.Text = Settings.Instance.Global_Update_ProxyUri;
            _updateSignatureTextBox.Text = Settings.Instance.Global_Update_Signature;

            if (Settings.Instance.Global_Update_Option == UpdateOption.None)
            {
                _updateOptionNoneRadioButton.IsChecked = true;
            }
            else if (Settings.Instance.Global_Update_Option == UpdateOption.AutoCheck)
            {
                _updateOptionAutoCheckRadioButton.IsChecked = true;
            }
            else if (Settings.Instance.Global_Update_Option == UpdateOption.AutoUpdate)
            {
                _updateOptionAutoUpdateRadioButton.IsChecked = true;
            }
        }

        #region Signature

        private void _signatureListViewUpdate()
        {
            _signatureListView_SelectionChanged(this, null);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _signatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _signatureUpButton.IsEnabled = false;
                    _signatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _signatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _signatureListViewItemCollection.Count - 1)
                    {
                        _signatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureDownButton.IsEnabled = true;
                    }
                }

                _signatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _signatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _signatureImportButton_Click(object sender, RoutedEventArgs e)
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
                                if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                                _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));

                                _signatureListView.Items.Refresh();
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
            }
        }

        private void _signatureExportButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
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

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex - 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Remove(item);
            _signatureListViewItemCollection.Insert(selectIndex + 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_signatureTextBox.Text)) return;

            _signatureListViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(_signatureTextBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512)));

            _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;
            _signatureListView.Items.Refresh();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            int selectIndex = _signatureListView.SelectedIndex;
            _signatureListViewItemCollection.Remove(item);
            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            Settings.Instance.Global_DigitalSignatureCollection.Clear();
            Settings.Instance.Global_DigitalSignatureCollection.AddRange(_signatureListViewItemCollection.Select(n => n.Value));

            Settings.Instance.Global_Update_Url = _updateUrlTextBox.Text;
            Settings.Instance.Global_Update_ProxyUri = _updateProxyUriTextBox.Text;
            Settings.Instance.Global_Update_Signature = _updateSignatureTextBox.Text;

            if (_updateOptionNoneRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.None;
            }
            else if (_updateOptionAutoCheckRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.AutoCheck;
            }
            else if (_updateOptionAutoUpdateRadioButton.IsChecked.Value)
            {
                Settings.Instance.Global_Update_Option = UpdateOption.AutoUpdate;
            }
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

    [DataContract(Name = "UpdateOption", Namespace = "http://Lair/Windows")]
    enum UpdateOption
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "AutoCheck")]
        AutoCheck = 1,

        [EnumMember(Value = "AutoUpdate")]
        AutoUpdate = 2,
    }
}
