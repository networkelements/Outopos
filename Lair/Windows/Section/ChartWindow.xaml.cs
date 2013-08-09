using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library;
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// ChartWindow.xaml の相互作用ロジック
    /// </summary>
    partial class ChartWindow : Window
    {
        private Section _section;
        private string _leaderSignature;
        private LairManager _lairManager;
        private ObservableCollection<Channel> _channelCollection;
        private ObservableCollection<string> _signatureCollection;

        public ChartWindow(Section section, string leaderSignature, LairManager lairManager)
        {
            _section = section;
            _leaderSignature = leaderSignature;
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

            _channelCollection = new ObservableCollection<Channel>();
            _channelListView.ItemsSource = _channelCollection;

            _signatureCollection = new ObservableCollection<string>();
            _signatureListView.ItemsSource = _signatureCollection;

            {
                Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                foreach (var leader in _lairManager.GetLeaders(_section))
                {
                    leaderDictionary[leader.Certificate.ToString()] = leader;
                }

                {
                    Leader leader;

                    if (leaderDictionary.TryGetValue(_leaderSignature, out leader))
                    {
                        Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                        foreach (var creator in _lairManager.GetCreators(_section))
                        {
                            creatorDictionary[creator.Certificate.ToString()] = creator;
                        }

                        List<ChartCreatorTreeViewItem> chartCreatorTreeViewItems = new List<ChartCreatorTreeViewItem>();

                        foreach (var creatorSignature in leader.CreatorSignatures)
                        {
                            Creator creator;

                            if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                            {
                                chartCreatorTreeViewItems.Add(new ChartCreatorTreeViewItem(creator));
                            }
                            else
                            {
                                chartCreatorTreeViewItems.Add(new ChartCreatorTreeViewItem(creatorSignature));
                            }
                        }

                        Dictionary<string, Manager> managerDictionary = new Dictionary<string, Manager>();

                        foreach (var manager in _lairManager.GetManagers(_section))
                        {
                            managerDictionary[manager.Certificate.ToString()] = manager;
                        }

                        List<ChartManagerTreeViewItem> chartManagerTreeViewItems = new List<ChartManagerTreeViewItem>();

                        foreach (var managerSignature in leader.ManagerSignatures)
                        {
                            Manager manager;

                            if (managerDictionary.TryGetValue(managerSignature, out manager))
                            {
                                chartManagerTreeViewItems.Add(new ChartManagerTreeViewItem(manager));
                            }
                            else
                            {
                                chartManagerTreeViewItems.Add(new ChartManagerTreeViewItem(managerSignature));
                            }
                        }

                        TreeViewItem leaderTreeViewItem = new TreeViewItem();
                        leaderTreeViewItem.IsExpanded = true;
                        leaderTreeViewItem.Header = new TextBlock() { Text = LanguagesManager.Instance.ChartWindow_Leader };
                        leaderTreeViewItem.Items.Add(new SignatureTreeViewItem(leader));

                        TreeViewItem creatorTreeViewItem = new TreeViewItem();
                        creatorTreeViewItem.IsExpanded = true;
                        creatorTreeViewItem.Header = new TextBlock() { Text = LanguagesManager.Instance.ChartWindow_Creator };

                        foreach (var item in chartCreatorTreeViewItems)
                        {
                            creatorTreeViewItem.Items.Add(item);
                        }

                        TreeViewItem managerTreeViewItem = new TreeViewItem();
                        managerTreeViewItem.IsExpanded = true;
                        managerTreeViewItem.Header = new TextBlock() { Text = LanguagesManager.Instance.ChartWindow_Manager };

                        foreach (var item in chartManagerTreeViewItems)
                        {
                            managerTreeViewItem.Items.Add(item);
                        }

                        _treeView.Items.Add(leaderTreeViewItem);
                        _treeView.Items.Add(creatorTreeViewItem);
                        _treeView.Items.Add(managerTreeViewItem);
                    }
                }
            }

            CollectionViewSource.GetDefaultView(_channelListView.ItemsSource).Filter = (object o) =>
            {
                string searchText = _channelSearchTextBox.Text;
                if (string.IsNullOrWhiteSpace(searchText)) return true;

                var item = o as Channel;
                if (item == null) return false;

                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                var text = (item.Name ?? "").ToLower();

                return words.All(n => text.Contains(n));
            };

            CollectionViewSource.GetDefaultView(_signatureListView.ItemsSource).Filter = (object o) =>
            {
                string searchText = _signatureSearchTextBox.Text;
                if (string.IsNullOrWhiteSpace(searchText)) return true;

                var item = o as string;
                if (item == null) return false;

                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                var text = (item ?? "").ToLower();

                return words.All(n => text.Contains(n));
            };
        }

        protected override void OnInitialized(EventArgs e)
        {
            WindowPosition.Move(this);

            base.OnInitialized(e);
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void _chartTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var treeViewItem = (IChartSignature)_treeView.SelectedItem;

            Clipboard.SetText(treeViewItem.Signature);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_treeView.SelectedItem is SignatureTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Hidden;
                _signatureGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (_treeView.SelectedItem is ChartCreatorTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Visible;
                _signatureGrid.Visibility = System.Windows.Visibility.Hidden;

                var selectTreeViewItem = (ChartCreatorTreeViewItem)_treeView.SelectedItem;

                _channelCollection.Clear();

                if (selectTreeViewItem.Value != null)
                {
                    foreach (var channel in selectTreeViewItem.Value.Channels)
                    {
                        _channelCollection.Add(channel);
                    }

                    this.Channel_Sort();
                }
            }
            else if (_treeView.SelectedItem is ChartManagerTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Hidden;
                _signatureGrid.Visibility = System.Windows.Visibility.Visible;

                var selectTreeViewItem = (ChartManagerTreeViewItem)_treeView.SelectedItem;

                _signatureCollection.Clear();

                if (selectTreeViewItem.Value != null)
                {
                    foreach (var signature in selectTreeViewItem.Value.TrustSignatures)
                    {
                        _signatureCollection.Add(signature);
                    }

                    this.Signature_Sort();
                }
            }
            else
            {
                _channelGrid.Visibility = System.Windows.Visibility.Hidden;
                _signatureGrid.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        #region Channel

        private void _channelListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _channelListView.SelectedItems;

            _channelListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _channelListViewCopyInfoMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _channelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void _channelListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetChannels(_channelListView.SelectedItems.Cast<Channel>());
        }

        private void _channelListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var channel in _channelListView.SelectedItems.Cast<Channel>())
            {
                sb.AppendLine(LairConverter.ToChannelString(channel));
                sb.AppendLine(MessageConverter.ToInfoMessage(channel));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _channelSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CollectionViewSource.GetDefaultView(_channelListView.ItemsSource).Refresh();

                e.Handled = true;
            }
        }

        #region Sort

        private void Channel_Sort()
        {
            _channelGridViewColumnHeaderClickedHandler(null, null);
        }

        private void _channelGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.ChartWindow_Channel_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ChartWindow_Channel_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Channel_Sort(headerClicked, direction);

                Settings.Instance.ChartWindow_Channel_LastHeaderClicked = headerClicked;
                Settings.Instance.ChartWindow_Channel_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ChartWindow_Channel_LastHeaderClicked != null)
                {
                    Channel_Sort(Settings.Instance.ChartWindow_Channel_LastHeaderClicked, Settings.Instance.ChartWindow_Channel_ListSortDirection);
                }
            }
        }

        private void Channel_Sort(string sortBy, ListSortDirection direction)
        {
            _channelListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ChartWindow_Name)
            {
                _channelListView.Items.SortDescriptions.Add(new SortDescription("Name", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ChartWindow_Id)
            {
                ListCollectionView listCollectionView = (ListCollectionView)CollectionViewSource.GetDefaultView(_channelListView.ItemsSource);
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

        #endregion

        #region Signature

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var signature in _signatureListView.SelectedItems.Cast<string>())
            {
                sb.AppendLine(signature);
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _signatureSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CollectionViewSource.GetDefaultView(_signatureListView.ItemsSource).Refresh();

                e.Handled = true;
            }
        }

        #region Sort

        private void Signature_Sort()
        {
            _signatureGridViewColumnHeaderClickedHandler(null, null);
        }

        private void _signatureGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.ChartWindow_Signature_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ChartWindow_Signature_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Signature_Sort(headerClicked, direction);

                Settings.Instance.ChartWindow_Signature_LastHeaderClicked = headerClicked;
                Settings.Instance.ChartWindow_Signature_ListSortDirection = direction;
            }
            else
            {
                if (Settings.Instance.ChartWindow_Signature_LastHeaderClicked != null)
                {
                    Signature_Sort(Settings.Instance.ChartWindow_Signature_LastHeaderClicked, Settings.Instance.ChartWindow_Channel_ListSortDirection);
                }
            }
        }

        private void Signature_Sort(string sortBy, ListSortDirection direction)
        {
            _signatureListView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ChartWindow_Signature)
            {
                _signatureListView.Items.SortDescriptions.Add(new SortDescription("Signature", direction));
            }
        }

        #endregion

        #endregion
    }
}
