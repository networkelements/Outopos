using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Outopos.Properties;
using Library.Net;
using Library.Net.Outopos;
using Library.Security;
using System.Collections;
using Library;

namespace Outopos.Windows
{
    delegate void ChatJoinEventHandler(object sender, Chat tag);

    /// <summary>
    /// ChatListWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChatListWindow : Window
    {
        private OutoposManager _outoposManager;
        private ObservableCollectionEx<Chat> _chatCollection;

        public event ChatJoinEventHandler ChatJoinEvent;

        public ChatListWindow(IEnumerable<Chat> chats, OutoposManager outoposManager)
        {
            _outoposManager = outoposManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Outopos.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _chatCollection = new ObservableCollectionEx<Chat>(chats);
            _listView.ItemsSource = _chatCollection;

            CollectionViewSource.GetDefaultView(_listView.ItemsSource).Filter = (object o) =>
            {
                string searchText = _searchTextBox.Text;
                if (string.IsNullOrWhiteSpace(searchText)) return true;

                var item = o as Chat;
                if (item == null) return false;

                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                var text = (item.Name ?? "").ToLower();

                foreach (var word in words)
                {
                    if (!text.Contains(word))
                    {
                        return false;
                    }
                }

                return true;
            };

            this.Sort();
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        protected virtual void OnChatJoinEvent(Chat chat)
        {
            if (this.ChatJoinEvent != null)
            {
                this.ChatJoinEvent(this, chat);
            }
        }

        #region _listView

        private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _joinButton.IsEnabled = (_listView.SelectedIndex != -1);
        }

        private void _listView_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_listView.GetCurrentIndex(e.GetPosition) < 0) return;

            foreach (var chat in _listView.SelectedItems.Cast<Chat>().ToArray())
            {
                _chatCollection.Remove(chat);

                this.OnChatJoinEvent(chat);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
            _listViewJoinMenuItem.IsEnabled = (selectItems != null && selectItems.Count > 0);
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetChats(_listView.SelectedItems.OfType<Chat>());
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var chat in _listView.SelectedItems.Cast<Chat>())
            {
                sb.AppendLine(OutoposConverter.ToChatString(chat));
                sb.AppendLine(MessageConverter.ToInfoMessage(chat));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _listViewJoinMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _joinButton_Click(null, null);
        }

        #endregion

        private void _searchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CollectionViewSource.GetDefaultView(_listView.ItemsSource).Refresh();

                e.Handled = true;
            }
        }

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.ChatListWindow_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ChatListWindow_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.ChatListWindow_LastHeaderClicked = headerClicked;
                Settings.Instance.ChatListWindow_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ChatListWindow_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.ChatListWindow_LastHeaderClicked, Settings.Instance.ChatListWindow_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ChatListWindow_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ChatListWindow_Id)
            {
                ListCollectionView listCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                listCollectionView.CustomSort = new ChatIdComparer(direction);
            }
        }

        sealed class ChatIdComparer : IComparer
        {
            private ListSortDirection _direction;

            public ChatIdComparer(ListSortDirection direction)
            {
                _direction = direction;
            }

            public int Compare(object x, object y)
            {
                if (_direction == ListSortDirection.Ascending) return Unsafe.Compare(((Chat)x).Id, ((Chat)y).Id);
                if (_direction == ListSortDirection.Descending) return Unsafe.Compare(((Chat)y).Id, ((Chat)x).Id);

                return 0;
            }
        }

        #endregion

        private void _joinButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var chat in _listView.SelectedItems.Cast<Chat>().ToArray())
            {
                _chatCollection.Remove(chat);

                this.OnChatJoinEvent(chat);
            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
