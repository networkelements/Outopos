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
using a = Library.Net.Lair;

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

        private Thread _searchThread = null;
        private Thread _watchThread;

        private volatile bool _refresh = false;

        private static Random _random = new Random();

        private SectionCategorizeTreeViewItem _treeViewItem;
        private LockedDictionary<SectionTreeViewItem, ChatControl> _chatControls = new LockedDictionary<SectionTreeViewItem, ChatControl>();

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

            _watchThread = new Thread(this.Watch);
            _watchThread.Priority = ThreadPriority.Highest;
            _watchThread.IsBackground = true;
            _watchThread.Name = "SectionControl_Watch";
            _watchThread.Start();

            this.Update();
        }

        private void Search()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (!_refresh) continue;

                    TreeViewItem tempTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        tempTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (tempTreeViewItem is SectionCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            foreach (var item in _chatGrid.Children.OfType<ChatControl>())
                            {
                                item.Visibility = System.Windows.Visibility.Hidden;
                            }

                            this.Update_Title();
                        }));
                    }
                    else if (tempTreeViewItem is SectionTreeViewItem)
                    {
                        var sectionTreeViewItem = (SectionTreeViewItem)tempTreeViewItem;

                        ChatControl chatControl;

                        if (!_chatControls.TryGetValue(sectionTreeViewItem, out chatControl))
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                lock (_chatControls.ThisLock)
                                {
                                    if (!_chatControls.TryGetValue(sectionTreeViewItem, out chatControl))
                                    {
                                        chatControl = new ChatControl(sectionTreeViewItem, sectionTreeViewItem.Value.ChatCategorizeTreeItem, _lairManager, _bufferManager);
                                        _chatControls[sectionTreeViewItem] = chatControl;
                                        _chatGrid.Children.Add(chatControl);

                                        chatControl.Visibility = System.Windows.Visibility.Hidden;
                                    }
                                }
                            }));
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            foreach (var item in _chatGrid.Children.OfType<ChatControl>())
                            {
                                if (item == chatControl)
                                {
                                    item.Visibility = System.Windows.Visibility.Visible;
                                }
                                else
                                {
                                    item.Visibility = System.Windows.Visibility.Hidden;
                                }
                            }

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
                Stopwatch refreshStopwatch = new Stopwatch();

                for (; ; )
                {
                    Thread.Sleep(1000);

                    if (!refreshStopwatch.IsRunning || refreshStopwatch.Elapsed.TotalMinutes >= 1)
                    {
                        refreshStopwatch.Restart();

                        var sectionTreeViewItems = new HashSet<SectionTreeViewItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            var sectionCategorizeTreeViewItems = new List<SectionCategorizeTreeViewItem>();
                            sectionCategorizeTreeViewItems.Add(_treeViewItem);

                            for (int i = 0; i < sectionCategorizeTreeViewItems.Count; i++)
                            {
                                sectionCategorizeTreeViewItems.AddRange(sectionCategorizeTreeViewItems[i].Items.OfType<SectionCategorizeTreeViewItem>());
                                sectionTreeViewItems.UnionWith(sectionCategorizeTreeViewItems[i].Items.OfType<SectionTreeViewItem>());
                            }
                        }));

                        foreach (var sectionTreeItem in sectionTreeViewItems.Select(n => n.Value))
                        {
                            var sectionProfiles = new List<SectionProfile>();

                            {
                                var leaderSectionProfile = _lairManager.GetSectionProfile(sectionTreeItem.Tag, sectionTreeItem.LeaderSignature);
                                if (leaderSectionProfile == null) continue;

                                sectionProfiles.Add(leaderSectionProfile);

                                var hashset = new HashSet<string>();

                                hashset.Add(sectionTreeItem.LeaderSignature);

                                for (int i = 0; i < sectionProfiles.Count; i++)
                                {
                                    foreach (var trustSignature in sectionProfiles[i].TrustSignatures)
                                    {
                                        if (hashset.Contains(trustSignature)) continue;
                                        hashset.Add(trustSignature);

                                        var item = _lairManager.GetSectionProfile(sectionTreeItem.Tag, trustSignature);

                                        if (item != null)
                                        {
                                            sectionProfiles.Add(item);
                                        }
                                        else
                                        {
                                            var cacheItem = sectionTreeItem.CacheSectionProfiles
                                                .FirstOrDefault(n => n.Signature == trustSignature);

                                            if (cacheItem != null)
                                            {
                                                sectionProfiles.Add(cacheItem);
                                            }
                                        }
                                    }
                                }
                            }

                            lock (sectionTreeItem.ThisLock)
                            {
                                lock (sectionTreeItem.CacheSectionProfiles.ThisLock)
                                {
                                    sectionTreeItem.CacheSectionProfiles.Clear();
                                    sectionTreeItem.CacheSectionProfiles.AddRange(sectionProfiles);
                                }
                            }
                        }

                        // _chatControlsの同期
                        {
                            foreach (var sectionTreeViewItem in sectionTreeViewItems)
                            {
                                if (_chatControls.ContainsKey(sectionTreeViewItem)) continue;

                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                                {
                                    lock (_chatControls.ThisLock)
                                    {
                                        if (!_chatControls.ContainsKey(sectionTreeViewItem))
                                        {
                                            var chatControl = new ChatControl(sectionTreeViewItem, sectionTreeViewItem.Value.ChatCategorizeTreeItem, _lairManager, _bufferManager);
                                            _chatControls[sectionTreeViewItem] = chatControl;
                                            _chatGrid.Children.Add(chatControl);

                                            chatControl.Visibility = System.Windows.Visibility.Hidden;
                                        }
                                    }
                                }));
                            }

                            lock (_chatControls.ThisLock)
                            {
                                foreach (var key in _chatControls.Keys.ToArray())
                                {
                                    if (sectionTreeViewItems.Contains(key)) continue;

                                    _chatControls.Remove(key);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Update()
        {
            Settings.Instance.SectionControl_SectionCategorizeTreeItem = _treeViewItem.Value;

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
                    var selectTreeViewItem = (SectionCategorizeTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectTreeViewItem.Value.Name);
                }
                else if (_treeView.SelectedItem is SectionTreeViewItem)
                {
                    var selectTreeViewItem = (SectionTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToSectionString(selectTreeViewItem.Value.Tag));
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
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem sectionCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_sectionCategorizeTreeViewItemDeleteMenuItem");
            MenuItem sectionCategorizeTreeViewItemCutMenuItem = contextMenu.GetMenuItem("_sectionCategorizeTreeViewItemCutMenuItem");
            MenuItem sectionCategorizeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_sectionCategorizeTreeViewItemPasteMenuItem");

            sectionCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            sectionCategorizeTreeViewItemCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            sectionCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsSectionCategorizeTreeItems() || Clipboard.ContainsSectionTreeItems() || Clipboard.ContainsSections();
        }

        private void _sectionCategorizeTreeViewItemNewSectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (Settings.Instance.Global_DigitalSignatureCollection.Count == 0)
            {
                ViewOptionsWindow window = new ViewOptionsWindow(_bufferManager);
                window.Owner = _mainWindow;
                window.SelectTab("Signature");
                window.ShowDialog();
            }

            if (Settings.Instance.Global_DigitalSignatureCollection.Count > 0)
            {
                NewSectionWindow newSectionwindow = new NewSectionWindow();
                newSectionwindow.Owner = _mainWindow;

                if (newSectionwindow.ShowDialog() == true)
                {
                    byte[] id = new byte[64];
                    (new System.Security.Cryptography.RNGCryptoServiceProvider()).GetBytes(id);

                    var tag = new Section(id, newSectionwindow.SectionName);
                    var sectionTreeItem = new SectionTreeItem(tag);
                    sectionTreeItem.LeaderSignature = newSectionwindow.LeaderSignature;

                    SectionTreeItemEditWindow sectionTreeItemEditWindow = new SectionTreeItemEditWindow(sectionTreeItem, _lairManager, _bufferManager);
                    sectionTreeItemEditWindow.Owner = _mainWindow;

                    if (sectionTreeItemEditWindow.ShowDialog() == true)
                    {
                        selectTreeViewItem.Value.SectionTreeItems.Add(sectionTreeItem);

                        selectTreeViewItem.Update();
                    }
                }
            }

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow();
            window.Title = LanguagesManager.Instance.NameWindow_Title_Category;
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(new SectionCategorizeTreeItem() { Name = window.Text });

                selectTreeViewItem.Update();
            }

            this.Update();
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
                sb.AppendLine(LairConverter.ToSectionString(item.Value.Tag, item.Value.LeaderSignature));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Tag, item.Value.LeaderSignature));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
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
                if (selectTreeViewItem.Value.SectionTreeItems.Any(n => n.Tag == item.Tag)) continue;

                selectTreeViewItem.Value.SectionTreeItems.Add(item);
            }

            foreach (var item in Clipboard.GetSections())
            {
                if (selectTreeViewItem.Value.SectionTreeItems.Any(n => n.Tag == item.Item1)) continue;

                var sectionTreeItem = new SectionTreeItem(item.Item1);
                sectionTreeItem.LeaderSignature = item.Item2;

                selectTreeViewItem.Value.SectionTreeItems.Add(sectionTreeItem);
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _sectionTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            SectionTreeItemEditWindow sectionTreeItemEditWindow = new SectionTreeItemEditWindow(selectTreeViewItem.Value, _lairManager, _bufferManager);
            sectionTreeItemEditWindow.Owner = _mainWindow;

            if (sectionTreeItemEditWindow.ShowDialog() == true)
            {
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _sectionTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
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

        private void _sectionTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
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

        private void _sectionTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionTreeItems(new SectionTreeItem[] { selectTreeViewItem.Value });
        }

        private void _sectionTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(LairConverter.ToSectionString(selectTreeViewItem.Value.Tag, selectTreeViewItem.Value.LeaderSignature));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Tag, selectTreeViewItem.Value.LeaderSignature));

            Clipboard.SetText(sb.ToString());
        }

        private void _sectionTreeViewItemTrustSignaturesPreviewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            SignatureTreeItem signatureTreeItem = SectionControl.GetSignatureTreeViewItem(selectTreeViewItem.Value);

            TrustSignaturesPreviewWindow window = new TrustSignaturesPreviewWindow(signatureTreeItem);
            window.Owner = _mainWindow;
            window.ShowDialog();
        }

        static SignatureTreeItem GetSignatureTreeViewItem(SectionTreeItem sectionTreeItem)
        {
            Dictionary<string, SectionProfile> dic = new Dictionary<string, SectionProfile>();

            foreach (var item in sectionTreeItem.CacheSectionProfiles)
            {
                dic[item.Signature] = item;
            }

            List<SignatureTreeItem> signatureTreeItems = new List<SignatureTreeItem>();
            HashSet<string> checkedSignatures = new HashSet<string>();

            {
                SectionProfile leaderSectionProfile;
                if (!dic.TryGetValue(sectionTreeItem.LeaderSignature, out leaderSectionProfile)) return null;

                signatureTreeItems.Add(new SignatureTreeItem(leaderSectionProfile));
                checkedSignatures.Add(sectionTreeItem.LeaderSignature);
            }

            for (int i = 0; i < signatureTreeItems.Count; i++)
            {
                var sortList = signatureTreeItems[i].SectionProfile.TrustSignatures.ToList();
                sortList.Sort((x, y) => x.CompareTo(y));

                foreach (var trustSignature in sortList)
                {
                    if (checkedSignatures.Contains(trustSignature)) continue;

                    SectionProfile sectionProfile;
                    if (!dic.TryGetValue(trustSignature, out sectionProfile)) continue;

                    var tempItem = new SignatureTreeItem(sectionProfile);
                    signatureTreeItems.Add(tempItem);
                    signatureTreeItems[i].Children.Add(tempItem);

                    checkedSignatures.Add(trustSignature);
                }
            }

            return signatureTreeItems[0];
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
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
                _sectionTreeViewItemDeleteMenuItem_Click(null, null);
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
                _sectionTreeViewItemCopyMenuItem_Click(null, null);
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
                _sectionTreeViewItemCutMenuItem_Click(null, null);
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
