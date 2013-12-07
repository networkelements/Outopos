using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml;
using Lair;
using Lair.Properties;
using Library;
using Library.Collections;
using Library.Net.Lair;
using Library.Security;
using a = Library.Net.Amoeba;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for SectionControl.xaml
    /// </summary>
    partial class SectionControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private static Random _random = new Random();

        private LockedDictionary<SectionTreeItem, ChatControl> _chatControls = new LockedDictionary<SectionTreeItem, ChatControl>();

        private Thread _searchThread;
        private Thread _watchThread;

        private volatile bool _refresh;

        private SectionCategorizeTreeViewItem _treeViewItem;

        private LockedHashSet<string> _trustSignatures = new LockedHashSet<string>();

        public SectionControl(LairManager lairManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            _treeViewItem = new SectionCategorizeTreeViewItem(Settings.Instance.SectionControl_SectionCategorizeTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Section)
                {
                    if (!_refresh) this.Update_Title();
                }
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SectionControl_SearchThread";
            _searchThread.Start();

            _watchThread = new Thread(new ThreadStart(this.Watch));
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "LibraryControl_WatchThread";
            _watchThread.Start();

            this.Update();
        }

        public IEnumerable<string> GetTrustSignatures()
        {
            return _trustSignatures;
        }

        private void Search()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (!_refresh) continue;

                    TreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        selectTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (selectTreeViewItem == null) continue;

                    if (selectTreeViewItem is SectionCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (selectTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _chatGrid.Children.Clear();

                            this.Update_Title();
                        }));
                    }
                    else if (selectTreeViewItem is SectionTreeViewItem)
                    {
                        SectionTreeViewItem sectionTreeViewItem = (SectionTreeViewItem)selectTreeViewItem;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (sectionTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _chatGrid.Children.Clear();
                            _chatGrid.Children.Add(_chatControls[sectionTreeViewItem.Value]);

                            this.Update_Title();
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Watch()
        {
            try
            {
                for (; ; )
                {
                    var sectionTreeViewItems = new List<SectionTreeViewItem>();

                    {
                        var categorizeSectionTreeViewItems = new List<SectionCategorizeTreeViewItem>();
                        categorizeSectionTreeViewItems.Add(_treeViewItem);

                        for (int i = 0; i < categorizeSectionTreeViewItems.Count; i++)
                        {
                            categorizeSectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionCategorizeTreeViewItem>());
                            sectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionTreeViewItem>());
                        }
                    }

                    foreach (var item in sectionTreeViewItems)
                    {
                        Dictionary<string, SectionProfileContent> sectionProfiles = new Dictionary<string, SectionProfileContent>();

                        foreach (var profile in _lairManager.GetSectionProfiles(item.Value.Section))
                        {
                            sectionProfiles[profile.Certificate.ToString()] = profile;
                        }

                        SectionProfileContent leaderSectionProfile;
                        if (!sectionProfiles.TryGetValue(item.Value.LeaderSignature, out leaderSectionProfile)) continue;

                        var leaderContent = _lairManager.GetContent(leaderSectionProfile);

                        List<string> trustSignatureList = new List<string>();

                        trustSignatureList.AddRange(leaderContent.TrustSignatures);

                        for (int i = 0; i < trustSignatureList.Count; i++)
                        {
                        }
                    }

                    Thread.Sleep(1000 * 60);
                }
            }
            catch (Exception)
            {

            }
        }

        private void Update()
        {
            Settings.Instance.SectionControl_SectionCategorizeTreeItem = _treeViewItem.Value;

            {
                var sectionTreeViewItems = new List<SectionTreeViewItem>();

                {
                    var categorizeSectionTreeViewItems = new List<SectionCategorizeTreeViewItem>();
                    categorizeSectionTreeViewItems.Add(_treeViewItem);

                    for (int i = 0; i < categorizeSectionTreeViewItems.Count; i++)
                    {
                        categorizeSectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionCategorizeTreeViewItem>());
                        sectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionTreeViewItem>());
                    }
                }

                foreach (var item in sectionTreeViewItems)
                {
                    if (_chatControls.ContainsKey(item.Value)) continue;

                    var chatControl = new ChatControl(this, _lairManager, _bufferManager, item);
                    chatControl.Height = Double.NaN;
                    chatControl.Width = Double.NaN;

                    _chatControls[item.Value] = chatControl;
                }

                foreach (var item in _chatControls.ToArray())
                {
                    if (sectionTreeViewItems.Any(n => n.Value == item.Key)) continue;

                    item.Value.Dispose();
                    _chatControls.Remove(item.Key);
                }
            }

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
        }

        private void Update_Title()
        {
            if (_refresh) return;

            if (_mainWindow.SelectedTab == MainWindowTabType.Section)
            {
                if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
                {
                    var selectItem = (SectionCategorizeTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Name);
                }
                else if (_treeView.SelectedItem is SectionTreeViewItem)
                {
                    var selectItem = (SectionTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToSectionString(selectItem.Value.Section));
                }
            }
        }

        #region _treeView

        private Point _startPoint = new Point(-1, -1);

        private void _treeView_PreviewDragOver(object sender, DragEventArgs e)
        {
            Point position = MouseUtilities.GetMousePosition(_treeView);

            if (position.Y < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallDecrement);
                }
                catch (Exception)
                {

                }
            }
            else if ((_treeView.ActualHeight - position.Y) < 50)
            {
                var peer = ItemsControlAutomationPeer.CreatePeerForElement(_treeView);
                var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

                try
                {
                    scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallIncrement);
                }
                catch (Exception)
                {

                }
            }
        }

        private void _treeView_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed && e.RightButton == System.Windows.Input.MouseButtonState.Released)
            {
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_treeViewItem == _treeView.SelectedItem) return;

                    DataObject data = new DataObject("TreeViewItem", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("TreeViewItem"))
            {
                var sourceItem = (TreeViewItem)e.Data.GetData("TreeViewItem");

                if (sourceItem is SectionCategorizeTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is SectionCategorizeTreeViewItem)
                    {
                        var s = (SectionCategorizeTreeViewItem)sourceItem;
                        var d = (SectionCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is SectionCategorizeTreeViewItem)
                        {
                            var p = (SectionCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.Children.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.Children.Clear();
                            p.Value.Children.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.Children.Add(s.Value);
                        d.Update();
                    }
                }
                else if (sourceItem is SectionTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is SectionCategorizeTreeViewItem)
                    {
                        var s = (SectionTreeViewItem)sourceItem;
                        var d = (SectionCategorizeTreeViewItem)destinationItem;

                        if (d.Value.SectionTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is SectionCategorizeTreeViewItem)
                        {
                            var p = (SectionCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.SectionTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.SectionTreeItems.Clear();
                            p.Value.SectionTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.SectionTreeItems.Add(s.Value);
                        d.Update();
                    }
                }

                this.Update();
            }
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            Point lposition = e.GetPosition(_treeView);

            if ((_treeView.ActualWidth - lposition.X) < 15
                || (_treeView.ActualHeight - lposition.Y) < 15)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView_SelectedItemChanged(null, null);
            }
            else
            {
                _startPoint = new Point(-1, -1);
            }
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            this.Update();
        }

        private void _sectionCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (SectionCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetSectionCategorizeTreeItems(new SectionCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parent = (SectionCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionCategorizeTreeItems(new SectionCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _sectionCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sectionTreeViewItems = new List<SectionTreeViewItem>();

            {
                var categorizeSectionTreeViewItems = new List<SectionCategorizeTreeViewItem>();
                categorizeSectionTreeViewItems.Add(_treeViewItem);

                for (int i = 0; i < categorizeSectionTreeViewItems.Count; i++)
                {
                    categorizeSectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionCategorizeTreeViewItem>());
                    sectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in sectionTreeViewItems)
            {
                sb.AppendLine(LairConverter.ToSectionString(item.Value.Section, item.Value.LeaderSignature));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Section, item.Value.LeaderSignature));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _sectionCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetSectionCategorizeTreeItems())
            {
                selectTreeViewItem.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetSectionTreeItems())
            {
                selectTreeViewItem.Value.SectionTreeItems.Add(item);
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemTrustOnMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemTrustOffMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemChartMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as SectionTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

        }

        private void _sectionTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<SectionCategorizeTreeViewItem>().LastOrDefault() as SectionCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.SectionTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionTreeItems(new SectionTreeItem[] { selectTreeViewItem.Value });

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<SectionCategorizeTreeViewItem>().LastOrDefault() as SectionCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.SectionTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionTreeItems(new SectionTreeItem[] { selectTreeViewItem.Value });
        }

        private void _sectionTreeItemTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(LairConverter.ToSectionString(selectTreeViewItem.Value.Section, selectTreeViewItem.Value.LeaderSignature));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Section, selectTreeViewItem.Value.LeaderSignature));

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemNewMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {

            }
        }
    }
}
