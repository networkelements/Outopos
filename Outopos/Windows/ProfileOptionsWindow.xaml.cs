using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Library;
using Library.Net.Outopos;
using Library.Security;
using Outopos.Properties;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for ProfileOptionsWindow.xaml
    /// </summary>
    partial class ProfileOptionsWindow : Window
    {
        private ProfileItem _profileItem;
        private OutoposManager _outoposManager;
        private BufferManager _bufferManager;

        private string _uploadSignature;
        private ObservableCollectionEx<string> _signatureCollection = new ObservableCollectionEx<string>();
        private ObservableCollectionEx<Wiki> _wikiCollection = new ObservableCollectionEx<Wiki>();
        private ObservableCollectionEx<Chat> _chatCollection = new ObservableCollectionEx<Chat>();

        public ProfileOptionsWindow(ProfileItem profileItem, OutoposManager outoposManager, BufferManager bufferManager)
        {
            _profileItem = profileItem;
            _outoposManager = outoposManager;
            _bufferManager = bufferManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _wikiTextBox.MaxLength = Wiki.MaxNameLength;
            _chatTextBox.MaxLength = Chat.MaxNameLength;

            lock (_profileItem.ThisLock)
            {
                _uploadSignature = _profileItem.UploadSignature;
                _signatureCollection.AddRange(_profileItem.TrustSignatures);
                _wikiCollection.AddRange(_profileItem.Wikis);
                _chatCollection.AddRange(_profileItem.Chats);
            }

            _signatureListView.ItemsSource = _signatureCollection;
            _wikiListView.ItemsSource = _wikiCollection;
            _chatListView.ItemsSource = _chatCollection;

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            this.Sort();
        }

        private void Sort()
        {
            _signatureListView.Items.SortDescriptions.Clear();
            _signatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowPosition.Move(this);

            for (int index = 0; index < Settings.Instance.Global_DigitalSignatureCollection.Count; index++)
            {
                if (Settings.Instance.Global_DigitalSignatureCollection[index].ToString() == _uploadSignature)
                {
                    _signatureComboBox.SelectedIndex = index + 1;

                    break;
                }
            }
        }

        #region _signatureListView

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Signature.Check(line[0]);
                }

                _signatureListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _signatureCollection.Remove(item);
            }
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.Check(signature)) continue;

                if (_signatureCollection.Contains(signature)) continue;
                _signatureCollection.Add(signature);
            }
        }

        #endregion

        #region _wikiListView

        private void _wikiTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_wikiListView.SelectedIndex == -1)
                {
                    _wikiAddButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _wikiListViewUpdate()
        {
            _wikiListView_SelectionChanged(this, null);
        }

        private void _wikiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _wikiListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _wikiUpButton.IsEnabled = false;
                    _wikiDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _wikiUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _wikiUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _wikiCollection.Count - 1)
                    {
                        _wikiDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _wikiDownButton.IsEnabled = true;
                    }
                }

                _wikiListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _wikiListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _wikiListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _wikiTextBox.Text = "";

                return;
            }
        }

        private void _wikiListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _wikiListView.SelectedItems;

            _wikiListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _wikiListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _wikiListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                _wikiListViewPasteMenuItem.IsEnabled = Clipboard.ContainsWikis();
            }
        }

        private void _wikiListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _wikiDeleteButton_Click(null, null);
        }

        private void _wikiListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _wikiListViewCopyMenuItem_Click(null, null);
            _wikiDeleteButton_Click(null, null);
        }

        private void _wikiListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wikiListView.SelectedItems.OfType<Wiki>())
            {
                sb.AppendLine(OutoposConverter.ToWikiString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _wikiListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tag in Clipboard.GetWikis())
            {
                try
                {
                    if (_wikiCollection.Contains(tag)) continue;
                    _wikiCollection.Add(tag);
                }
                catch (Exception)
                {

                }
            }

            _wikiListViewUpdate();
        }

        private void _wikiUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wikiListView.SelectedItem as Wiki;
            if (item == null) return;

            var selectIndex = _wikiListView.SelectedIndex;
            if (selectIndex == -1) return;

            _wikiCollection.Move(selectIndex, selectIndex - 1);

            _wikiListViewUpdate();
        }

        private void _wikiDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wikiListView.SelectedItem as Wiki;
            if (item == null) return;

            var selectIndex = _wikiListView.SelectedIndex;
            if (selectIndex == -1) return;

            _wikiCollection.Move(selectIndex, selectIndex + 1);

            _wikiListViewUpdate();
        }

        private void _wikiAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wikiTextBox.Text == "") return;

            byte[] id = new byte[64];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(id);
            }

            var item = new Wiki(_wikiTextBox.Text, id);

            if (_wikiCollection.Contains(item)) return;
            _wikiCollection.Add(item);

            _wikiListViewUpdate();
        }

        private void _wikiDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _wikiListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _wikiListView.SelectedItems.OfType<Wiki>().ToArray())
            {
                _wikiCollection.Remove(item);
            }

            _wikiListViewUpdate();
        }

        #endregion

        #region _chatListView

        private void _chatTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_chatListView.SelectedIndex == -1)
                {
                    _chatAddButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _chatListViewUpdate()
        {
            _chatListView_SelectionChanged(this, null);
        }

        private void _chatListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _chatListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _chatUpButton.IsEnabled = false;
                    _chatDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _chatUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _chatUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _chatCollection.Count - 1)
                    {
                        _chatDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _chatDownButton.IsEnabled = true;
                    }
                }

                _chatListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _chatListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _chatListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _chatTextBox.Text = "";

                return;
            }
        }

        private void _chatListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _chatListView.SelectedItems;

            _chatListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _chatListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _chatListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                _chatListViewPasteMenuItem.IsEnabled = Clipboard.ContainsChats();
            }
        }

        private void _chatListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _chatDeleteButton_Click(null, null);
        }

        private void _chatListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _chatListViewCopyMenuItem_Click(null, null);
            _chatDeleteButton_Click(null, null);
        }

        private void _chatListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _chatListView.SelectedItems.OfType<Chat>())
            {
                sb.AppendLine(OutoposConverter.ToChatString(item));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _chatListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tag in Clipboard.GetChats())
            {
                try
                {
                    if (_chatCollection.Contains(tag)) continue;
                    _chatCollection.Add(tag);
                }
                catch (Exception)
                {

                }
            }

            _chatListViewUpdate();
        }

        private void _chatUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _chatListView.SelectedItem as Chat;
            if (item == null) return;

            var selectIndex = _chatListView.SelectedIndex;
            if (selectIndex == -1) return;

            _chatCollection.Move(selectIndex, selectIndex - 1);

            _chatListViewUpdate();
        }

        private void _chatDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _chatListView.SelectedItem as Chat;
            if (item == null) return;

            var selectIndex = _chatListView.SelectedIndex;
            if (selectIndex == -1) return;

            _chatCollection.Move(selectIndex, selectIndex + 1);

            _chatListViewUpdate();
        }

        private void _chatAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_chatTextBox.Text == "") return;

            byte[] id = new byte[64];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(id);
            }

            var item = new Chat(_chatTextBox.Text, id);

            if (_chatCollection.Contains(item)) return;
            _chatCollection.Add(item);

            _chatListViewUpdate();
        }

        private void _chatDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _chatListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _chatListView.SelectedItems.OfType<Chat>().ToArray())
            {
                _chatCollection.Remove(item);
            }

            _chatListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            var digitalSignatureComboBoxItem = _signatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            var uploadSignature = (digitalSignature == null) ? null : digitalSignature.ToString();

            ProfileContent content = null;

            lock (_profileItem.ThisLock)
            {
                if (uploadSignature != _profileItem.UploadSignature)
                {
                    _profileItem.UploadSignature = uploadSignature;

                    if (!string.IsNullOrWhiteSpace(_profileItem.UploadSignature))
                    {
                        _profileItem.Exchange = new Exchange(ExchangeAlgorithm.Rsa2048);
                    }
                    else
                    {
                        _profileItem.Exchange = null;
                    }

                    _profileItem.OldExchanges.Clear();
                }

                lock (_profileItem.TrustSignatures.ThisLock)
                {
                    _profileItem.TrustSignatures.Clear();
                    _profileItem.TrustSignatures.AddRange(_signatureCollection);
                }

                lock (_profileItem.Wikis.ThisLock)
                {
                    _profileItem.Wikis.Clear();
                    _profileItem.Wikis.AddRange(_wikiCollection);
                }

                lock (_profileItem.Chats.ThisLock)
                {
                    _profileItem.Chats.Clear();
                    _profileItem.Chats.AddRange(_chatCollection);
                }

                content = new ProfileContent(
                    _profileItem.Exchange.GetExchangePublicKey(),
                    _profileItem.TrustSignatures,
                    _profileItem.Wikis,
                    _profileItem.Chats);
            }

            if (digitalSignature != null)
            {
                _outoposManager.Upload(content, TimeSpan.Zero, digitalSignature);
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
