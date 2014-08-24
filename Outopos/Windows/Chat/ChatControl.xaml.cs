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
using Outopos;
using Outopos.Properties;
using Library;
using Library.Collections;
using Library.Net.Outopos;
using Library.Security;
using A = Library.Net.Amoeba;
using System.Windows.Documents;

namespace Outopos.Windows
{
    /// <summary>
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    partial class ChatControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private OutoposManager _outoposManager;
        private BufferManager _bufferManager;

        private static Random _random = new Random();

        private Thread _searchThread;
        private Thread _cacheThread;

        private volatile bool _refresh;
        private volatile bool _cacheUpdate;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private ChatCategorizeTreeViewItem _treeViewItem;
        private ObservableCollection<ChatMessageWrapper> _listViewItemCollection = new ObservableCollection<ChatMessageWrapper>();

        private const HashAlgorithm _hashAlgorithm = HashAlgorithm.Sha512;

        public ChatControl(OutoposManager outoposManager, BufferManager bufferManager)
        {
            _outoposManager = outoposManager;
            _bufferManager = bufferManager;

            _treeViewItem = new ChatCategorizeTreeViewItem(Settings.Instance.ChatControl_ChatCategorizeTreeItem);

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

            _topicUploadButton.IsEnabled = false;
            _messageUploadButton.IsEnabled = false;

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);

            RichTextBoxHelper.ChatClickEvent += this.ChatClickEvent;
            RichTextBoxHelper.GetAnchorChatMessageWrapperEvent = this.GetAnchorChatMessageWrapperEvent;

            RichTextBoxHelper.GetMaxHeightEvent = this.GetMaxHeightEvent;
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listView.Items.Refresh();
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

        private ChatMessageWrapper GetAnchorChatMessageWrapperEvent(object sender, Anchor anchor)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return null;

