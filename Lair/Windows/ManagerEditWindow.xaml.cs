using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using System.Collections.ObjectModel;

namespace Lair.Windows
{
    /// <summary>
    /// ManagerEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ManagerEditWindow : Window
    {
        private BufferManager _bufferManager;

        private ObservableCollection<string> _signatureListViewItemCollection = new ObservableCollection<string>();

        private Manager _leader;

        private Section _section;
        private List<string> _creators = new List<string>();
        private string _comment;
        private Certificate _certificate;
        private List<DigitalSignature> _digitalSignatures = new List<DigitalSignature>();

        public ManagerEditWindow(Section section, IEnumerable<string> creators, string comment, Certificate certificate, IEnumerable<DigitalSignature> digitalSignatures, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _section = section;
            if (creators != null) _creators.AddRange(creators);
            _comment = comment;
            _certificate = certificate;
            _digitalSignatures.AddRange(digitalSignatures);

            _signatureListViewItemCollection = new ObservableCollection<string>(_creators);

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(_digitalSignatures.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            if (_certificate != null)
            {
                int index = 0;
                for (; _digitalSignatures.Count < index
                    && _digitalSignatures[index].ToString() != _certificate.ToString(); index++) ;

                _signatureComboBox.SelectedIndex = index + 1;
            }

            _signatureComboBox_SelectionChanged(null, null);

            _signatureListView.ItemsSource = _signatureListViewItemCollection;
         
            _commentTextBox.Text = _comment;
        }

        public Manager Manager
        {
            get
            {
                return _leader;
            }
        }

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _okButton.IsEnabled = (_signatureComboBox.SelectedIndex > 0);
        }

        private void _signatureComboBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectComboBoxItem = (DigitalSignatureComboBoxItem)_signatureComboBox.SelectedItem;

            Clipboard.SetText(selectComboBoxItem.Value.ToString());
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
            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _signatureTextBox.Text = "";
                return;
            }

            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            _signatureTextBox.Text = item;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            _signatureListViewPasteMenuItem.IsEnabled = Clipboard.GetText().Split('\r', '\n').Any(n => Signature.HasSignature(n));
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<string>())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    if (!Signature.HasSignature(item)) continue;

                    if (_signatureListViewItemCollection.Contains(item)) continue;
                    _signatureListViewItemCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;

            _signatureListViewUpdate();
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Move(selectIndex, selectIndex - 1);

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as string;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureListViewItemCollection.Move(selectIndex, selectIndex + 1);

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_signatureTextBox.Text) || !Signature.HasSignature(_signatureTextBox.Text)) return;

            var item = _signatureTextBox.Text;

            if (_signatureListViewItemCollection.Contains(item)) return;
            _signatureListViewItemCollection.Add(item);

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;

            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureTextBox.Text = "";

            foreach (var item in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _signatureListViewItemCollection.Remove(item);
            }

            _signatureListView.SelectedIndex = selectIndex;
            _signatureListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            string comment = _commentTextBox.Text;
            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _leader = new Manager(_section, comment, new SignatureCollection(_signatureListViewItemCollection), digitalSignature);
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_managersTabItem.IsSelected)
            {
                _signatureListViewPasteMenuItem_Click(null, null);
            }
        }
    }
}
