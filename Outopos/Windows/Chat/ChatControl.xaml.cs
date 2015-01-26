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
        private ObservableCollection<ChatMessageWrapper> _listBoxItemCollection = new ObservableCollection<ChatMessageWrapper>();

        private const HashAlgorithm _hashAlgorithm = HashAlgorithm.Sha256;

        public ChatControl(OutoposManager outoposManager, BufferManager bufferManager)
        {
            _outoposManager = outoposManager;
            _bufferManager = bufferManager;

            _treeViewItem = new ChatCategorizeTreeViewItem(Settings.Instance.ChatControl_ChatCategorizeTreeItem);

            InitializeComponent();

            // ReplyMessage
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tools\ReplyMessage.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                _replyMessageButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
            }

            // NewMessage
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tools\NewMessage.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                _newMessageButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
            }

            // Trust
            {
                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], @"Tools\Trust.png"), FileMode.Open, FileAccess.Read, FileShare.Read);
                bitmap.EndInit();
                if (bitmap.CanFreeze) bitmap.Freeze();

                _trustToggleButton.Content = new Image() { Source = bitmap, Height = 32, Width = 32 };
            }

            _treeView.Items.Add(_treeViewItem);

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _listBox.ItemsSource = _listBoxItemCollection;

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

            _newMessageButton.IsEnabled = false;
            _replyMessageButton.IsEnabled = false;

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);

            RichTextBoxHelper.ChatClickEvent += this.ChatClickEvent;
            RichTextBoxHelper.GetAnchorChatMessageWrapperEvent = this.GetAnchorChatMessageWrapperEvent;

            RichTextBoxHelper.GetMaxHeightEvent = this.GetMaxHeightEvent;
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            _listBox.Items.Refresh();
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
                var pair = selectTreeViewItem.Value.ChatMessages
                    .First(n => n.Key.VerifyHash(anchor.Hash, anchor.HashAlgorithm));

                return new ChatMessageWrapper(pair.Key, pair.Value, Trust.ContainSignature(pair.Key.Certificate.ToString()));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private double GetMaxHeightEvent(object sender)
        {
            return _listBox.ActualHeight;
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

                            _newMessageButton.IsEnabled = false;
                            _replyMessageButton.IsEnabled = false;
                            _trustToggleButton.IsEnabled = false;
                            _trustToggleButton.IsChecked = false;

                            _listBoxItemCollection.Clear();
                        }));
                    }
                    else if (tempTreeViewItem is ChatTreeViewItem)
                    {
                        ChatTreeViewItem chatTreeViewItem = (ChatTreeViewItem)tempTreeViewItem;

                        var newList = new HashSet<ChatMessageWrapper>();

                        lock (chatTreeViewItem.Value.ThisLock)
                        {
                            newList.UnionWith(chatTreeViewItem.Value.ChatMessages
                                .Select(n => new ChatMessageWrapper(n.Key, n.Value, Trust.ContainSignature(n.Key.Certificate.ToString()))));

                            foreach (var pair in chatTreeViewItem.Value.ChatMessages.ToArray())
                            {
                                chatTreeViewItem.Value.ChatMessages[pair.Key] = (pair.Value & ~ChatMessageState.IsUnread);
                            }
                        }

                        var oldList = new HashSet<ChatMessageWrapper>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listBoxItemCollection.ToArray());
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

                            {
                                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);

                                _newMessageButton.IsEnabled = (digitalSignature != null);

                                _trustToggleButton.IsEnabled = true;
                                _trustToggleButton.IsChecked = chatTreeViewItem.Value.IsTrustEnabled;
                            }

                            if (removeList.Count > 100)
                            {
                                _listBoxItemCollection.Clear();

                                foreach (var item in newList)
                                {
                                    _listBoxItemCollection.Add(item);
                                }
                            }
                            else
                            {
                                foreach (var item in addList)
                                {
                                    _listBoxItemCollection.Add(item);
                                }

                                foreach (var item in removeList)
                                {
                                    _listBoxItemCollection.Remove(item);
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
            if (_listBoxItemCollection.Count > 0)
            {
                _listBox.GoTop();
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

                    {
                        _outoposManager.SetSearchChats(chatTreeViewItems.Select(n => n.Value.Tag).ToArray());
                    }

                    foreach (var treeViewItem in chatTreeViewItems)
                    {
                        bool isUpdate = false;

                        // ChatMessage
                        {
                            var oldList = new List<ChatMessage>();
                            var newList = new List<ChatMessage>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                oldList.AddRange(treeViewItem.Value.ChatMessages.Keys);
                                newList.AddRange(this.GetChatMessages(treeViewItem.Value.Tag, oldList, treeViewItem.Value.IsTrustEnabled));

                                foreach (var item in oldList)
                                {
                                    if (!newList.Contains(item))
                                    {
                                        treeViewItem.Value.ChatMessages.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    if (!oldList.Contains(item))
                                    {
                                        treeViewItem.Value.ChatMessages.Add(item, ChatMessageState.IsUnread);
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

        private IEnumerable<ChatMessage> GetChatMessages(Chat chat, IEnumerable<ChatMessage> cachedChatMessages, bool isTrust)
        {
            var chatMessages = new HashSet<ChatMessage>();
            chatMessages.UnionWith(cachedChatMessages);

            if (isTrust)
            {
                chatMessages.UnionWith(_outoposManager.GetChatMessages(chat, -1));
            }
            else
            {
                chatMessages.UnionWith(_outoposManager.GetChatMessages(chat, Trust.GetLimit()));
            }

            var sortedList = new List<ChatMessage>();

            if (isTrust)
            {
                foreach (var chatMessage in chatMessages)
                {
                    if (!Trust.ContainSignature(chatMessage.Certificate.ToString())) continue;

                    sortedList.Add(chatMessage);
                }
            }
            else
            {
                foreach (var chatMessage in chatMessages)
                {
                    sortedList.Add(chatMessage);
                }
            }

            sortedList.Sort((x, y) => y.CreationTime.CompareTo(x.CreationTime));

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
                    .Where(n => n.Value.ChatMessages.Any(m => m.Value.HasFlag(ChatMessageState.IsUnread))))
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
                sb.AppendLine();
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
                    foreach (var pair in item.Value.ChatMessages.ToArray())
                    {
                        item.Value.ChatMessages[pair.Key] = pair.Value & ~ChatMessageState.IsUnread;
                    }
                }
            }

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemChatListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            HashSet<Chat> chats = new HashSet<Chat>(Settings.Instance.Global_Profiles.ToArray().SelectMany(n => n.Value.Chats));

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

        #region Tools

        private void _newMessageButton_Click(object sender, RoutedEventArgs e)
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

        private void _replyMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);
            if (digitalSignature == null) return;

            var anchors = _listBox.SelectedItems.OfType<ChatMessageWrapper>().Select(n => new Anchor(n.ChatMessage.CreateHash(_hashAlgorithm), _hashAlgorithm)).ToList();

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

        private void _trustToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustEnabled = _trustToggleButton.IsChecked.Value;

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        #endregion

        #region _listBox

        private void _listBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _listBox.Items.Refresh();
        }

        private void _listBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Home)
            {
                _listBox.GoTop();
            }
            else if (e.Key == System.Windows.Input.Key.End)
            {
                _listBox.GoBottom();
            }
            else if (e.Key == System.Windows.Input.Key.PageUp)
            {
                _listBox.PageUp();
            }
            else if (e.Key == System.Windows.Input.Key.PageDown)
            {
                _listBox.PageDown();
            }
        }

        private void _listBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)
                    || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                {
                    var index = _listBox.GetCurrentIndex(e.GetPosition);
                    if (index == -1) return;

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listBox.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                    var selectItem = listViewItemCollection[index];

                    if (_listBox.SelectedItems.Contains(selectItem))
                    {
                        _listBox.SelectedItems.Remove(selectItem);
                    }
                    else
                    {
                        _listBox.SelectedItems.Add(selectItem);
                    }
                }
                else
                {
                    var index = _listBox.GetCurrentIndex(e.GetPosition);
                    if (index == -1) return;

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listBox.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                    var selectItem = listViewItemCollection[index];

                    if (_listBox.SelectedItems.Count != 1 || !_listBox.SelectedItems.Contains(selectItem))
                    {
                        _listBox.SelectedItems.Clear();
                        _listBox.SelectedItems.Add(selectItem);
                    }
                }
            }
            else if (e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var index = _listBox.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var listViewItemCollection = CollectionViewSource.GetDefaultView(_listBox.ItemsSource).Cast<ChatMessageWrapper>().ToArray();
                var selectItem = listViewItemCollection[index];

                if (!_listBox.SelectedItems.Contains(selectItem))
                {
                    _listBox.SelectedItems.Clear();
                    _listBox.SelectedItems.Add(selectItem);
                }
            }
        }

        private void _listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_listBox.SelectedItems.Count != 0)
            {
                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == Settings.Instance.Global_ProfileItem.UploadSignature);

                _replyMessageButton.IsEnabled = (digitalSignature != null);
            }
            else
            {
                _replyMessageButton.IsEnabled = false;
            }
        }

        #endregion

        #region Sort

        private void Sort()
        {
            var list = _listBoxItemCollection.ToList();

            list.Sort((x, y) =>
            {
                int c = y.ChatMessage.CreationTime.CompareTo(x.ChatMessage.CreationTime);
                if (c != 0) return c;
                c = x.ChatMessage.Certificate.ToString().CompareTo(y.ChatMessage.Certificate.ToString());
                if (c != 0) return c;
                c = x.GetHashCode().CompareTo(y.GetHashCode());
                if (c != 0) return c;

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listBoxItemCollection.IndexOf(list[i]);

                if (i != o) _listBoxItemCollection.Move(o, i);
            }
        }

        #endregion

        #region _richTextBox

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBoxEx;
            if (richTextBox == null) return;

            var selectItem = _listBox.SelectedItem as ChatMessageWrapper;
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

            var anchors = _listBox.SelectedItems.OfType<ChatMessageWrapper>().Select(n => new Anchor(n.ChatMessage.CreateHash(_hashAlgorithm), _hashAlgorithm)).ToList();

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
            foreach (var item in _listBox.SelectedItems.OfType<ChatMessageWrapper>())
            {
                var signature = item.ChatMessage.Certificate.ToString();

                if (Settings.Instance.Global_ProfileItem.TrustSignatures.Contains(signature)) continue;
                Settings.Instance.Global_ProfileItem.TrustSignatures.Add(signature);
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
    }

    class ChatMessageWrapper : IEquatable<ChatMessageWrapper>
    {
        public ChatMessageWrapper(ChatMessage chatMessage, ChatMessageState state, bool isTrust)
        {
            this.ChatMessage = chatMessage;
            this.State = state;
            this.IsTrust = isTrust;
        }

        public ChatMessage ChatMessage { get; private set; }
        public ChatMessageState State { get; private set; }
        public bool IsTrust { get; private set; }

        public override int GetHashCode()
        {
            if (this.ChatMessage == null) return 0;
            else return this.ChatMessage.GetHashCode();
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

            if (this.ChatMessage != other.ChatMessage
                || this.State != other.State
                || this.IsTrust != other.IsTrust)
            {
                return false;
            }

            return true;
        }
    }
}
