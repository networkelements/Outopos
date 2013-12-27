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
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    partial class ChatControl : UserControl, IDisposable
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

        private ChatCategorizeTreeViewItem _treeViewItem;
        private ObservableCollection<ChatMessageWrapper> _listViewItemCollection = new ObservableCollection<ChatMessageWrapper>();

        private volatile bool _disposed;

        public ChatControl(SectionTreeViewItem sectionTreeViewItem, ChatCategorizeTreeItem chatCategorizeTreeItem, LairManager lairManager, BufferManager bufferManager)
        {
            _sectionTreeViewItem = sectionTreeViewItem;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            _treeViewItem = new ChatCategorizeTreeViewItem(chatCategorizeTreeItem);

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
            _searchThread.Name = "ChatControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "ChatControl_CacheThread";
            _cacheThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            _trustToggleButton.IsEnabled = false;
            _topicUploadButton.IsEnabled = false;
            _messageUploadButton.IsEnabled = false;

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);

            RichTextBoxHelper.ChatClickEvent += this.ChatClickEvent;
        }

        private void ChatClickEvent(object sender, Chat chat)
        {
            if (chat.Id == null || chat.Name == null) return;
            if (this.Visibility != System.Windows.Visibility.Visible) return;

            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var parentTreeViewItem = selectTreeViewItem.Parent as ChatCategorizeTreeViewItem;
            if (parentTreeViewItem == null) return;

            if (parentTreeViewItem.Value.ChatTreeItems.Any(n => n.Tag == chat)) return;

            var chatTreeItem = new ChatTreeItem(chat);
            parentTreeViewItem.Value.ChatTreeItems.Add(chatTreeItem);

            parentTreeViewItem.Update();
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

                    if (tempTreeViewItem is ChatCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _trustToggleButton.IsEnabled = false;
                            _topicUploadButton.IsEnabled = false;
                            _messageUploadButton.IsEnabled = false;

                            _listViewItemCollection.Clear();
                        }));
                    }
                    else if (tempTreeViewItem is ChatTreeViewItem)
                    {
                        ChatTreeViewItem chatTreeViewItem = (ChatTreeViewItem)tempTreeViewItem;

                        var trustSignatures = new HashSet<string>();
                        trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
                        trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

                        var newList = new HashSet<ChatMessageWrapper>();

                        lock (chatTreeViewItem.Value.ThisLock)
                        {
                            newList.UnionWith(chatTreeViewItem.Value.ReadChatMessages
                                .Select(n => new ChatMessageWrapper() { Value = n, IsTrust = trustSignatures.Contains(n.Signature) }));

                            newList.UnionWith(chatTreeViewItem.Value.UnreadChatMessages
                                .Select(n => new ChatMessageWrapper() { IsNew = true, Value = n, IsTrust = trustSignatures.Contains(n.Signature) }));

                            chatTreeViewItem.Value.ReadChatMessages.AddRange(chatTreeViewItem.Value.UnreadChatMessages);
                            chatTreeViewItem.Value.UnreadChatMessages.Clear();
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
                                var list = new List<ChatMessageWrapper>();

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

                        var oldList = new HashSet<ChatMessageWrapper>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listViewItemCollection.ToArray());
                        }));

                        var removeList = new List<ChatMessageWrapper>();
                        var addList = new List<ChatMessageWrapper>();

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
                            if (chatTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            {
                                _trustToggleButton.IsEnabled = true;
                                _trustToggleButton.IsChecked = chatTreeViewItem.Value.IsTrustEnabled;

                                _topicUploadButton.IsEnabled = trustSignatures.Contains(_sectionTreeViewItem.Value.UploadSignature);

                                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);

                                _messageUploadButton.IsEnabled = (digitalSignature != null);
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
                    var chatTreeViewItems = new List<ChatTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var categorizeChatTreeViewItems = new List<ChatCategorizeTreeViewItem>();
                        categorizeChatTreeViewItems.Add(_treeViewItem);

                        for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
                        {
                            categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                            chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
                        }
                    }));

                    foreach (var treeViewItem in chatTreeViewItems)
                    {
                        bool isUpdate = false;

                        // ChatTopic
                        {
                            var chatTopicList = _lairManager.GetChatTopic(treeViewItem.Value.Tag, _sectionTreeViewItem.Value.CacheSectionProfiles.Select(n => n.Signature)).ToList();
                            chatTopicList.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));
                            var tempChatTopic = chatTopicList.FirstOrDefault();

                            if (tempChatTopic != null)
                            {
                                treeViewItem.Value.ChatTopic = tempChatTopic;
                                treeViewItem.Value.IsNewTopic = true;
                                isUpdate |= true;
                            }
                        }

                        // ChatMessage
                        {
                            var oldList = new HashSet<ChatMessage>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                lock (treeViewItem.Value.ThisLock)
                                {
                                    oldList.UnionWith(treeViewItem.Value.ReadChatMessages);
                                    oldList.UnionWith(treeViewItem.Value.UnreadChatMessages);
                                }
                            }

                            var newList = new HashSet<ChatMessage>();

                            if (treeViewItem.Value.IsTrustEnabled)
                            {
                                var trustSignatures = new HashSet<string>();
                                trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
                                trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

                                newList.UnionWith(_lairManager.GetChatMessage(treeViewItem.Value.Tag, trustSignatures));

                                foreach (var item in Collection.Merge(treeViewItem.Value.ReadChatMessages, treeViewItem.Value.UnreadChatMessages))
                                {
                                    if (!trustSignatures.Contains(item.Signature)) continue;
                                    newList.Add(item);
                                }
                            }
                            else
                            {
                                newList.UnionWith(_lairManager.GetChatMessage(treeViewItem.Value.Tag));

                                foreach (var item in Collection.Merge(treeViewItem.Value.ReadChatMessages, treeViewItem.Value.UnreadChatMessages))
                                {
                                    newList.Add(item);
                                }
                            }

                            {
                                var sortList = newList.ToList();

                                sortList.Sort((x, y) =>
                                {
                                    int c = x.CreationTime.CompareTo(y.CreationTime);
                                    if (c != 0) return c;
                                    c = x.Signature.CompareTo(y.Signature);
                                    if (c != 0) return c;

                                    return x.GetHashCode().CompareTo(y.GetHashCode());
                                });

                                var tempList = sortList.Skip(sortList.Count - 1024).ToList();
                                tempList.Reverse();

                                sortList = tempList.ToList();

                                newList.Clear();
                                newList.UnionWith(sortList);
                            }

                            lock (treeViewItem.Value.ThisLock)
                            {
                                foreach (var item in oldList)
                                {
                                    if (!newList.Contains(item))
                                    {
                                        treeViewItem.Value.ReadChatMessages.Remove(item);
                                        treeViewItem.Value.UnreadChatMessages.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    if (!oldList.Contains(item))
                                    {
                                        treeViewItem.Value.UnreadChatMessages.Add(item);
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

                foreach (var item in items.OfType<ChatTreeViewItem>()
                    .Where(n => n.Value.IsNewTopic || n.Value.UnreadChatMessages.Count > 0))
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

                if (sourceItem is ChatCategorizeTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is ChatCategorizeTreeViewItem)
                    {
                        var s = (ChatCategorizeTreeViewItem)sourceItem;
                        var d = (ChatCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is ChatCategorizeTreeViewItem)
                        {
                            var p = (ChatCategorizeTreeViewItem)parentItem;

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
                else if (sourceItem is ChatTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is ChatCategorizeTreeViewItem)
                    {
                        var s = (ChatTreeViewItem)sourceItem;
                        var d = (ChatCategorizeTreeViewItem)destinationItem;

                        if (d.Value.ChatTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is ChatCategorizeTreeViewItem)
                        {
                            var p = (ChatCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.ChatTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.ChatTreeItems.Clear();
                            p.Value.ChatTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.ChatTreeItems.Add(s.Value);
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

        private void _chatCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            _startPoint = new Point(-1, -1);

            MenuItem chatCategorizeTreeViewItemDeleteMenuItem = contextMenu.GetMenuItem("_chatCategorizeTreeViewItemDeleteMenuItem");
            MenuItem chatCategorizeTreeViewItemCutMenuItem = contextMenu.GetMenuItem("_chatCategorizeTreeViewItemCutMenuItem");
            MenuItem chatCategorizeTreeViewItemPasteMenuItem = contextMenu.GetMenuItem("_chatCategorizeTreeViewItemPasteMenuItem");

            chatCategorizeTreeViewItemDeleteMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            chatCategorizeTreeViewItemCutMenuItem.IsEnabled = (selectTreeViewItem != _treeViewItem);
            chatCategorizeTreeViewItemPasteMenuItem.IsEnabled = Clipboard.ContainsChatCategorizeTreeItems() || Clipboard.ContainsChatTreeItems() || Clipboard.ContainsChats();
        }

        private void _chatCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow();
            window.Title = LanguagesManager.Instance.NameWindow_Title_Category;
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(new ChatCategorizeTreeItem() { Name = window.Text });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _chatCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Chat", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (ChatCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _chatCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Chat", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (ChatCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _chatCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetChatCategorizeTreeItems(new ChatCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parent = (ChatCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _chatCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChatCategorizeTreeItems(new ChatCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _chatCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var chatTreeViewItems = new List<ChatTreeViewItem>();

            {
                var categorizeChatTreeViewItems = new List<ChatCategorizeTreeViewItem>();
                categorizeChatTreeViewItems.Add(selectTreeViewItem);

                for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
                {
                    categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                    chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in chatTreeViewItems)
            {
                sb.AppendLine(LairConverter.ToChatString(item.Value.Tag, null));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Tag, null));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _chatCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetChatCategorizeTreeItems())
            {
                selectTreeViewItem.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetChatTreeItems())
            {
                if (selectTreeViewItem.Value.ChatTreeItems.Any(n => n.Tag == item.Tag)) continue;

                selectTreeViewItem.Value.ChatTreeItems.Add(item);
            }

            foreach (var item in Clipboard.GetChats())
            {
                if (selectTreeViewItem.Value.ChatTreeItems.Any(n => n.Tag == item.Item1)) continue;

                var chatTreeItem = new ChatTreeItem(item.Item1);

                selectTreeViewItem.Value.ChatTreeItems.Add(chatTreeItem);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemTrustOnMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var chatTreeViewItems = new List<ChatTreeViewItem>();

            var categorizeChatTreeViewItems = new List<ChatCategorizeTreeViewItem>();
            categorizeChatTreeViewItems.Add(selectTreeViewItem);

            for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
            {
                categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
            }

            foreach (var item in chatTreeViewItems)
            {
                item.Value.IsTrustEnabled = true;
                item.Update();
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemTrustOffMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var chatTreeViewItems = new List<ChatTreeViewItem>();

            var categorizeChatTreeViewItems = new List<ChatCategorizeTreeViewItem>();
            categorizeChatTreeViewItems.Add(selectTreeViewItem);

            for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
            {
                categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
            }

            foreach (var item in chatTreeViewItems)
            {
                item.Value.IsTrustEnabled = false;
                item.Update();
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var chatTreeViewItems = new List<ChatTreeViewItem>();

            var categorizeChatTreeViewItems = new List<ChatCategorizeTreeViewItem>();
            categorizeChatTreeViewItems.Add(selectTreeViewItem);

            for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
            {
                categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
            }

            foreach (var item in chatTreeViewItems)
            {
                lock (item.Value.ThisLock)
                {
                    item.Value.ReadChatMessages.AddRange(item.Value.UnreadChatMessages);
                    item.Value.UnreadChatMessages.Clear();
                }
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemChatListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            HashSet<Chat> chats = new HashSet<Chat>(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.Chats));

            {
                var chatCategorizeTreeItems = new List<ChatCategorizeTreeItem>();
                chatCategorizeTreeItems.Add(_treeViewItem.Value);

                for (int i = 0; i < chatCategorizeTreeItems.Count; i++)
                {
                    chatCategorizeTreeItems.AddRange(chatCategorizeTreeItems[i].Children);
                    chats.ExceptWith(chatCategorizeTreeItems[i].ChatTreeItems.Select(n => n.Tag));
                }
            }

            ChatListWindow window = new ChatListWindow(chats, _lairManager);
            window.Owner = _mainWindow;

            window.ChatJoinEvent += (object sender2, Chat tag) =>
            {
                var channelTreeItem = new ChatTreeItem(tag);

                selectTreeViewItem.Value.ChatTreeItems.Add(channelTreeItem);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.ShowDialog();
        }

        private void _chatTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _chatTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Chat", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<ChatCategorizeTreeViewItem>().LastOrDefault() as ChatCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.ChatTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _chatTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChatTreeItems(new ChatTreeItem[] { selectTreeViewItem.Value });

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<ChatCategorizeTreeViewItem>().LastOrDefault() as ChatCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.ChatTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _chatTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChatTreeItems(new ChatTreeItem[] { selectTreeViewItem.Value });
        }

        private void _chatTreeItemTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(LairConverter.ToChatString(selectTreeViewItem.Value.Tag, null));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Tag, null));

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
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)
                    || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                {
                    var index = _listView.GetCurrentIndex(e.GetPosition);
                    if (index == -1) return;

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                    var selectItem = listViewItemCollection[index];

                    if (_listView.SelectedItems.Contains(selectItem))
                    {
                        _listView.SelectedItems.Remove(selectItem);
                    }
                    else
                    {
                        _listView.SelectedItems.Add(selectItem);
                    }
                }
                else
                {
                    var index = _listView.GetCurrentIndex(e.GetPosition);
                    if (index == -1) return;

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                    var selectItem = listViewItemCollection[index];

                    if (_listView.SelectedItems.Count != 1 || !_listView.SelectedItems.Contains(selectItem))
                    {
                        _listView.SelectedItems.Clear();
                        _listView.SelectedItems.Add(selectItem);
                    }
                }
            }
            else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                var selectItem = listViewItemCollection[index];

                if (!_listView.SelectedItems.Contains(selectItem))
                {
                    _listView.SelectedItems.Clear();
                    _listView.SelectedItems.Add(selectItem);
                }
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
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            var selectItem = _listView.SelectedItem as ChatMessageWrapper;
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
                    var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);

                    item.IsEnabled = (digitalSignature != null);
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
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var trustSignatures = new HashSet<string>();
            trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
            trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            var responsMessages = _listView.SelectedItems.OfType<ChatMessageWrapper>().Select(n => n.Value).ToList();

            ChatMessageEditWindow window = new ChatMessageEditWindow(selectTreeViewItem.Value.Tag, "", responsMessages, digitalSignature, trustSignatures.Contains(digitalSignature.ToString()), _lairManager);
            window.Owner = _mainWindow;

            window.Closed += (object sender2, EventArgs e2) =>
            {
                if (window.ChatMessage == null) return;
                if (selectTreeViewItem.Value.UnreadChatMessages.Contains(window.ChatMessage)) return;

                selectTreeViewItem.Value.UnreadChatMessages.Add(window.ChatMessage);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.Show();
        }

        private void _richTextBoxTrustMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _listView.SelectedItems.OfType<ChatMessageWrapper>())
            {
                if (_sectionTreeViewItem.Value.TrustSignatures.Contains(item.Value.Signature)) continue;
                _sectionTreeViewItem.Value.TrustSignatures.Add(item.Value.Signature);
            }
        }

        #endregion

        #region Tool

        private void _trustToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustEnabled = true;

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _trustToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustEnabled = false;

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _topicUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var trustSignatures = new HashSet<string>();
            trustSignatures.Add(_sectionTreeViewItem.Value.LeaderSignature);
            trustSignatures.UnionWith(_sectionTreeViewItem.Value.CacheSectionProfiles.SelectMany(n => n.TrustSignatures));

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            ChatMessageEditWindow window = new ChatMessageEditWindow(selectTreeViewItem.Value.Tag, "", null, digitalSignature, trustSignatures.Contains(digitalSignature.ToString()), _lairManager);
            window.Owner = _mainWindow;

            window.Closed += (object sender2, EventArgs e2) =>
            {
                if (window.ChatMessage == null) return;
                if (selectTreeViewItem.Value.UnreadChatMessages.Contains(window.ChatMessage)) return;

                selectTreeViewItem.Value.UnreadChatMessages.Add(window.ChatMessage);

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
            if (_treeView.SelectedItem is ChatCategorizeTreeViewItem)
            {
                _chatCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewItem)
            {
                _chatCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewItem)
            {
                _chatTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewItem)
            {
                _chatCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewItem)
            {
                _chatTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewItem)
            {
                _chatCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewItem)
            {
                _chatTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChatCategorizeTreeViewItem)
            {
                _chatCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChatTreeViewItem)
            {

            }
        }

        private void Execute_Search(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }

        ~ChatControl()
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

    class ChatMessageWrapper : IEquatable<ChatMessageWrapper>
    {
        public bool IsNew { get; set; }
        public bool IsTrust { get; set; }
        public ChatMessage Value { get; set; }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ChatMessageWrapper)) return false;

            return this.Equals((ChatMessageWrapper)obj);
        }

        public bool Equals(ChatMessageWrapper other)
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
