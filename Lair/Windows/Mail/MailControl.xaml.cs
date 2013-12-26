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
using A = Library.Net.Amoeba;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for MailControl.xaml
    /// </summary>
    partial class MailControl : UserControl, IDisposable
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private SectionTreeViewItem _sectionTreeViewItem;
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private static Random _random = new Random();

        private Thread _searchThread;
        private Thread _cacheThread;

        private volatile bool _refresh;
        private volatile bool _cacheUpdate;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private MailCategorizeTreeViewItem _treeViewItem;
        private ObservableCollection<SectionMessageWrapper> _listViewItemCollection = new ObservableCollection<SectionMessageWrapper>();

        private volatile bool _disposed;

        public MailControl(SectionTreeViewItem sectionTreeViewItem, MailCategorizeTreeItem mailCategorizeTreeItem, LairManager lairManager, BufferManager bufferManager)
        {
            _sectionTreeViewItem = sectionTreeViewItem;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            _treeViewItem = new MailCategorizeTreeViewItem(mailCategorizeTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _listView.ItemsSource = _listViewItemCollection;

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "MailControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "MailControl_CacheThread";
            _cacheThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            _messageUploadButton.IsEnabled = false;

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
        }

        private void Search()
        {
            try
            {
                while (!_disposed)
                {
                    Thread.Sleep(100);
                    if (!_refresh) continue;

                    TreeViewItem tempTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        tempTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (tempTreeViewItem is MailCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _messageUploadButton.IsEnabled = false;

                            _listViewItemCollection.Clear();
                        }));
                    }
                    else if (tempTreeViewItem is MailTreeViewItem)
                    {
                        MailTreeViewItem mailTreeViewItem = (MailTreeViewItem)tempTreeViewItem;

                        var trustSignatures = new HashSet<string>();
                        trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
                        trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

                        var newList = new HashSet<SectionMessageWrapper>();

                        lock (mailTreeViewItem.Value.ThisLock)
                        {
                            newList.UnionWith(mailTreeViewItem.Value.SentSectionMessages
                                .Select(n => new SectionMessageWrapper() { Value = n, IsTrust = trustSignatures.Contains(n.Signature) }));

                            newList.UnionWith(mailTreeViewItem.Value.ReadSectionMessages
                                .Where(n => !mailTreeViewItem.Value.SentSectionMessages.Contains(n))
                                .Select(n => new SectionMessageWrapper() { Value = n, IsTrust = trustSignatures.Contains(n.Signature) }));

                            newList.UnionWith(mailTreeViewItem.Value.UnreadSectionMessages
                                .Where(n => !mailTreeViewItem.Value.SentSectionMessages.Contains(n))
                                .Select(n => new SectionMessageWrapper() { IsNew = true, Value = n, IsTrust = trustSignatures.Contains(n.Signature) }));

                            mailTreeViewItem.Value.ReadSectionMessages.AddRange(mailTreeViewItem.Value.UnreadSectionMessages);
                            mailTreeViewItem.Value.UnreadSectionMessages.Clear();
                        }

                        {
                            string searchText = null;

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                searchText = _searchTextBox.Text;
                            }));

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                var list = new List<SectionMessageWrapper>();

                                foreach (var item in newList)
                                {
                                    var text = RichTextBoxHelper.MessageToString(item.Value.CreationTime, item.Value.Signature, item.Value.Comment).ToLower();
                                    if (!words.All(n => text.Contains(n))) continue;

                                    list.Add(item);
                                }

                                newList.Clear();
                                newList.UnionWith(list);
                            }
                        }

                        var oldList = new HashSet<SectionMessageWrapper>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listViewItemCollection.ToArray());
                        }));

                        var removeList = new List<SectionMessageWrapper>();
                        var addList = new List<SectionMessageWrapper>();

                        foreach (var item in oldList)
                        {
                            if (!newList.Contains(item)) removeList.Add(item);
                        }

                        foreach (var item in newList)
                        {
                            if (!oldList.Contains(item)) addList.Add(item);
                        }

                        int layoutUpdated = 0;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (mailTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            {
                                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
                                var sectionProfile = _sectionTreeViewItem.Value.CacheSectionProfiles.FirstOrDefault(n => n.Signature == mailTreeViewItem.Value.TargetSignature);

                                _messageUploadButton.IsEnabled = (digitalSignature != null && sectionProfile != null && trustSignatures.Contains(digitalSignature.ToString()));
                            }

                            if (removeList.Count > 100)
                            {
                                _listViewItemCollection.Clear();

                                foreach (var item in newList)
                                {
                                    _listViewItemCollection.Add(item);
                                }
                            }
                            else
                            {
                                foreach (var item in addList)
                                {
                                    _listViewItemCollection.Add(item);
                                }

                                foreach (var item in removeList)
                                {
                                    _listViewItemCollection.Remove(item);
                                }
                            }

                            this.Sort();

                            var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                            view.Refresh();

                            if (_listViewItemCollection.Count > 0)
                            {
                                _listView.GoBottom();

                                _listView.UpdateLayout();
                                var topItem = _listViewItemCollection.Where(n => n.IsNew).FirstOrDefault();
                                if (topItem == null) topItem = _listViewItemCollection.LastOrDefault();
                                if (topItem != null) _listView.ScrollIntoView(topItem);
                            }

                            layoutUpdated = _layoutUpdated;

                            this.Update_TreeView_Color();
                        }));

                        while (_layoutUpdated == layoutUpdated) Thread.Sleep(100);

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            _listView.UpdateLayout();
                            var topItem = _listViewItemCollection.Where(n => n.IsNew).FirstOrDefault();
                            if (topItem == null) topItem = _listViewItemCollection.LastOrDefault();
                            if (topItem != null) _listView.ScrollIntoView(topItem);
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Cache()
        {
            try
            {
                while (!_disposed)
                {
                    var mailTreeViewItems = new List<MailTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var categorizeMailTreeViewItems = new List<MailCategorizeTreeViewItem>();
                        categorizeMailTreeViewItems.Add(_treeViewItem);

                        for (int i = 0; i < categorizeMailTreeViewItems.Count; i++)
                        {
                            categorizeMailTreeViewItems.AddRange(categorizeMailTreeViewItems[i].Items.OfType<MailCategorizeTreeViewItem>());
                            mailTreeViewItems.AddRange(categorizeMailTreeViewItems[i].Items.OfType<MailTreeViewItem>());
                        }
                    }));

                    foreach (var treeViewItem in mailTreeViewItems)
                    {
                        bool isUpdate = false;

                        // SectionMessage
                        {
                            var oldList = new HashSet<SectionMessage>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                lock (treeViewItem.Value.ThisLock)
                                {
                                    oldList.UnionWith(treeViewItem.Value.ReadSectionMessages);
                                    oldList.UnionWith(treeViewItem.Value.UnreadSectionMessages);
                                }
                            }

                            var newList = new HashSet<SectionMessage>();

                            {
                                newList.UnionWith(_lairManager.GetSectionMessages(_sectionTreeViewItem.Value.Tag, new string[] { treeViewItem.Value.TargetSignature }, _sectionTreeViewItem.Value.Exchange.GetPrivateKey()));

                                foreach (var item in Collection.Merge(treeViewItem.Value.ReadSectionMessages, treeViewItem.Value.UnreadSectionMessages))
                                {
                                    newList.Add(item);
                                }
                            }

                            lock (treeViewItem.Value.ThisLock)
                            {
                                foreach (var item in oldList)
                                {
                                    if (!newList.Contains(item))
                                    {
                                        treeViewItem.Value.ReadSectionMessages.Remove(item);
                                        treeViewItem.Value.UnreadSectionMessages.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    if (!oldList.Contains(item))
                                    {
                                        treeViewItem.Value.UnreadSectionMessages.Add(item);
                                        isUpdate |= true;
                                    }
                                }
                            }
                        }

                        if (isUpdate)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                treeViewItem.Update();
                            }));
                        }
                    }

                    {
                        var trustSignatures = new HashSet<string>();
                        trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
                        trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

                        HashSet<string> signatures = new HashSet<string>(_lairManager.GetSectionMessages(_sectionTreeViewItem.Value.Tag, trustSignatures, _sectionTreeViewItem.Value.Exchange.GetPrivateKey()).Select(n => n.Signature));
                        signatures.ExceptWith(mailTreeViewItems.Select(n => n.Value.TargetSignature));
                        signatures.Remove(_sectionTreeViewItem.Value.UploadSignature);

                        if (signatures.Count > 0)
                        {
                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                            {
                                foreach (var signature in signatures)
                                {
                                    var mailTreeItem = new MailTreeItem();
                                    mailTreeItem.TargetSignature = signature;

                                    _treeViewItem.Value.MailTreeItems.Add(mailTreeItem);
                                }

                                _treeViewItem.Update();
                            }));
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (_cacheUpdate)
                        {
                            _cacheUpdate = false;
                            this.Update();
                        }
                        else
                        {
                            this.Update_TreeView_Color();
                        }
                    }));

                    _autoResetEvent.WaitOne(1000 * 30);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Update()
        {
            this.Update_TreeView_Color();

            _refresh = true;
        }

        private void Update_Cache()
        {
            _cacheUpdate = true;
            _autoResetEvent.Set();
        }

        private void Update_TreeView_Color()
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;

            {
                var items = new List<TreeViewItem>();
                items.Add(_treeViewItem);

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (TreeViewItem item in items[i].Items)
                    {
                        items.Add(item);
                    }
                }

                var hitItems = new HashSet<TreeViewItem>();

                foreach (var item in items.OfType<MailTreeViewItem>()
                    .Where(n => n.Value.UnreadSectionMessages.Count > 0))
                {
                    hitItems.UnionWith(_treeView.GetAncestors(item));
                }

                foreach (var item in items)
                {
                    var textBlock = (TextBlock)item.Header;

                    if (hitItems.Contains(item))
                    {
                        textBlock.FontWeight = FontWeights.ExtraBlack;

                        if (selectTreeViewItem != item)
                        {
                            textBlock.Foreground = new SolidColorBrush(App.LairColors.Tree_Hit);
                        }
                        else
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                        }
                    }
                    else
                    {
                        textBlock.FontWeight = FontWeights.Normal;

                        if (selectTreeViewItem != item)
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
                        }
                        else
                        {
                            textBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                        }
                    }
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

                if (sourceItem is MailCategorizeTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is MailCategorizeTreeViewItem)
                    {
                        var s = (MailCategorizeTreeViewItem)sourceItem;
                        var d = (MailCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is MailCategorizeTreeViewItem)
                        {
                            var p = (MailCategorizeTreeViewItem)parentItem;

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
                else if (sourceItem is MailTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is MailCategorizeTreeViewItem)
                    {
                        var s = (MailTreeViewItem)sourceItem;
                        var d = (MailCategorizeTreeViewItem)destinationItem;

                        if (d.Value.MailTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is MailCategorizeTreeViewItem)
                        {
                            var p = (MailCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.MailTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.MailTreeItems.Clear();
                            p.Value.MailTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.MailTreeItems.Add(s.Value);
                        d.Update();
                    }
                }

                this.Update_Cache();
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

        private void _mailCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem mailCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_mailCategorizeTreeViewItemDeleteMenuItem");
            MenuItem mailCategorizeTreeViewItemCutMenuItem = contextMenu.GetMenuItem("_mailCategorizeTreeViewItemCutMenuItem");
            MenuItem mailCategorizeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_mailCategorizeTreeViewItemPasteMenuItem");

            mailCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            mailCategorizeTreeViewItemCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            mailCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsMailCategorizeTreeItems() || Clipboard.ContainsMailTreeItems();
        }

        private void _mailCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow();
            window.Title = LanguagesManager.Instance.NameWindow_Title_Category;
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(new MailCategorizeTreeItem() { Name = window.Text });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _mailCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Mail", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (MailCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _mailCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Mail", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (MailCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _mailCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetMailCategorizeTreeItems(new MailCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parent = (MailCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _mailCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetMailCategorizeTreeItems(new MailCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _mailCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var mailTreeViewItems = new List<MailTreeViewItem>();

            {
                var categorizeMailTreeViewItems = new List<MailCategorizeTreeViewItem>();
                categorizeMailTreeViewItems.Add(_treeViewItem);

                for (int i = 0; i < categorizeMailTreeViewItems.Count; i++)
                {
                    categorizeMailTreeViewItems.AddRange(categorizeMailTreeViewItems[i].Items.OfType<MailCategorizeTreeViewItem>());
                    mailTreeViewItems.AddRange(categorizeMailTreeViewItems[i].Items.OfType<MailTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in mailTreeViewItems)
            {
                sb.AppendLine(item.Value.TargetSignature);
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _mailCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetMailCategorizeTreeItems())
            {
                selectTreeViewItem.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetMailTreeItems())
            {
                if (selectTreeViewItem.Value.MailTreeItems.Any(n => n.TargetSignature == item.TargetSignature)) continue;

                selectTreeViewItem.Value.MailTreeItems.Add(item);
            }

            var trustSignatures = new HashSet<string>(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));
            trustSignatures.Remove(_sectionTreeViewItem.Value.UploadSignature);

            foreach (var signature in Clipboard.GetText().Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Signature.HasSignature(signature)) continue;
                if (!trustSignatures.Contains(signature)) continue;
                if (selectTreeViewItem.Value.MailTreeItems.Any(n => n.TargetSignature == signature)) continue;

                var mailTreeItem = new MailTreeItem();
                mailTreeItem.TargetSignature = signature;

                selectTreeViewItem.Value.MailTreeItems.Add(mailTreeItem);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _mailCategorizeTreeViewItemSignatureListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var trustSignatures = new HashSet<string>(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));
            trustSignatures.Remove(_sectionTreeViewItem.Value.UploadSignature);

            {
                var mailCategorizeTreeItems = new List<MailCategorizeTreeItem>();
                mailCategorizeTreeItems.Add(_treeViewItem.Value);

                for (int i = 0; i < mailCategorizeTreeItems.Count; i++)
                {
                    mailCategorizeTreeItems.AddRange(mailCategorizeTreeItems[i].Children);
                    trustSignatures.ExceptWith(mailCategorizeTreeItems[i].MailTreeItems.Select(n => n.TargetSignature));
                }
            }

            SignatureListWindow window = new SignatureListWindow(trustSignatures, _lairManager);
            window.Owner = _mainWindow;

            window.SignatureAddEvent += (object sender2, string targetSignature) =>
            {
                var mailTreeItem = new MailTreeItem();
                mailTreeItem.TargetSignature = targetSignature;

                selectTreeViewItem.Value.MailTreeItems.Add(mailTreeItem);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.ShowDialog();
        }

        private void _mailTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _mailTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Mail", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<MailCategorizeTreeViewItem>().LastOrDefault() as MailCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.MailTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _mailTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetMailTreeItems(new MailTreeItem[] { selectTreeViewItem.Value });

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<MailCategorizeTreeViewItem>().LastOrDefault() as MailCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.MailTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _mailTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetMailTreeItems(new MailTreeItem[] { selectTreeViewItem.Value });
        }

        private void _mailTreeItemTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(selectTreeViewItem.Value.TargetSignature);

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _listView

        private volatile int _layoutUpdated;

        private void _listView_LayoutUpdated(object sender, EventArgs e)
        {
            _layoutUpdated++;
        }

        private void _listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
            var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

            _gridViewColumn.Width = Math.Max(0, _listView.ActualWidth - 21);

            _listView.Items.Refresh();
        }

        private void _listView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Home)
            {
                _listView.GoTop();
            }
            else if (e.Key == System.Windows.Input.Key.End)
            {
                _listView.GoBottom();
            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                _listView.PageUp();
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                _listView.PageDown();
            }
        }

        private void _listView_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed
                || e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                _listView.SelectedIndex = index;
            }
        }

        #endregion

        #region Sort

        private void Sort()
        {
            var list = _listViewItemCollection.ToList();

            list.Sort((x, y) =>
            {
                var vx = x.Value;
                var vy = y.Value;

                int c = vx.CreationTime.CompareTo(vy.CreationTime);
                if (c != 0) return c;
                c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                if (c != 0) return c;

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }
        }

        #endregion

        #region _richTextBox

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            var selectItem = _listView.SelectedItem as SectionMessageWrapper;
            if (selectItem == null) return;

            var list = new List<MenuItem>();

            list.AddRange(richTextBox.ContextMenu.Items.OfType<MenuItem>());

            for (int i = 0; i < list.Count; i++)
            {
                list.AddRange(list[i].Items.OfType<MenuItem>());
            }

            foreach (var item in list)
            {
                if (item.Name == "_richTextBoxCopyMenuItem")
                {
                    item.IsEnabled = !richTextBox.Selection.IsEmpty;
                }
                else if (item.Name == "_richTextBoxResponsMenuItem")
                {
                    var trustSignatures = new HashSet<string>();
                    trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
                    trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

                    var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
                    var sectionProfile = _sectionTreeViewItem.Value.CacheSectionProfiles.FirstOrDefault(n => n.Signature == selectTreeViewItem.Value.TargetSignature);

                    item.IsEnabled = (digitalSignature != null && sectionProfile != null && trustSignatures.Contains(digitalSignature.ToString()));
                }
            }
        }

        private void _richTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            Clipboard.SetText(richTextBox.Selection.Text);
        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            var sectionProfile = _sectionTreeViewItem.Value.CacheSectionProfiles.FirstOrDefault(n => n.Signature == selectTreeViewItem.Value.TargetSignature);
            if (sectionProfile == null || sectionProfile.ExchangePublicKey == null) return;

            var responsMessage = _listView.SelectedItem as SectionMessageWrapper;

            SectionMessageEditWindow window = new SectionMessageEditWindow(_sectionTreeViewItem.Value.Tag, "", responsMessage.Value, sectionProfile.ExchangePublicKey, digitalSignature, _lairManager);
            window.Owner = _mainWindow;

            window.Closed += (object sender2, EventArgs e2) =>
            {
                if (window.SectionMessage == null) return;
                if (selectTreeViewItem.Value.SentSectionMessages.Contains(window.SectionMessage)) return;

                selectTreeViewItem.Value.SentSectionMessages.Add(window.SectionMessage);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.Show();
        }

        private void _richTextBoxTrustMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _listView.SelectedItems.OfType<SectionMessageWrapper>())
            {
                if (_sectionTreeViewItem.Value.TrustSignatures.Contains(item.Value.Signature)) continue;
                _sectionTreeViewItem.Value.TrustSignatures.Add(item.Value.Signature);
            }
        }

        #endregion

        #region Tool

        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as MailTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            var sectionProfile = _sectionTreeViewItem.Value.CacheSectionProfiles.FirstOrDefault(n => n.Signature == selectTreeViewItem.Value.TargetSignature);
            if (sectionProfile == null || sectionProfile.ExchangePublicKey == null) return;

            SectionMessageEditWindow window = new SectionMessageEditWindow(_sectionTreeViewItem.Value.Tag, "", null, sectionProfile.ExchangePublicKey, digitalSignature, _lairManager);
            window.Owner = _mainWindow;

            window.Closed += (object sender2, EventArgs e2) =>
            {
                if (window.SectionMessage == null) return;
                if (selectTreeViewItem.Value.SentSectionMessages.Contains(window.SectionMessage)) return;

                selectTreeViewItem.Value.SentSectionMessages.Add(window.SectionMessage);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.Show();
        }

        #endregion

        #region Search

        private void _searchCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";

            this.Update();
        }

        private void _searchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is MailCategorizeTreeViewItem)
            {
                _mailCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is MailTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is MailCategorizeTreeViewItem)
            {
                _mailCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is MailTreeViewItem)
            {
                _mailTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is MailCategorizeTreeViewItem)
            {
                _mailCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is MailTreeViewItem)
            {
                _mailTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is MailCategorizeTreeViewItem)
            {
                _mailCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is MailTreeViewItem)
            {
                _mailTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is MailCategorizeTreeViewItem)
            {
                _mailCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is MailTreeViewItem)
            {

            }
        }

        private void Execute_Search(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }

        ~MailControl()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;

            if (disposing)
            {

            }
        }

        #region IDisposable

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    class SectionMessageWrapper : IEquatable<SectionMessageWrapper>
    {
        public bool IsNew { get; set; }
        public bool IsTrust { get; set; }
        public SectionMessage Value { get; set; }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SectionMessageWrapper)) return false;

            return this.Equals((SectionMessageWrapper)obj);
        }

        public bool Equals(SectionMessageWrapper other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.IsNew != other.IsNew
                || this.IsTrust != other.IsTrust
                || this.Value != other.Value)
            {
                return false;
            }

            return true;
        }
    }
}
