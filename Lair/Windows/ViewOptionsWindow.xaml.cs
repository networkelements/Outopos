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
    /// ViewOptionsWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ViewOptionsWindow : Window
    {
        private BufferManager _bufferManager;

        private ObservableCollectionEx<SignatureListViewItem> _signatureListViewItemCollection;

        public ViewOptionsWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

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

            _signatureListViewItemCollection = new ObservableCollectionEx<SignatureListViewItem>(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.Clone())));
            _signatureListView.ItemsSource = _signatureListViewItemCollection;
            _signatureListViewUpdate();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _updateTreeViewItem.IsSelected = true;

            WindowPosition.Move(this);
        }

        #region Signature

        private void _signatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _signatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _signatureListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
                e.Handled = true;
            }
        }

        private void _signatureListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string filePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)))
                {
                    try
                    {
                        using (FileStream stream = new FileStream(filePath, FileMode.Open))
                        {
                            var signature = DigitalSignatureConverter.FromDigitalSignatureStream(stream);
                            if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                            _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                _signatureListViewUpdate();
            }
        }

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

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureImportButton_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        try
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                            {
                                var signature = DigitalSignatureConverter.FromDigitalSignatureStream(stream);
                                if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                                _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _signatureListViewUpdate();
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
                dialog.RestoreDirectory = true;
                dialog.FileName = signature.ToString();
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    try
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Create))
                        using (Stream signatureStream = DigitalSignatureConverter.ToDigitalSignatureStream(signature))
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
                    catch (Exception)
                    {

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

            _signatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SignatureListViewItem;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_signatureTextBox.Text)) return;

            try
            {
                _signatureListViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(_signatureTextBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512)));
            }
            catch (Exception)
            {

            }

            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.RemoveAt(selectIndex);

            _signatureListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            Settings.Instance.Global_DigitalSignatureCollection.Clear();
            Settings.Instance.Global_DigitalSignatureCollection.AddRange(_signatureListViewItemCollection.Select(n => n.Value));

            Settings.Instance.Global_Update_Url = _updateUrlTextBox.Text;
            Settings.Instance.Global_Update_ProxyUri = _updateProxyUriTextBox.Text;
            if (Signature.HasSignature(_updateSignatureTextBox.Text)) Settings.Instance.Global_Update_Signature = _updateSignatureTextBox.Text;

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
                _text = _value.ToString();
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

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_signaturesTreeViewItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_signaturesTreeViewItem.IsSelected)
            {

            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_signaturesTreeViewItem.IsSelected)
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_signaturesTreeViewItem.IsSelected)
            {

            }
        }
    }
}
