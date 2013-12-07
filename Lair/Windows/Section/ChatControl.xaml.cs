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
    /// Interaction logic for ChatControl.xaml
    /// </summary>
    partial class ChatControl : UserControl, IDisposable
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private SectionControl _sectionControl;
        private LairManager _lairManager;
        private BufferManager _bufferManager;
        private SectionTreeViewItem _sectionTreeViewItem;

        private static Random _random = new Random();

        private Thread _searchThread;
        private Thread _cacheThread;

        private volatile bool _refresh;
        private volatile bool _cacheUpdate;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private ChatCategorizeTreeViewItem _treeViewItem;
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();

        private volatile bool _disposed;

        public ChatControl(SectionControl sectionControl, LairManager lairManager, BufferManager bufferManager,
            SectionTreeViewItem sectionTreeViewItem)
        {
            _sectionControl = sectionControl;
            _lairManager = lairManager;
            _bufferManager = bufferManager;
            _sectionTreeViewItem = sectionTreeViewItem;

            _treeViewItem = new ChatCategorizeTreeViewItem(_sectionTreeViewItem.Value.ChatCategorizeTreeItem);

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

            this.Update();

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
                    Thread.Sleep(1000);
                    if (!_refresh) continue;

                    TreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        selectTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (selectTreeViewItem is ChatCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (selectTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _trustToggleButton.ClearValue(ToggleButton.ForegroundProperty);
                            _trustToggleButton.IsEnabled = false;
                            _trustToggleButton.IsChecked = false;

                            _topicUploadButton.IsEnabled = false;
                            _messageUploadButton.IsEnabled = false;

                            _listViewItemCollection.Clear();
                        }));
                    }
                    else if (selectTreeViewItem is ChatTreeViewItem)
                    {
                        ChatTreeViewItem chatTreeViewItem = (ChatTreeViewItem)selectTreeViewItem;

                        var newList = new HashSet<ChatMessageInformaiton>();

                        lock (chatTreeViewItem.Value.ThisLock)
                        {
                            foreach (var item in chatTreeViewItem.Value.ChatMessageInformation)
                            {
                                newList.Add(item);
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
                                List<ChatMessageInformaiton> list = new List<ChatMessageInformaiton>();

                                foreach (var item in newList)
                                {
                                    var text = RichTextBoxHelper.MessageToString(item).ToLower();
                                    if (!words.All(n => text.Contains(n))) continue;

                                    list.Add(item);
                                }

                                newList.Clear();
                                newList.UnionWith(list);
                            }
                        }

                        var oldList = new HashSet<ChatMessageInformaiton>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            oldList.UnionWith(_listViewItemCollection.OfType<ChatMessageInformaiton>().ToArray());

                            foreach (var item in oldList)
                            {
                                item.IsNew = false;
                            }
                        }));

                        var removeList = new List<ChatMessageInformaiton>();
                        var addList = new List<ChatMessageInformaiton>();

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
                                var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);

                                {
                                    _trustToggleButton.IsEnabled = true;

                                    if (chatTreeViewItem.Value.IsTrustEnabled)
                                    {
                                        _trustToggleButton.Foreground = new SolidColorBrush(App.LairColors.Trust_On);
                                    }
                                    else
                                    {
                                        _trustToggleButton.Foreground = new SolidColorBrush(App.LairColors.Trust_Off);
                                    }
                                }

                                _topicUploadButton.IsEnabled = (digitalSignature != null);
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
                                var topItem = _listViewItemCollection.OfType<ChatMessageInformaiton>().FirstOrDefault(n => n.IsNew);
                                if (topItem == null) topItem = _listViewItemCollection.OfType<ChatMessageInformaiton>().LastOrDefault();
                                if (topItem != null) _listView.ScrollIntoView(topItem);
                            }

                            layoutUpdated = _layoutUpdated;

                            this.Update_TreeView_Color();
                        }));

                        while (_layoutUpdated == layoutUpdated) Thread.Sleep(100);

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            _listView.UpdateLayout();
                            var topItem = _listViewItemCollection.OfType<ChatMessageInformaiton>().FirstOrDefault(n => n.IsNew);
                            if (topItem == null) topItem = _listViewItemCollection.OfType<ChatMessageInformaiton>().LastOrDefault();
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
                            Dictionary<string, ChatTopicContent> chatTopicDictionary = new Dictionary<string, ChatTopicContent>();

                            foreach (var item in _lairManager.GetChatTopics(treeViewItem.Value.Chat))
                            {
                                chatTopicDictionary[item.Certificate.ToString()] = item;
                            }

                            List<ChatTopicContent> chatTopics = new List<ChatTopicContent>();

                            foreach (var signature in _sectionControl.GetTrustSignatures())
                            {
                                ChatTopicContent tempChatTopic;

                                if (chatTopicDictionary.TryGetValue(signature, out tempChatTopic))
                                {
                                    chatTopics.Add(tempChatTopic);
                                }
                            }

                            chatTopics.Sort((x, y) =>
                            {
                                return y.CreationTime.CompareTo(x.CreationTime);
                            });

                            lock (treeViewItem.Value.ThisLock)
                            {
                                foreach (var chatTopic in chatTopics)
                                {
                                    try
                                    {
                                        if (treeViewItem.Value.ChatTopicInformation.Header != chatTopic)
                                        {
                                            var information = new ChatTopicInformation();
                                            information.IsNew = true;
                                            information.Header = chatTopic;
                                            information.Content = _lairManager.GetContent(chatTopic);

                                            treeViewItem.Value.ChatTopicInformation = information;
                                            isUpdate |= true;
                                        }

                                        break;
                                    }
                                    catch (Exception)
                                    {

                                    }
                                }
                            }
                        }

                        // ChatMessage
                        {
                            var oldList = new HashSet<ChatMessageContent>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                lock (treeViewItem.Value.ThisLock)
                                {
                                    oldList.UnionWith(treeViewItem.Value.ChatMessageInformation.Select(n => n.Header));
                                }
                            }

                            HashSet<ChatMessageContent> newList = new HashSet<ChatMessageContent>(_lairManager.GetChatMessages(treeViewItem.Value.Chat));
                            newList.UnionWith(oldList);

                            {
                                var trustSignatures = new HashSet<string>(_sectionControl.GetTrustSignatures());
                                var tempList = new List<ChatMessageContent>();

                                foreach (var message in newList)
                                {
                                    if (!trustSignatures.Contains(message.Certificate.ToString())) continue;

                                    tempList.Add(message);
                                }

                                tempList.Sort((x, y) =>
                                {
                                    int c = x.CreationTime.CompareTo(y.CreationTime);
                                    if (c != 0) return c;
                                    c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                                    if (c != 0) return c;

                                    return x.GetHashCode().CompareTo(y.GetHashCode());
                                });

                                tempList = tempList.Skip(tempList.Count - 256).ToList();
                                tempList.Reverse();

                                newList.Clear();
                                newList.UnionWith(tempList);
                            }

                            lock (treeViewItem.Value.ThisLock)
                            {
                                foreach (var item in treeViewItem.Value.ChatMessageInformation.ToArray())
                                {
                                    if (!newList.Contains(item.Header))
                                    {
                                        treeViewItem.Value.ChatMessageInformation.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    try
                                    {
                                        if (!oldList.Contains(item))
                                        {
                                            var information = new ChatMessageInformaiton();
                                            information.IsNew = true;
                                            information.Header = item;
                                            information.Content = _lairManager.GetContent(item);

                                            treeViewItem.Value.ChatMessageInformation.Add(information);
                                            isUpdate |= true;
                                        }

                                        break;
                                    }
                                    catch (Exception)
                                    {

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

                    _autoResetEvent.WaitOne(1000 * 60);
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
                    .Where(n => (n.Value.ChatTopicInformation != null && n.Value.ChatTopicInformation.IsNew) || n.Value.ChatMessageInformation.Any(m => m.IsNew)))
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

        }

        private void _chatCategorizeTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _chatCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

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
                categorizeChatTreeViewItems.Add(_treeViewItem);

                for (int i = 0; i < categorizeChatTreeViewItems.Count; i++)
                {
                    categorizeChatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatCategorizeTreeViewItem>());
                    chatTreeViewItems.AddRange(categorizeChatTreeViewItems[i].Items.OfType<ChatTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in chatTreeViewItems)
            {
                sb.AppendLine(LairConverter.ToChatString(item.Value.Chat));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Chat));
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
                selectTreeViewItem.Value.ChatTreeItems.Add(item);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _chatCategorizeTreeViewItemTrustOnMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _chatCategorizeTreeViewItemTrustOffMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _chatCategorizeTreeViewItemMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _chatCategorizeTreeViewItemChatListMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _chatTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as ChatTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

        }

        private void _chatTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

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

            sb.AppendLine(LairConverter.ToChatString(selectTreeViewItem.Value.Chat));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Chat));

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

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).OfType<ChatMessageInformaiton>().ToArray();
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

                    var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).OfType<ChatMessageInformaiton>().ToArray();
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

                var listViewItemCollection = CollectionViewSource.GetDefaultView(_listView.ItemsSource).OfType<ChatMessageInformaiton>().ToArray();
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
            var list = new List<object>(_listViewItemCollection);

            list.Sort((x, y) =>
            {
                if (x is ChatTopicInformation)
                {
                    if (y is ChatTopicInformation)
                    {
                        var vx = ((ChatTopicInformation)x).Header;
                        var vy = ((ChatTopicInformation)y).Header;

                        int c = vx.CreationTime.CompareTo(vy.CreationTime);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is ChatMessageInformaiton)
                    {
                        return 1;
                    }
                }
                else if (x is ChatMessageInformaiton)
                {
                    if (y is ChatMessageInformaiton)
                    {
                        var vx = ((ChatMessageInformaiton)x).Header;
                        var vy = ((ChatMessageInformaiton)y).Header;

                        int c = vx.CreationTime.CompareTo(vy.CreationTime);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is ChatTopicInformation)
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

        #endregion

        #region _richTextBox

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _richTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _richTextBoxTrustMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _richTextBoxFilterWordMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _richTextBoxFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region Tool

        private void _trustToggleButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustEnabled = !selectTreeViewItem.Value.IsTrustEnabled;

            this.Update_Cache();

            e.Handled = true;
        }

        private void _topicUploadButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

        }

        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChatTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

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
                _chatCategorizeTreeViewItemNewMenuItem_Click(null, null);
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
}