            try
            {
                var pair = selectTreeViewItem.Value.ChatMessageInfos
                    .First(n => n.Key.Header.VerifyHash(anchor.Hash, anchor.HashAlgorithm));

                return new ChatMessageWrapper(pair.Key, pair.Value, Trust.ContainSignature(pair.Key.Header.Certificate.ToString()));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private double GetMaxHeightEvent(object sender)
        {
            return _listView.ActualHeight;
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

                        ChatTopicInfo chatTopicInfo = null;

                        var newList = new HashSet<ChatMessageWrapper>();

                        lock (chatTreeViewItem.Value.ThisLock)
                        {
                            chatTopicInfo = chatTreeViewItem.Value.ChatTopicInfo;

                            newList.UnionWith(chatTreeViewItem.Value.ChatMessageInfos
                                .Select(n => new ChatMessageWrapper(n.Key, n.Value, Trust.ContainSignature(n.Key.Header.Certificate.ToString()))));

                            foreach (var pair in chatTreeViewItem.Value.ChatMessageInfos.ToArray())
                            {
                                chatTreeViewItem.Value.ChatMessageInfos[pair.Key] = (pair.Value & ~ChatMessageState.IsUnread);
                            }
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
                                    var text = RichTextBoxHelper.MessageToString(item.Info.Header.CreationTime, item.Info.Header.Certificate.ToString(), item.Info.Content.Comment).ToLower();
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

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (chatTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            if (chatTopicInfo != null)
                            {
                                RichTextBoxHelper.SetRichTextBox(_chatTopicRichTextBox, chatTopicInfo);
                            }
                            else
                            {
                                _chatTopicRichTextBox.Document = new FlowDocument();
                            }

                            {
                                _trustToggleButton.IsEnabled = true;
                                _trustToggleButton.IsChecked = chatTreeViewItem.Value.IsTrustEnabled;

                                _topicUploadButton.IsEnabled = Trust.ContainSignature(Settings.Instance.Global_ProfileItem.UploadSignature);

                                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);

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

                            this.Scroll();

                            this.Update_TreeView_Color();
                        }));
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        private void Scroll()
        {
            if (_listViewItemCollection.Count > 0)
            {
                _listView.GoBottom();

                var topItem = _listViewItemCollection.Where(n => n.State.HasFlag(ChatMessageState.IsUnread)).FirstOrDefault();
                if (topItem == null) topItem = _listViewItemCollection.LastOrDefault();
                if (topItem != null) _listView.ScrollIntoView(topItem);
            }
        }

        private void Cache()
        {
            try
            {
                for (; ; )
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
                            lock (treeViewItem.Value.ThisLock)
                            {
                                var oldInfo = treeViewItem.Value.ChatTopicInfo;
                                ChatTopicInfo newInfo;

                                {
                                    var infos = new List<ChatTopicInfo>();
                                    if (oldInfo != null) infos.Add(oldInfo);

                                    newInfo = this.GetChatTopicInfo(treeViewItem.Value.Tag, infos);
                                }

                                if (oldInfo != newInfo)
                                {
                                    treeViewItem.Value.ChatTopicInfo = newInfo;
                                    isUpdate |= true;
                                }
                            }
                        }

                        // ChatMessage
                        {
                            var oldList = new List<ChatMessageInfo>();
                            var newList = new List<ChatMessageInfo>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                oldList.AddRange(treeViewItem.Value.ChatMessageInfos.Keys);
                                newList.AddRange(this.GetChatMessageInfos(treeViewItem.Value.Tag, oldList, treeViewItem.Value.IsTrustEnabled));

                                foreach (var item in oldList)
                                {
                                    if (!newList.Contains(item))
                                    {
                                        treeViewItem.Value.ChatMessageInfos.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    if (!oldList.Contains(item))
                                    {
                                        treeViewItem.Value.ChatMessageInfos.Add(item, ChatMessageState.IsUnread);
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
            catch (Exception)
            {

            }
        }

        private ChatTopicInfo GetChatTopicInfo(Chat chat, IEnumerable<ChatTopicInfo> cachedInfos)
        {
            var caches = new Dictionary<ChatTopicHeader, ChatTopicContent>();

            foreach (var info in cachedInfos)
            {
                caches.Add(info.Header, info.Content);
            }

            // Trust
            {
                var headers = new List<ChatTopicHeader>();

                {
                    var hashSet = new HashSet<ChatTopicHeader>();

                    foreach (var header in CollectionUtilities.Unite(cachedInfos.Select(n => n.Header), _outoposManager.GetChatTopicHeaders(chat)))
                    {
                        if (!Trust.ContainSignature(header.Certificate.ToString())) continue;

                        hashSet.Add(header);
                    }

                    headers.AddRange(hashSet);
                }

                headers.Sort((x, y) =>
                {
                    return y.CreationTime.CompareTo(x.CreationTime);
                });

                foreach (var header in headers)
                {
                    ChatTopicContent content;

                    if (!caches.TryGetValue(header, out content))
                    {
                        content = _outoposManager.GetContent(header);
                    }

                    if (content == null) continue;

                    return new ChatTopicInfo(header, content);
                }
            }

            return null;
        }

        private IEnumerable<ChatMessageInfo> GetChatMessageInfos(Chat chat, IEnumerable<ChatMessageInfo> cachedInfos, bool trust)
        {
            var infos = new HashSet<ChatMessageInfo>();

            {
                var caches = new Dictionary<ChatMessageHeader, ChatMessageContent>();

                foreach (var info in cachedInfos)
                {
                    caches.Add(info.Header, info.Content);
                }

                var now = DateTime.UtcNow;

                // Trust
                {
                    var headers = new List<ChatMessageHeader>();

                    {
                        var hashSet = new HashSet<ChatMessageHeader>();

                        foreach (var header in CollectionUtilities.Unite(cachedInfos.Select(n => n.Header), _outoposManager.GetChatMessageHeaders(chat)))
                        {
                            if ((now - header.CreationTime).TotalDays > 64) continue;
                            if (!Trust.ContainSignature(header.Certificate.ToString())) continue;

                            hashSet.Add(header);
                        }

                        headers.AddRange(hashSet);
                    }

                    headers.Sort((x, y) =>
                    {
                        return y.CreationTime.CompareTo(x.CreationTime);
                    });

                    int count = 0;

                    foreach (var header in headers)
                    {
                        ChatMessageContent content;

                        if (!caches.TryGetValue(header, out content))
                        {
                            content = _outoposManager.GetContent(header);
                        }

                        if (content == null) continue;

                        infos.Add(new ChatMessageInfo(header, content));

                        if (++count >= 1024) break;
                    }
                }

                // Untrust
                if (!trust)
                {
                    var headers = new List<ChatMessageHeader>();

                    {
                        var hashSet = new HashSet<ChatMessageHeader>();

                        foreach (var header in CollectionUtilities.Unite(cachedInfos.Select(n => n.Header), _outoposManager.GetChatMessageHeaders(chat)))
                        {
                            if (header.Cost <= (Trust.GetLimit() - 2)) continue;
                            if ((now - header.CreationTime).TotalDays > 7) continue;
                            if (Trust.ContainSignature(header.Certificate.ToString())) continue;

                            hashSet.Add(header);
                        }

                        headers.AddRange(hashSet);
                    }

                    headers.Sort((x, y) =>
                    {
                        int c = y.Cost.CompareTo(x.Cost);
                        if (c != 0) return c;
                        c = y.CreationTime.CompareTo(x.CreationTime);
                        if (c != 0) return c;

                        return CollectionUtilities.Compare(x.CreateHash(_hashAlgorithm), y.CreateHash(_hashAlgorithm));
                    });

                    int count = 0;

                    foreach (var header in headers)
                    {
                        ChatMessageContent content;

                        if (!caches.TryGetValue(header, out content))
                        {
                            content = _outoposManager.GetContent(header);
                        }

                        if (content == null) continue;

                        infos.Add(new ChatMessageInfo(header, content));

                        if (++count >= 256) break;
                    }
                }
            }

            var sortedList = infos.ToList();
            sortedList.Sort((x, y) => y.Header.CreationTime.CompareTo(x.Header.CreationTime));

            return sortedList.Take(1024);
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
                    .Where(n => n.Value.ChatMessageInfos.Any(m => m.Value.HasFlag(ChatMessageState.IsUnread))))
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
                            textBlock.Foreground = new SolidColorBrush(App.Colors.Tree_Hit);
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

        #region _tabControl

        private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != _tabControl) return;

            if (_tabControl.SelectedItem == _messageTabItem)
            {
                this.Scroll();
            }
        }

        #endregion

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
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow(selectTreeViewItem.Value.Name);
            window.Title = LanguagesManager.Instance.NameWindow_Title_Category;
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Name = window.Text;

                selectTreeViewItem.Update();
            }

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
                sb.AppendLine(OutoposConverter.ToChatString(item.Value.Tag));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Tag));
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

