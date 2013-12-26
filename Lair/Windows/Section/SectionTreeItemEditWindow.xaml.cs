using System;
using System.Collections.Generic;
using System.Linq;
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
using Library;
using Library.Net.Lair;
using Library.Security;
using System.ComponentModel;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for SectionTreeItemEditWindow.xaml
    /// </summary>
    partial class SectionTreeItemEditWindow : Window
    {
        private SectionTreeItem _sectionTreeItem;
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private string _uploadSignature;
        private ObservableCollectionEx<string> _trustSignatureCollection = new ObservableCollectionEx<string>();
        private ObservableCollectionEx<Wiki> _wikiCollection;
        private ObservableCollectionEx<Chat> _chatCollection;

        public SectionTreeItemEditWindow(SectionTreeItem sectionTreeItem, LairManager lairManager, BufferManager bufferManager)
        {
            _sectionTreeItem = sectionTreeItem;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            _wikiTextBox.MaxLength = Wiki.MaxNameLength;
            _chatTextBox.MaxLength = Chat.MaxNameLength;

            lock (_sectionTreeItem.ThisLock)
            {
                _tagTextBox.Text = MessageConverter.ToSectionString(_sectionTreeItem.Tag);
                _sectionLeaderSignatureTextBox.Text = _sectionTreeItem.LeaderSignature;
                _uploadSignature = _sectionTreeItem.UploadSignature;
                _trustSignatureCollection.AddRange(_sectionTreeItem.TrustSignatures);
                _wikiCollection = new ObservableCollectionEx<Wiki>(_sectionTreeItem.Wikis);
                _chatCollection = new ObservableCollectionEx<Chat>(_sectionTreeItem.Chats);
                _commentTextBox.Text = _sectionTreeItem.Comment;
            }

            _trustSignatureListView.ItemsSource = _trustSignatureCollection;
            _wikiListView.ItemsSource = _wikiCollection;
            _chatListView.ItemsSource = _chatCollection;

            _signatureComboBox.ItemsSource = digitalSignatureCollection;

            this.Sort();
        }

        private void Sort()
        {
            _trustSignatureListView.Items.SortDescriptions.Clear();
            _trustSignatureListView.Items.SortDescriptions.Add(new SortDescription(null, ListSortDirection.Ascending));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int index = 0; index < Settings.Instance.Global_DigitalSignatureCollection.Count; index++)
            {
                if (Settings.Instance.Global_DigitalSignatureCollection[index].ToString() == _uploadSignature)
                {
                    _signatureComboBox.SelectedIndex = index + 1;

                    break;
                }
            }

            WindowPosition.Move(this);
        }

        #region _trustSignature

        private void _trustSignatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _trustSignatureListView.SelectedItems;

            _trustSignatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _trustSignatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _trustSignatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                bool flag = false;

                if (Clipboard.ContainsText())
                {
                    var line = Clipboard.GetText().Split('\r', '\n');
                    flag = Signature.HasSignature(line[0]);
                }

                _trustSignatureListViewPasteMenuItem.IsEnabled = flag;
            }
        }

        private void _trustSignatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                _trustSignatureCollection.Remove(item);
            }
        }

        private void _trustSignatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _trustSignatureListViewCopyMenuItem_Click(null, null);
            _trustSignatureListViewDeleteMenuItem_Click(null, null);
        }

        private void _trustSignatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _trustSignatureListView.SelectedItems.OfType<string>().ToArray())
            {
                sb.AppendLine(item);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _trustSignatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var signature in Clipboard.GetText().Split('\r', '\n'))
            {
                if (!Signature.HasSignature(signature)) continue;

                if (_trustSignatureCollection.Contains(signature)) continue;
                _trustSignatureCollection.Add(signature);
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
                sb.AppendLine(LairConverter.ToWikiString(item, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _wikiListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tuple in Clipboard.GetWikis())
            {
                try
                {
                    if (_wikiCollection.Contains(tuple.Item1)) continue;
                    _wikiCollection.Add(tuple.Item1);
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
            (new System.Security.Cryptography.RNGCryptoServiceProvider()).GetBytes(id);

            var item = new Wiki(id, _wikiTextBox.Text);

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
                sb.AppendLine(LairConverter.ToChatString(item, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _chatListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tuple in Clipboard.GetChats())
            {
                try
                {
                    if (_chatCollection.Contains(tuple.Item1)) continue;
                    _chatCollection.Add(tuple.Item1);
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
            (new System.Security.Cryptography.RNGCryptoServiceProvider()).GetBytes(id);

            var item = new Chat(id, _chatTextBox.Text);

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

            lock (_sectionTreeItem.ThisLock)
            {
                _sectionTreeItem.UploadSignature = (digitalSignature == null) ? null : digitalSignature.ToString();

                lock (_sectionTreeItem.TrustSignatures.ThisLock)
                {
                    _sectionTreeItem.TrustSignatures.Clear();
                    _sectionTreeItem.TrustSignatures.AddRange(_trustSignatureCollection);
                }

                lock (_sectionTreeItem.Wikis.ThisLock)
                {
                    _sectionTreeItem.Wikis.Clear();
                    _sectionTreeItem.Wikis.AddRange(_wikiCollection);
                }

                lock (_sectionTreeItem.Chats.ThisLock)
                {
                    _sectionTreeItem.Chats.Clear();
                    _sectionTreeItem.Chats.AddRange(_chatCollection);
                }

                _sectionTreeItem.Comment = _commentTextBox.Text;
            }

            if (digitalSignature != null)
            {
                _lairManager.UploadSectionProfile(_sectionTreeItem.Tag,
                    _sectionTreeItem.Comment,
                    _sectionTreeItem.Exchange.GetPublicKey(),
                    _sectionTreeItem.TrustSignatures,
                    _sectionTreeItem.Wikis,
                    _sectionTreeItem.Chats,
                    digitalSignature);
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
