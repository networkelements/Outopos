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
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using System.Collections;
using Library;

namespace Lair.Windows
{
    delegate void ChannelJoinEventHandler(object sender, Channel channel);

    /// <summary>
    /// ChannelListWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChannelListWindow : Window
    {
        private LairManager _lairManager;
        private ObservableCollection<Channel> _channelCollection;

        public event ChannelJoinEventHandler ChannelJoinEvent;

        public ChannelListWindow(IEnumerable<Channel> channels, LairManager lairManager)
        {
            _lairManager = lairManager;

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            _channelCollection = new ObservableCollection<Channel>(channels);
            _listView.ItemsSource = _channelCollection;

            CollectionViewSource.GetDefaultView(_listView.ItemsSource).Filter = (object o) =>
            {
                string searchText = _searchTextBox.Text;
                if (string.IsNullOrWhiteSpace(searchText)) return true;

                var item = o as Channel;
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

        protected virtual void OnChannelJoinEvent(Channel channel)
        {
            if (this.ChannelJoinEvent != null)
            {
                this.ChannelJoinEvent(this, channel);
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

            foreach (var channel in _listView.SelectedItems.Cast<Channel>().ToArray())
            {
                _channelCollection.Remove(channel);

                this.OnChannelJoinEvent(channel);
            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewJoinMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetChannels(_listView.SelectedItems.Cast<Channel>());
        }

        private void _listViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var channel in _listView.SelectedItems.Cast<Channel>())
            {
                sb.AppendLine(LairConverter.ToChannelString(channel));
                sb.AppendLine(MessageConverter.ToInfoMessage(channel));
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

                if (headerClicked != Settings.Instance.ChannelListWindow_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ChannelListWindow_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.ChannelListWindow_LastHeaderClicked = headerClicked;
                Settings.Instance.ChannelListWindow_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ChannelListWindow_LastHeaderClicked != null)
                {
                    Sort(Settings.Instance.ChannelListWindow_LastHeaderClicked, Settings.Instance.ChannelListWindow_ListSortDirection);
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ChannelListWindow_Name)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ChannelListWindow_Id)
            {
                ListCollectionView listCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                listCollectionView.CustomSort = new ChannelIdComparer(direction);
            }
        }

        sealed class ChannelIdComparer : IComparer
        {
            private ListSortDirection _direction;

            public ChannelIdComparer(ListSortDirection direction)
            {
                _direction = direction;
            }

            public int Compare(object x, object y)
            {
                var cx = x as Channel;
                var cy = y as Channel;

                if (_direction == ListSortDirection.Ascending) return Collection.Compare(cx.Id, cy.Id);
                if (_direction == ListSortDirection.Descending) return Collection.Compare(cy.Id, cx.Id);

                return 0;
            }
        }

        #endregion

        private void _joinButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var channel in _listView.SelectedItems.Cast<Channel>().ToArray())
            {
                _channelCollection.Remove(channel);

                this.OnChannelJoinEvent(channel);
            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