            foreach (var tag in Clipboard.GetChats())
            {
                if (selectTreeViewItem.Value.ChatTreeItems.Any(n => n.Tag == tag)) continue;

                var chatTreeItem = new ChatTreeItem(tag);

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
                    foreach (var pair in item.Value.ChatMessageInfos.ToArray())
                    {
                        item.Value.ChatMessageInfos[pair.Key] = pair.Value & ~ChatMessageState.IsUnread;
                    }
                }
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemChatListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            HashSet<Chat> chats = new HashSet<Chat>(Settings.Instance.Global_Cache_ProfileInfos.ToArray().SelectMany(n => n.Value.Content.Chats));

            {
                var chatCategorizeTreeItems = new List<ChatCategorizeTreeItem>();
                chatCategorizeTreeItems.Add(_treeViewItem.Value);

                for (int i = 0; i < chatCategorizeTreeItems.Count; i++)
                {
                    chatCategorizeTreeItems.AddRange(chatCategorizeTreeItems[i].Children);
                    chats.ExceptWith(chatCategorizeTreeItems[i].ChatTreeItems.Select(n => n.Tag));
                }
            }

            ChatListWindow window = new ChatListWindow(chats, _outoposManager);
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

            sb.AppendLine(OutoposConverter.ToChatString(selectTreeViewItem.Value.Tag));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Tag));

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
                int c = x.Info.Header.CreationTime.CompareTo(y.Info.Header.CreationTime);
                if (c != 0) return c;
                c = x.Info.Header.Certificate.ToString().CompareTo(y.Info.Header.Certificate.ToString());
                if (c != 0) return c;
                c = x.GetHashCode().CompareTo(y.GetHashCode());
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

        #region _chatTopicRichText

        private void _chatTopicRichTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(_chatTopicRichTextBox.Selection.Text.Replace("\u00A0", ""));
        }

        #endregion

        #region _richTextBox

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBoxEx;
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
                    var richTextBoxList = new List<RichTextBoxEx>();
                    richTextBoxList.Add(richTextBox);

                    for (int i = 0; i < richTextBoxList.Count; i++)
                    {
                        richTextBoxList.AddRange(richTextBoxList[i].Children);
                    }

                    item.IsEnabled = richTextBoxList.Any(n => !n.Selection.IsEmpty);
                }
                else if (item.Name == "_richTextBoxResponsMenuItem")
                {
                    var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);

                    item.IsEnabled = (digitalSignature != null);
                }
            }
        }

        private void _richTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBoxEx;
            if (richTextBox == null) return;

            var richTextBoxList = new List<RichTextBoxEx>();
            richTextBoxList.Add(richTextBox);

            for (int i = 0; i < richTextBoxList.Count; i++)
            {
                richTextBoxList.AddRange(richTextBoxList[i].Children);
            }

            for (int i = 0; i < richTextBoxList.Count; i++)
            {
                if (richTextBoxList[i].Selection.IsEmpty) continue;

                Clipboard.SetText(richTextBoxList[i].Selection.Text);

                break;
            }
        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);
            if (digitalSignature == null) return;

            var anchors = _listView.SelectedItems.OfType<ChatMessageWrapper>().Select(n => new Anchor(n.Info.Header.CreateHash(_hashAlgorithm), _hashAlgorithm)).ToList();

            ChatMessageEditWindow window = new ChatMessageEditWindow(
                selectTreeViewItem.Value.Tag,
                "",
                anchors,
                digitalSignature,
                Trust.ContainSignature(digitalSignature.ToString()),
                _outoposManager);

            window.Owner = _mainWindow;

            window.Show();
        }

        private void _richTextBoxTrustMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _listView.SelectedItems.OfType<ChatMessageWrapper>())
            {
                var signature = item.Info.Header.Certificate.ToString();

                if (Settings.Instance.Global_ProfileItem.TrustSignatures.Contains(signature)) continue;
                Settings.Instance.Global_ProfileItem.TrustSignatures.Add(signature);
            }
        }

        #endregion

        #region Tool

        private void _trustToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustEnabled = _trustToggleButton.IsChecked.Value;

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _topicUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);
            if (digitalSignature == null) return;

            if (Trust.ContainSignature(digitalSignature.ToString()))
            {
                string comment = "";

                if (selectTreeViewItem.Value.ChatTopicInfo != null)
                {
                    comment = selectTreeViewItem.Value.ChatTopicInfo.Content.Comment;
                }

                ChatTopicEditWindow window = new ChatTopicEditWindow(
                    selectTreeViewItem.Value.Tag,
                    comment,
                    digitalSignature,
                    true,
                    _outoposManager);

                window.Owner = _mainWindow;

                window.Show();
            }
        }

        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);
            if (digitalSignature == null) return;

            ChatMessageEditWindow window = new ChatMessageEditWindow(
                selectTreeViewItem.Value.Tag,
                "",
                null,
                digitalSignature,
                Trust.ContainSignature(digitalSignature.ToString()),
                _outoposManager);

            window.Owner = _mainWindow;

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
    }

    class ChatMessageWrapper : IEquatable<ChatMessageWrapper>
    {
        public ChatMessageWrapper(ChatMessageInfo info, ChatMessageState state, bool isTrust)
        {
            this.Info = info;
            this.State = state;
            this.IsTrust = isTrust;
        }

        public ChatMessageInfo Info { get; private set; }
        public ChatMessageState State { get; private set; }
        public bool IsTrust { get; private set; }

        public override int GetHashCode()
        {
            if (this.Info == null) return 0;
            else return this.Info.GetHashCode();
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

            if (this.Info != other.Info
                || this.State != other.State
                || this.IsTrust != other.IsTrust)
            {
                return false;
            }

            return true;
        }
    }
}
