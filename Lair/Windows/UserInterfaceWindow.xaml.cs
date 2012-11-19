using System;
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
        private List<string> _messageFontFamilyComboBoxItemCollection = new List<string>();

        public UserInterfaceWindow(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _signatureListViewItemCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new SignatureListViewItem(n.DeepClone())));
            _messageFontFamilyComboBoxItemCollection.AddRange(Fonts.SystemFontFamilies.Select(n => n.ToString()));

            InitializeComponent();

            _miscellaneousStackPanel.DataContext = new ExpanderListViewModel();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
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

            _amoebaPathTextBox.Text = Settings.Instance.Global_Amoeba_Path;

            _messageFontFamilyComboBox.ItemsSource = _messageFontFamilyComboBoxItemCollection;
            _messageFontFamilyComboBox.SelectedItem = Settings.Instance.Global_Fonts_MessageFontFamily;

            _messageFontSizeTextBox.Text = Settings.Instance.Global_Fonts_MessageFontSize.ToString();

            _miscellaneousClearUrlHistoryCheckBox.IsChecked = Settings.Instance.Global_UrlClearHistory_IsEnabled;
        }

        public UserInterfaceWindow(int i, BufferManager bufferManager)
            : this(bufferManager)
        {
            ((TabItem)_tabControl.Items[i]).IsSelected = true;
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
                            var signature = DigitalSignatureConverter.FromSignatureStream(stream);
                            if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                            _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));
                        }
                    }
                    catch (Exception)
                    {

                    }
                }

                _signatureListView.Items.Refresh();
            }
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
                                var signature = DigitalSignatureConverter.FromSignatureStream(stream);
                                if (_signatureListViewItemCollection.Any(n => n.Value == signature)) continue;

                                _signatureListViewItemCollection.Add(new SignatureListViewItem(signature));
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    _signatureListView.Items.Refresh();
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
                dialog.FileName = MessageConverter.ToSignatureString(signature);
                dialog.DefaultExt = ".signature";
                dialog.Filter = "Signature (*.signature)|*.signature";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    try
                    {
                        using (FileStream stream = new FileStream(fileName, FileMode.Create))
                        using (Stream signatureStream = DigitalSignatureConverter.ToSignatureStream(signature))
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

            try
            {
                _signatureListViewItemCollection.Add(new SignatureListViewItem(new DigitalSignature(_signatureTextBox.Text, DigitalSignatureAlgorithm.Rsa2048_Sha512)));

                _signatureListView.SelectedIndex = _signatureListViewItemCollection.Count - 1;
                _signatureListView.Items.Refresh();
            }
            catch (Exception)
            {

            }
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

        #region Amoeba

        private void _amoebaPathTextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = false;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = ".exe";
                dialog.Filter = "Exe files (*.exe)|*.exe";

                try
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(_amoebaPathTextBox.Text);
                    dialog.FileName = Path.GetFileName(_amoebaPathTextBox.Text);
                }
                catch (Exception)
                {

                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _amoebaPathTextBox.Text = dialog.FileName;
                }
            }
        }

        #endregion

        #region Fonts

        private static double GetStringToDouble(string value)
        {
            StringBuilder builder = new StringBuilder("0");

            foreach (var item in value)
            {
                var w = item.ToString();

                if (Regex.IsMatch(w, "[0-9\\.]"))
                {
                    if (w == ".") builder.Replace(".", "");
                    builder.Append(w);
                }
            }

            double count = 0;

            try
            {
                count = double.Parse(builder.ToString().TrimEnd('.'));
            }
            catch (OverflowException)
            {
                count = double.MaxValue;
            }

            return count;
        }

        private void _messageFontSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_messageFontSizeTextBox.Text)) return;

            StringBuilder builder = new StringBuilder("");

            foreach (var item in _messageFontSizeTextBox.Text)
            {
                if (Regex.IsMatch(item.ToString(), "[0-9\\.]"))
                {
                    builder.Append(item.ToString());
                }
            }

            var value = builder.ToString();
            if (_messageFontSizeTextBox.Text != value) _messageFontSizeTextBox.Text = value;
        }
       
        #endregion

        #region Miscellaneous

        private void _miscellaneousStackPanel_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Expander expander = e.Source as Expander;
            if (expander == null) return;

            foreach (var item in _miscellaneousStackPanel.Children.OfType<Expander>())
            {
                if (expander != item) item.IsExpanded = false;
            }
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

            Settings.Instance.Global_Amoeba_Path = _amoebaPathTextBox.Text;

            Settings.Instance.Global_Fonts_MessageFontFamily = (string)_messageFontFamilyComboBox.SelectedItem;

            double messageFontSize = UserInterfaceWindow.GetStringToDouble(_messageFontSizeTextBox.Text);
            Settings.Instance.Global_Fonts_MessageFontSize = Math.Max(Math.Min(messageFontSize, 100), 1);

            Settings.Instance.Global_UrlClearHistory_IsEnabled = _miscellaneousClearUrlHistoryCheckBox.IsChecked.Value;
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

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_signatureTabItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
        }

        public class ExpanderListViewModel
        {
            public ExpanderListViewModel()
            {
                this.SelectedExpander = "1";
            }

            public string SelectedExpander { get; set; }
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
