using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using System.Collections.ObjectModel;

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

            {
                Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                foreach (var leader in _lairManager.GetLeaders(_section))
                {
                    leaderDictionary[leader.Certificate.ToString()] = leader;
                }

                {
                    Leader leader;
                    HashSet<Creator> creators = new HashSet<Creator>();
                    HashSet<Manager> managers = new HashSet<Manager>();

                    if (leaderDictionary.TryGetValue(_leaderSignature, out leader))
                    {
                        Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                        foreach (var creator in _lairManager.GetCreators(_section))
                        {
                            creatorDictionary[creator.Certificate.ToString()] = creator;
                        }

                        foreach (var creatorSignature in leader.CreatorSignatures)
                        {
                            Creator creator;

                            if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                            {
                                creators.Add(creator);
                            }
                        }

                        Dictionary<string, Manager> managerDictionary = new Dictionary<string, Manager>();

                        foreach (var manager in _lairManager.GetManagers(_section))
                        {
                            managerDictionary[manager.Certificate.ToString()] = manager;
                        }

                        foreach (var managerSignature in leader.ManagerSignatures)
                        {
                            Manager manager;

                            if (managerDictionary.TryGetValue(managerSignature, out manager))
                            {
                                managers.Add(manager);
                            }
                        }
                    }

                    List<ChartCreatorTreeViewItem> chartCreatorTreeViewItems = new List<ChartCreatorTreeViewItem>();

                    foreach (var creator in creators)
                    {
                        chartCreatorTreeViewItems.Add(new ChartCreatorTreeViewItem(creator));
                    }

                    List<ChartManagerTreeViewItem> chartManagerTreeViewItems = new List<ChartManagerTreeViewItem>();

                    foreach (var manager in managers)
                    {
                        chartManagerTreeViewItems.Add(new ChartManagerTreeViewItem(manager));
                    }

                    ChartLeaderTreeViewItem chartLeaderTreeViewItem = new ChartLeaderTreeViewItem(leader, chartCreatorTreeViewItems, chartManagerTreeViewItems);
                    chartLeaderTreeViewItem.IsExpanded = true;
                    _treeView.Items.Add(chartLeaderTreeViewItem);
                }
            }
        }

        private void _closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_treeView.SelectedItem is ChartLeaderTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Hidden;
                _signatureGrid.Visibility = System.Windows.Visibility.Hidden;
            }
            else if (_treeView.SelectedItem is ChartCreatorTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Visible;
                _signatureGrid.Visibility = System.Windows.Visibility.Hidden;

                var selectTreeViewItem = (ChartCreatorTreeViewItem)_treeView.SelectedItem;

                _channelListView.Items.Clear();

                foreach (var channel in selectTreeViewItem.Value.Channels)
                {
                    _channelListView.Items.Add(channel);
                }
            }
            else if (_treeView.SelectedItem is ChartManagerTreeViewItem)
            {
                _channelGrid.Visibility = System.Windows.Visibility.Hidden;
                _signatureGrid.Visibility = System.Windows.Visibility.Visible;

                var selectTreeViewItem = (ChartManagerTreeViewItem)_treeView.SelectedItem;

                _signatureListView.Items.Clear();

                foreach (var signature in selectTreeViewItem.Value.TrustSignatures)
                {
                    _signatureListView.Items.Add(signature);
                }
            }
        }

        #region Channel

        private void _channelListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _channelGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

        }

        private void _channelListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void _channelListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelListViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        #endregion

        #region Signature

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _signatureGridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _signatureSearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

        }

        #endregion
    }

    class ChartLeaderTreeViewItem : TreeViewItem
    {
        private Leader _value;
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();

        public ChartLeaderTreeViewItem(Leader leader, IEnumerable<ChartCreatorTreeViewItem> creatorTreeViewItems, IEnumerable<ChartManagerTreeViewItem> managerTreeViewItems)
            : base()
        {
            _value = leader;

            foreach (var treeViewItem in creatorTreeViewItems)
            {
                _listViewItemCollection.Add(treeViewItem);
            }

            foreach (var treeViewItem in managerTreeViewItems)
            {
                _listViewItemCollection.Add(treeViewItem);
            }

            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Update();
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            base.Header = new TextBlock() { Text = string.Format("Leader {0}", _value.Certificate.ToString()) };

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.Cast<object>().ToList();

            list.Sort(delegate(object x, object y)
            {
                if (x is ChartCreatorTreeViewItem)
                {
                    if (y is ChartCreatorTreeViewItem)
                    {
                        var vx = ((ChartCreatorTreeViewItem)x).Value;
                        var vy = ((ChartCreatorTreeViewItem)y).Value;

                        int c = vx.Certificate.ToString().CompareTo(vy.Certificate.ToString());
                        if (c != 0) return c;
                    }
                    else if (y is ChartManagerTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is ChartManagerTreeViewItem)
                {
                    if (y is ChartManagerTreeViewItem)
                    {
                        var vx = ((ChartManagerTreeViewItem)x).Value;
                        var vy = ((ChartManagerTreeViewItem)y).Value;

                        int c = vx.Certificate.ToString().CompareTo(vy.Certificate.ToString());
                        if (c != 0) return c;
                    }
                    else if (y is ChartCreatorTreeViewItem)
                    {
                        return -1;
                    }
                }

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }
        }

        public Leader Value
        {
            get
            {
                return _value;
            }
        }
    }

    class ChartCreatorTreeViewItem : TreeViewItem
    {
        private Creator _value;

        public ChartCreatorTreeViewItem(Creator creator)
            : base()
        {
            _value = creator;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Update();
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            base.Header = new TextBlock() { Text = string.Format("Creator {0}", _value.Certificate.ToString()) };
        }

        public Creator Value
        {
            get
            {
                return _value;
            }
        }
    }

    class ChartManagerTreeViewItem : TreeViewItem
    {
        private Manager _value;

        public ChartManagerTreeViewItem(Manager manager)
            : base()
        {
            _value = manager;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Update();
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            base.Header = new TextBlock() { Text = string.Format("Manager {0}", _value.Certificate.ToString()) };
        }

        public Manager Value
        {
            get
            {
                return _value;
            }
        }
    }
}
