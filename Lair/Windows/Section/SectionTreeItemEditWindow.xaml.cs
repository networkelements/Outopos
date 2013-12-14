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
        private ObservableCollectionEx<Archive> _archiveCollection;
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

            lock (_sectionTreeItem.ThisLock)
            {
                _tagTextBox.Text = MessageConverter.ToSectionString(_sectionTreeItem.Tag);
                _sectionLeaderSignatureTextBox.Text = _sectionTreeItem.LeaderSignature;
                _uploadSignature = _sectionTreeItem.UploadSignature;
                _trustSignatureCollection.AddRange(_sectionTreeItem.TrustSignatures);
                _archiveCollection = new ObservableCollectionEx<Archive>(_sectionTreeItem.Archives);
                _chatCollection = new ObservableCollectionEx<Chat>(_sectionTreeItem.Chats);
            }

            _trustSignatureListView.ItemsSource = _trustSignatureCollection;
            _archiveListView.ItemsSource = _archiveCollection;
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

        #region _archiveListView

        private void _archiveTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (_archiveListView.SelectedIndex == -1)
                {
                    _archiveAddButton_Click(null, null);
                }

                e.Handled = true;
            }
        }

        private void _archiveListViewUpdate()
        {
            _archiveListView_SelectionChanged(this, null);
        }

        private void _archiveListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _archiveListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _archiveUpButton.IsEnabled = false;
                    _archiveDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _archiveUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _archiveUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _archiveCollection.Count - 1)
                    {
                        _archiveDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _archiveDownButton.IsEnabled = true;
                    }
                }

                _archiveListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _archiveListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _archiveListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _archiveTextBox.Text = "";

                return;
            }
        }

        private void _archiveListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _archiveListView.SelectedItems;

            _archiveListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _archiveListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _archiveListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                _archiveListViewPasteMenuItem.IsEnabled = Clipboard.ContainsArchives();
            }
        }

        private void _archiveListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _archiveDeleteButton_Click(null, null);
        }

        private void _archiveListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _archiveListViewCopyMenuItem_Click(null, null);
            _archiveDeleteButton_Click(null, null);
        }

        private void _archiveListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _archiveListView.SelectedItems.OfType<Archive>())
            {
                sb.AppendLine(LairConverter.ToArchiveString(item, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _archiveListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var tuple in Clipboard.GetArchives())
            {
                try
                {
                    if (_archiveCollection.Contains(tuple.Item1)) continue;
                    _archiveCollection.Add(tuple.Item1);
                }
                catch (Exception)
                {

                }
            }

            _archiveListViewUpdate();
        }

        private void _archiveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _archiveListView.SelectedItem as Archive;
            if (item == null) return;

            var selectIndex = _archiveListView.SelectedIndex;
            if (selectIndex == -1) return;

            _archiveCollection.Move(selectIndex, selectIndex - 1);

            _archiveListViewUpdate();
        }

        private void _archiveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _archiveListView.SelectedItem as Archive;
            if (item == null) return;

            var selectIndex = _archiveListView.SelectedIndex;
            if (selectIndex == -1) return;

            _archiveCollection.Move(selectIndex, selectIndex + 1);

            _archiveListViewUpdate();
        }

        private void _archiveAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_archiveTextBox.Text == "") return;

            byte[] id = new byte[64];
            (new System.Security.Cryptography.RNGCryptoServiceProvider()).GetBytes(id);

            var item = new Archive(id, _archiveTextBox.Text);

            if (_archiveCollection.Contains(item)) return;
            _archiveCollection.Add(item);

            _archiveListViewUpdate();
        }

        private void _archiveDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _archiveListView.SelectedIndex;
            if (selectIndex == -1) return;

            foreach (var item in _archiveListView.SelectedItems.OfType<Archive>().ToArray())
            {
                _archiveCollection.Remove(item);
            }

            _archiveListViewUpdate();
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

            SectionProfileContent content = null;

            lock (_sectionTreeItem.ThisLock)
            {
                _sectionTreeItem.UploadSignature = (digitalSignature == null) ? null : digitalSignature.ToString();

                lock (_sectionTreeItem.TrustSignatures.ThisLock)
                {
                    _sectionTreeItem.TrustSignatures.Clear();
                    _sectionTreeItem.TrustSignatures.AddRange(_trustSignatureCollection);
                }

                lock (_sectionTreeItem.Archives.ThisLock)
                {
                    _sectionTreeItem.Archives.Clear();
                    _sectionTreeItem.Archives.AddRange(_archiveCollection);
                }

                lock (_sectionTreeItem.Chats.ThisLock)
                {
                    _sectionTreeItem.Chats.Clear();
                    _sectionTreeItem.Chats.AddRange(_chatCollection);
                }

                content = new SectionProfileContent(null,
                    _sectionTreeItem.Exchange.GetPublicKey(),
                    _sectionTreeItem.TrustSignatures,
                    _sectionTreeItem.Archives,
                    _sectionTreeItem.Chats);
            }

            if (digitalSignature != null)
            {
                _lairManager.Upload(_sectionTreeItem.Tag, content, digitalSignature);
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
