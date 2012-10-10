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
using System.Windows.Documents;
using System.Windows.Input;
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

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for ChannelControl.xaml
    /// </summary>
    partial class ChannelControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private Thread _searchThread = null;
        private Thread _cacheThread = null;
        private Thread _filterThread = null;
        private volatile bool _refresh = false;
        private volatile bool _isEditing = false;
        private volatile bool _scroll = false;

        private ObservableCollection<MessageEx> _listViewItemCollection = new ObservableCollection<MessageEx>();

        private LockedDictionary<Channel, List<Message>> _messages = new LockedDictionary<Channel, List<Message>>();

        private static Random _random = new Random();

        public ChannelControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.ChannelControl_Category;
            _listView.ItemsSource = _listViewItemCollection;

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                if (App.SelectTab == "Channel")
                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "CacheThread";
            _cacheThread.Start();

            _filterThread = new Thread(new ThreadStart(this.Filter));
            _filterThread.Priority = ThreadPriority.Highest;
            _filterThread.IsBackground = true;
            _filterThread.Name = "FilterThread";
            _filterThread.Start();

            _lairManager.UnlockChannelsEvent += new UnlockChannelsEventHandler(_lairManager_UnlockChannelsEvent);
            _lairManager.UnlockMessagesEvent += new UnlockMessagesEventHandler(_lairManager_UnlockMessagesEvent);
            _lairManager.UnlockFiltersEvent += new UnlockFiltersEventHandler(_lairManager_UnlockFiltersEvent);

            RichTextBoxHelper.ChannelClickEvent += new ChannelClickEventHandler(RichTextBoxHelper_ChannelClickEvent);
            RichTextBoxHelper.SeedClickEvent += new SeedClickEventHandler(RichTextBoxHelper_SeedClickEvent);
            RichTextBoxHelper.LinkClickEvent += new LinkClickEventHandler(RichTextBoxHelper_LinkClickEvent);
            RichTextBoxHelper.GetMaxHeightEvent += new GetMaxHeightEventHandler(RichTextBoxHelper_GetMaxHeightEvent);

            _searchRowDefinition.Height = new GridLength(0);
        }

        private void Search()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(1000);
                    if (!_refresh) continue;

                    BoardTreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (_treeView.SelectedItem is CategoryTreeViewItem)
                        {
                            var selectitem = (CategoryTreeViewItem)_treeView.SelectedItem;

                            _listViewItemCollection.Clear();

                            _signatureComboBox.Items.Clear();
                            _signatureComboBox.Text = "";
                            _signatureComboBox.IsEnabled = false;

                            _signButton.IsEnabled = false;

                            _newMessageButton.IsEnabled = false;

                            if (App.SelectTab == "Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectitem.Value.Name);
                        }
                        else if (_treeView.SelectedItem is BoardTreeViewItem)
                        {
                            selectTreeViewItem = (BoardTreeViewItem)_treeView.SelectedItem;
                            selectTreeViewItem.Hit = false;
                        }
                    }), null);

                    if (selectTreeViewItem == null)
                    {
                        _refresh = false;

                        continue;
                    }

                    HashSet<Message> newList = new HashSet<Message>();
                    HashSet<Message> oldList = new HashSet<Message>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        oldList.UnionWith(_listViewItemCollection.OfType<MessageEx>().Select(n => n.Value).ToArray());
                    }), null);

                    IList<Message> messages;
                    IList<Filter> filters;

                    _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        _isEditing = true;

                        _signatureComboBox.Items.Clear();
                        _signatureComboBox.Items.Add("");

                        foreach (var item in filters)
                        {
                            _signatureComboBox.Items.Add(MessageConverter.ToSignatureString(item.Certificate));
                        }

                        _signatureComboBox.Text = selectTreeViewItem.Value.Signature;
                        _signatureComboBox.IsEnabled = true;

                        _isEditing = false;

                        _signButton.IsEnabled = true;

                        _newMessageButton.IsEnabled = true;
                    }), null);

                    Filter filter = filters.FirstOrDefault(n => selectTreeViewItem.Value.Signature == MessageConverter.ToSignatureString(n.Certificate));

                    if (filter != null)
                    {
                        foreach (var message in messages)
                        {
                            if (filter.Keys.Any(n => message.VerifyHash(n.Hash, n.HashAlgorithm)))
                            {
                                newList.Add(message);
                            }
                        }
                    }
                    else
                    {
                        foreach (var message in messages)
                        {
                            newList.Add(message);
                        }
                    }

                    List<CategoryTreeViewItem> categoryTreeViewItems = new List<CategoryTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        categoryTreeViewItems.AddRange(_treeViewItem.GetLineage(selectTreeViewItem).OfType<CategoryTreeViewItem>());
                    }), null);

                    foreach (var item in categoryTreeViewItems)
                    {
                        ChannelControl.Filter(ref newList, item.Value);
                    }

                    {
                        string searchText = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            searchText = _searchTextBox.Text.ToLower();
                        }), null);

                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            List<Message> list = new List<Message>();

                            foreach (var item in newList)
                            {
                                if (item.Content.ToLower().Contains(searchText))
                                {
                                    list.Add(item);
                                }
                            }

                            newList.Clear();
                            newList.UnionWith(list);
                        }
                    }

                    List<Message> sortList = new List<Message>();

                    {
                        var tempList = newList.ToList();

                        tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                        {
                            return x.CreationTime.CompareTo(y.CreationTime);
                        }));

                        sortList = tempList.Skip(tempList.Count - 256).ToList();

                        newList.Clear();
                        newList.UnionWith(sortList);
                    }

                    lock (_messages.ThisLock)
                    {
                        if (!_messages.ContainsKey(selectTreeViewItem.Value.Channel))
                        {
                            _messages[selectTreeViewItem.Value.Channel] = new List<Message>(sortList);
                        }
                        else if (!Collection.Equals(sortList, _messages[selectTreeViewItem.Value.Channel]))
                        {
                            _messages[selectTreeViewItem.Value.Channel].Clear();
                            _messages[selectTreeViewItem.Value.Channel].AddRange(sortList);
                        }
                    }

                    var removeList = new List<Message>();
                    var addList = new List<Message>();

                    foreach (var item in oldList)
                    {
                        if (!sortList.Contains(item)) removeList.Add(item);
                    }

                    foreach (var item in sortList)
                    {
                        if (!oldList.Contains(item)) addList.Add(item);
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (selectTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        bool sortFlag = false;

                        if (removeList.Count > 100)
                        {
                            sortFlag = true;

                            _listViewItemCollection.Clear();

                            foreach (var item in newList)
                            {
                                _listViewItemCollection.Add(new MessageEx(selectTreeViewItem.Value, item));
                            }
                        }
                        else
                        {
                            if (addList.Count != 0) sortFlag = true;
                            if (removeList.Count != 0) sortFlag = true;

                            foreach (var item in addList)
                            {
                                _listViewItemCollection.Add(new MessageEx(selectTreeViewItem.Value, item));
                            }

                            var tempList = _listViewItemCollection.Where(n => removeList.Contains(n.Value)).ToArray();

                            foreach (var item in tempList)
                            {
                                _listViewItemCollection.Remove(item);
                            }
                        }

                        selectTreeViewItem.Count = _listViewItemCollection.Count;

                        if (sortFlag) this.Sort();

                        if (_scroll)
                        {
                            if (_listViewItemCollection.Count > 0)
                            {
                                _listView.GoBottom();
                            }
                        }

                        if (App.SelectTab == "Channel")
                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));
                    }), null);
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
                for (; ; )
                {
                    List<BoardTreeViewItem> boardTreeViewItems = new List<BoardTreeViewItem>();

                    for (; ; )
                    {
                        List<BoardTreeViewItem> items = new List<BoardTreeViewItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            var list = new List<TreeViewItem>();
                            list.Add(_treeViewItem);

                            for (int i = 0; i < list.Count; i++)
                            {
                                foreach (TreeViewItem item in list[i].Items)
                                {
                                    list.Add(item);
                                }
                            }

                            foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                            {
                                items.Add(item);
                            }
                        }), null);

                        var selectTreeViewItem = items.FirstOrDefault(n => !boardTreeViewItems.Any(m => n == m));
                        if (selectTreeViewItem == null) break;

                        boardTreeViewItems.Add(selectTreeViewItem);

                        {
                            HashSet<Message> newList = new HashSet<Message>();

                            IList<Message> messages;
                            IList<Filter> filters;

                            _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                            Filter filter = filters.FirstOrDefault(n => selectTreeViewItem.Value.Signature == MessageConverter.ToSignatureString(n.Certificate));

                            if (filter != null)
                            {
                                foreach (var message in messages)
                                {
                                    if (filter.Keys.Any(n => message.VerifyHash(n.Hash, n.HashAlgorithm)))
                                    {
                                        newList.Add(message);
                                    }
                                }
                            }
                            else
                            {
                                foreach (var message in messages)
                                {
                                    newList.Add(message);
                                }
                            }

                            List<CategoryTreeViewItem> categoryTreeViewItems = new List<CategoryTreeViewItem>();

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                categoryTreeViewItems.AddRange(_treeViewItem.GetLineage(selectTreeViewItem).OfType<CategoryTreeViewItem>());
                            }), null);

                            foreach (var item in categoryTreeViewItems)
                            {
                                ChannelControl.Filter(ref newList, item.Value);
                            }

                            List<Message> sortList = new List<Message>();

                            {
                                var tempList = newList.ToList();

                                tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                                {
                                    return x.CreationTime.CompareTo(y.CreationTime);
                                }));

                                sortList = tempList.Skip(tempList.Count - 256).ToList();

                                newList.Clear();
                                newList.UnionWith(sortList);
                            }

                            bool updateFlag = false;
                            int count = sortList.Count;

                            lock (_messages.ThisLock)
                            {
                                if (!_messages.ContainsKey(selectTreeViewItem.Value.Channel))
                                {
                                    _messages[selectTreeViewItem.Value.Channel] = new List<Message>(sortList);
                                }
                                else if (!Collection.Equals(sortList, _messages[selectTreeViewItem.Value.Channel]))
                                {
                                    if (!newList.IsSubsetOf(_messages[selectTreeViewItem.Value.Channel]))
                                    {
                                        updateFlag = true;
                                    }

                                    _messages[selectTreeViewItem.Value.Channel].Clear();
                                    _messages[selectTreeViewItem.Value.Channel].AddRange(sortList);
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                selectTreeViewItem.Count = count;
                            }), null);

                            if (updateFlag)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                {
                                    selectTreeViewItem.Hit = true;
                                }), null);
                            }
                        }
                    }

                    Thread.Sleep(1000 * 60);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Filter()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(new TimeSpan(1, 0, 0));

                    List<BoardTreeViewItem> items = new List<BoardTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        var list = new List<TreeViewItem>();
                        list.Add(_treeViewItem);

                        for (int i = 0; i < list.Count; i++)
                        {
                            foreach (TreeViewItem item in list[i].Items)
                            {
                                list.Add(item);
                            }
                        }

                        foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                        {
                            items.Add(item);
                        }
                    }), null);

                    foreach (BoardTreeViewItem item in items)
                    {
                        if (item.Value.FilterUploadDigitalSignature != null)
                        {
                            lock (_messages.ThisLock)
                            {
                                if (_messages.ContainsKey(item.Value.Channel))
                                {
                                    List<Library.Net.Lair.Key> keys = new List<Library.Net.Lair.Key>();

                                    foreach (var m in _messages[item.Value.Channel])
                                    {
                                        keys.Add(new Library.Net.Lair.Key(m.GetHash(HashAlgorithm.Sha512), HashAlgorithm.Sha512));
                                    }

                                    _lairManager.Upload(new Filter(item.Value.Channel, keys, item.Value.FilterUploadDigitalSignature));
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

        void _lairManager_UnlockChannelsEvent(object sender, ref IList<Channel> unlockChannels)
        {
            HashSet<Channel> items = new HashSet<Channel>();

            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
            {
                var list = new List<TreeViewItem>();
                list.Add(_treeViewItem);

                for (int i = 0; i < list.Count; i++)
                {
                    foreach (TreeViewItem item in list[i].Items)
                    {
                        list.Add(item);
                    }
                }

                foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                {
                    items.Add(item.Value.Channel);
                }
            }), null);

            var channels = _lairManager.GetChannels();

            foreach (var item in channels.Where(n => !items.Contains(n)).OrderBy(n => _random.Next()))
            {
                unlockChannels.Add(item);
            }
        }

        void _lairManager_UnlockMessagesEvent(object sender, Channel channel, ref IList<Message> unlockMessages)
        {
            List<BoardTreeViewItem> items = new List<BoardTreeViewItem>();

            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
            {
                var list = new List<TreeViewItem>();
                list.Add(_treeViewItem);

                for (int i = 0; i < list.Count; i++)
                {
                    foreach (TreeViewItem item in list[i].Items)
                    {
                        list.Add(item);
                    }
                }

                foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                {
                    items.Add(item);
                }
            }), null);

            var selectTreeViewItem = items.FirstOrDefault(n => n.Value.Channel == channel);

            if (selectTreeViewItem == null)
            {
                IList<Message> messages;
                IList<Filter> filters;

                _lairManager.GetChannelInfomation(channel, out messages, out filters);

                foreach (var item in messages)
                {
                    unlockMessages.Add(item);
                }
            }
            else
            {
                DateTime now = DateTime.UtcNow;

                lock (selectTreeViewItem.Value.ThisLock)
                {
                    foreach (var lm in selectTreeViewItem.Value.LockMessages.ToArray())
                    {
                        if ((now - lm.CreationTime) > new TimeSpan(64, 0, 0, 0))
                            selectTreeViewItem.Value.LockMessages.Remove(lm);
                    }
                }
                
                HashSet<Message> newList = new HashSet<Message>();

                IList<Message> messages;
                IList<Filter> filters;

                _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                Filter filter = filters.FirstOrDefault(n => selectTreeViewItem.Value.Signature == MessageConverter.ToSignatureString(n.Certificate));

                if (filter != null)
                {
                    foreach (var message in messages)
                    {
                        if (filter.Keys.Any(n => message.VerifyHash(n.Hash, n.HashAlgorithm)))
                        {
                            newList.Add(message);
                        }
                    }
                }
                else
                {
                    foreach (var message in messages)
                    {
                        newList.Add(message);
                    }
                }

                List<CategoryTreeViewItem> categoryTreeViewItems = new List<CategoryTreeViewItem>();

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                {
                    categoryTreeViewItems.AddRange(_treeViewItem.GetLineage(selectTreeViewItem).OfType<CategoryTreeViewItem>());
                }), null);

                foreach (var item in categoryTreeViewItems)
                {
                    ChannelControl.Filter(ref newList, item.Value);
                }

                {
                    List<Message> list1 = new List<Message>();

                    foreach (var item in messages)
                    {
                        if (selectTreeViewItem.Value.LockMessages.Contains(item)) continue;
                        if (newList.Contains(item)) continue;

                        list1.Add(item);
                    }

                    list1 = list1.OrderBy(n => _random.Next()).ToList();

                    List<Message> list2 = new List<Message>();

                    foreach (var item in newList)
                    {
                        if (selectTreeViewItem.Value.LockMessages.Contains(item)) continue;

                        list2.Add(item);
                    }

                    list2.Sort(new Comparison<Message>((Message x, Message y) =>
                    {
                        return x.CreationTime.CompareTo(y.CreationTime);
                    }));

                    foreach (var item in list1)
                    {
                        unlockMessages.Add(item);
                    }

                    foreach (var item in list2)
                    {
                        unlockMessages.Add(item);
                    }
                }
            }
        }

        void _lairManager_UnlockFiltersEvent(object sender, Channel channel, ref IList<Filter> unlockFilters)
        {
            HashSet<string> items = new HashSet<string>();

            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
            {
                var list = new List<TreeViewItem>();
                list.Add(_treeViewItem);

                for (int i = 0; i < list.Count; i++)
                {
                    foreach (TreeViewItem item in list[i].Items)
                    {
                        list.Add(item);
                    }
                }

                foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                {
                    items.Add(item.Value.Signature);
                }
            }), null);

            IList<Message> messages;
            IList<Filter> filters;

            _lairManager.GetChannelInfomation(channel, out messages, out filters);

            foreach (var item in filters.Where(n =>
               {
                   var s = MessageConverter.ToSignatureString(n.Certificate);

                   return !items.Contains(s);
               }).OrderBy(n => _random.Next()))
            {
                unlockFilters.Add(item);
            }
        }

        void RichTextBoxHelper_ChannelClickEvent(object sender, Channel channel)
        {
            var selectBoardTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectBoardTreeViewItem == null) return;

            var list2 = _treeViewItem.GetLineage(selectBoardTreeViewItem).OfType<TreeViewItem>().ToList();
            var selectCategoryTreeViewItem = ((CategoryTreeViewItem)list2[list2.Count - 2]) as CategoryTreeViewItem;

            {
                if (channel.Name == null || channel.Id == null) return;
                if (selectCategoryTreeViewItem.Value.Boards.Any(n => n.Channel == channel)) return;

                selectCategoryTreeViewItem.Value.Boards.Add(new Board() { Channel = channel });
            }

            selectCategoryTreeViewItem.Update();

            this.Update();
        }

        void RichTextBoxHelper_SeedClickEvent(object sender, Library.Net.Amoeba.Seed seed)
        {
            if (string.IsNullOrWhiteSpace(Settings.Instance.Global_Amoeba_Path))
            {
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        var path = p.MainModule.FileName;

                        if (Path.GetFileName(path) == "Amoeba.exe")
                        {
                            Settings.Instance.Global_Amoeba_Path = path;

                            break;
                        }
                    }
                    catch (Win32Exception)
                    {

                    }
                    catch (Exception)
                    {

                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Settings.Instance.Global_Amoeba_Path))
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = Settings.Instance.Global_Amoeba_Path;
                    process.StartInfo.Arguments = string.Format("Download {0}", Library.Net.Amoeba.AmoebaConverter.ToSeedString(seed));
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(Settings.Instance.Global_Amoeba_Path);
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                }
                catch (Exception)
                {

                }
            }
        }

        void RichTextBoxHelper_LinkClickEvent(object sender, string link)
        {
            Process.Start(link);
        }

        double RichTextBoxHelper_GetMaxHeightEvent(object sender)
        {
            return _listView.ActualHeight - 18;
        }

        private static void Filter(ref HashSet<Message> messages, Category category)
        {
            lock (category.ThisLock)
            {
                DateTime now = DateTime.UtcNow;

                lock (category.SearchMessageCollection.ThisLock)
                {
                    foreach (var m in category.SearchMessageCollection.ToArray())
                    {
                        if ((now - m.Value.CreationTime) > new TimeSpan(64, 0, 0, 0))
                            category.SearchMessageCollection.Remove(m);
                    }
                }

                messages.IntersectWith(messages.ToArray().Where(searchItem =>
                {
                    bool flag;

                    lock (category.SearchWordCollection.ThisLock)
                    {
                        if (category.SearchWordCollection.Any(n => n.Contains == true))
                        {
                            flag = category.SearchWordCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchItem.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (!flag) return false;
                        }
                    }

                    lock (category.SearchSignatureCollection.ThisLock)
                    {
                        if (category.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            flag = category.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains)
                                {
                                    if (searchContains.Value == "Anonymous")
                                    {
                                        return searchItem.Certificate == null;
                                    }
                                    else
                                    {
                                        return MessageConverter.ToSignatureString(searchItem.Certificate) == searchContains.Value;
                                    }
                                }

                                return false;
                            });
                            if (!flag) return false;
                        }
                    }

                    lock (category.SearchMessageCollection.ThisLock)
                    {
                        if (category.SearchMessageCollection.Any(n => n.Contains == true))
                        {
                            flag = category.SearchMessageCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchItem == searchContains.Value;

                                return false;
                            });
                            if (!flag) return false;
                        }
                    }

                    lock (category.SearchRegexCollection.ThisLock)
                    {
                        if (category.SearchRegexCollection.Any(n => n.Contains == true))
                        {
                            flag = category.SearchRegexCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Content);

                                return false;
                            });
                            if (!flag) return false;
                        }
                    }

                    return true;
                }));

                messages.ExceptWith(messages.ToArray().Where(searchItem =>
                {
                    bool flag;

                    lock (category.SearchWordCollection.ThisLock)
                    {
                        if (category.SearchWordCollection.Any(n => n.Contains == false))
                        {
                            flag = category.SearchWordCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchItem.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchSignatureCollection.ThisLock)
                    {
                        if (category.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            flag = category.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains)
                                {
                                    if (searchContains.Value == "Anonymous")
                                    {
                                        return searchItem.Certificate == null;
                                    }
                                    else
                                    {
                                        return MessageConverter.ToSignatureString(searchItem.Certificate) == searchContains.Value;
                                    }
                                }

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchMessageCollection.ThisLock)
                    {
                        if (category.SearchMessageCollection.Any(n => n.Contains == false))
                        {
                            flag = category.SearchMessageCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchItem == searchContains.Value;

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchRegexCollection.ThisLock)
                    {
                        if (category.SearchRegexCollection.Any(n => n.Contains == false))
                        {
                            flag = category.SearchRegexCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return false;
                }));
            }
        }

        private void Update(bool scroll = false)
        {
            Settings.Instance.ChannelControl_Category = _treeViewItem.Value;

            if (scroll) _treeView_SelectedItemChanged(this, null);
            else _treeView_SelectedItemChanged(null, null);

            _treeViewItem.Sort();
        }

        public void Refresh()
        {
            _listView.Items.Refresh();
        }

        #region _treeView

        private Point _startPoint = new Point(-1, -1);

        private void _treeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Released)
            {
                if (_startPoint.X == -1 && _startPoint.Y == -1) return;

                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance
                    || Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (_treeViewItem == _treeView.SelectedItem) return;

                    DataObject data = new DataObject("item", _treeView.SelectedItem);
                    DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("item"))
            {
                dynamic s = e.Data.GetData("item");
                var t = _treeView.GetCurrentItem(e.GetPosition) as CategoryTreeViewItem;
                if (t == null || t.Equals(s)
                    || t.Value.Categories.Any(n => object.ReferenceEquals(n, s.Value))
                    || t.Value.Boards.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_treeViewItem.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                var list = _treeViewItem.GetLineage((TreeViewItem)s).OfType<TreeViewItem>().ToList();

                t.IsSelected = true;

                if (s.Value is Category)
                {
                    var tItems = ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.Clear();
                    ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.AddRange(tItems);
                }
                else if (s.Value is Board)
                {
                    var tItems = ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Clear();
                    ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.AddRange(tItems);
                }

                if (s.Value is Category) t.Value.Categories.Add(s.Value);
                else if (s.Value is Board && !t.Value.Boards.Any(n => n.Channel == s.Value.Channel)) t.Value.Boards.Add(s.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();
                t.Update();

                this.Update();
            }
        }

        private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            _startPoint = e.GetPosition(null);

            if (item.IsSelected == true)
                _treeView_SelectedItemChanged(null, null);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
            _scroll = (sender != null);
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                _treeViewNewChannelMenuItem.IsEnabled = true;
                _treeViewNewCategoryMenuItem.IsEnabled = true;
                _treeViewEditMenuItem.IsEnabled = true;
                _treeViewDeleteMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
                _treeViewCutMenuItem.IsEnabled = !(selectTreeViewItem == _treeViewItem);
                _treeViewCopyMenuItem.IsEnabled = true;

                {
                    var categories = Clipboard.GetCategories();
                    var channels = Clipboard.GetChannels();

                    _treeViewPasteMenuItem.IsEnabled = (categories.Count() + channels.Count()) > 0 ? true : false;
                }
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                _treeViewNewChannelMenuItem.IsEnabled = false;
                _treeViewNewCategoryMenuItem.IsEnabled = false;
                _treeViewEditMenuItem.IsEnabled = false;
                _treeViewDeleteMenuItem.IsEnabled = true;
                _treeViewCutMenuItem.IsEnabled = true;
                _treeViewCopyMenuItem.IsEnabled = true;
                _treeViewPasteMenuItem.IsEnabled = false;
            }
        }

        private void _treeViewNewChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            NewChannelWindow window = new NewChannelWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Boards.Add(new Board() { Channel = window.Channel });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            Category category = new Category();

            CategoryEditWindow window = new CategoryEditWindow(ref category);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Categories.Add(category);

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            Category category = selectTreeViewItem.Value;

            CategoryEditWindow window = new CategoryEditWindow(ref category);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Channel", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.Remove(selectTreeViewItem.Value);
                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Channel", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Remove(selectTreeViewItem.Value);
                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
        }

        private void _treeViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                Clipboard.SetCategories(new Category[] { selectTreeViewItem.Value });

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.Remove(selectTreeViewItem.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                Clipboard.SetChannels(new Channel[] { selectTreeViewItem.Value.Channel });

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Remove(selectTreeViewItem.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
        }

        private void _treeViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                Clipboard.SetCategories(new Category[] { selectTreeViewItem.Value });
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                Clipboard.SetChannels(new Channel[] { selectTreeViewItem.Value.Channel });
            }
        }

        private void _treeViewCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                List<Channel> channels = new List<Channel>();
                List<Category> categoryList = new List<Category>();

                categoryList.Add(selectTreeViewItem.Value);

                for (int i = 0; i < categoryList.Count; i++)
                {
                    categoryList.AddRange(categoryList[i].Categories);

                    var tempList = categoryList[i].Boards.Select(n => n.Channel).ToList();

                    tempList.Sort(delegate(Channel x, Channel y)
                    {
                        int c = x.Name.CompareTo(y.Name);
                        if (c != 0) return c;

                        return Collection.Compare(x.Id, y.Id);
                    });

                    channels.AddRange(tempList);
                }

                var sb = new StringBuilder();

                foreach (var channel in channels)
                {
                    sb.AppendLine(LairConverter.ToChannelString(channel));
                    sb.AppendLine(MessageConverter.ToInfoMessage(channel));
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                var sb = new StringBuilder();

                {
                    sb.AppendLine(LairConverter.ToChannelString(selectTreeViewItem.Value.Channel));
                    sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Channel));
                    sb.AppendLine();
                }

                Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
            }
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.Categories.AddRange(Clipboard.GetCategories());

            foreach (var channel in Clipboard.GetChannels())
            {
                if (channel.Name == null || channel.Id == null) continue;
                if (selectTreeViewItem.Value.Boards.Any(n => n.Channel == channel)) continue;

                selectTreeViewItem.Value.Boards.Add(new Board() { Channel = channel });
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        #endregion

        #region _listView

        private void _listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
            var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

            _gridViewColumn.Width = _listView.ActualWidth - 21;

            _listView.Items.Refresh();
        }

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            var list = new List<MenuItem>();

            list.AddRange(richTextBox.ContextMenu.Items.OfType<MenuItem>());

            for (int i = 0; i < list.Count; i++)
            {
                list.AddRange(list[i].Items.OfType<MenuItem>());
            }

            foreach (var item in list)
            {
                if (item.Name == "_richTextBoxFilterWordMenuItem")
                {
                    item.IsEnabled = !richTextBox.Selection.IsEmpty;
                }
                else if (item.Name == "_richTextBoxLockThisMenuItem")
                {
                    item.IsEnabled = !((MessageEx)_listView.SelectedItem).IsLock;
                }
                else if (item.Name == "_richTextBoxUnlockThisMenuItem")
                {
                    item.IsEnabled = ((MessageEx)_listView.SelectedItem).IsLock;
                }
            }
        }

        private void _listView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (e.ChangedButton == MouseButton.Middle)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var mx = _listViewItemCollection[index];
                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                var item = new SearchContains<Message>()
                {
                    Contains = false,
                    Value = mx.Value,
                };

                if (((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchMessageCollection.Contains(item)) return;
                ((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchMessageCollection.Add(item);

                this.Update();
            }

            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && e.ChangedButton == MouseButton.Left)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var mx = _listViewItemCollection[index];
                mx.IsLock = !mx.IsLock;
            }
        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var selectItem = _listView.SelectedItem as MessageEx;
            if (selectItem == null) return;

            string text = richTextBox.Selection.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                text = RichTextBoxHelper.GetMessageToString(selectItem.Value);
            }

            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            builder.AppendLine();

            foreach (var line in text.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
            {
                builder.AppendLine("> " + line);
            }

            richTextBox.Selection.Select(richTextBox.Document.ContentStart, richTextBox.Document.ContentStart);

            MessageEditWindow window = new MessageEditWindow(selectTreeViewItem.Value, builder.ToString(), _lairManager);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                this.Update();
            }
        }

        private void _richTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            var selectItem = _listView.SelectedItem as MessageEx;
            if (selectItem == null) return;

            string text = richTextBox.Selection.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                text = RichTextBoxHelper.GetMessageToString(selectItem.Value);
            }

            Clipboard.SetText(text);
        }

        private void _richTextBoxLockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var item = (MessageEx)_listView.SelectedItem;
            item.IsLock = true;
        }

        private void _richTextBoxUnlockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var item = (MessageEx)_listView.SelectedItem;
            item.IsLock = false;
        }

        private void _richTextBoxLockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.LockMessages.Clear();

            foreach (var item in _listViewItemCollection)
            {
                item.IsLock = true;
            }
        }

        private void _richTextBoxUnlockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.LockMessages.Clear();

            foreach (var item in _listViewItemCollection)
            {
                item.IsLock = false;
            }
        }

        private void _richTextBoxFilterWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            if (richTextBox.Selection.IsEmpty) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

            var text = richTextBox.Selection.Text.Trim('\r', '\n');
            if (text.Length == 0) return;

            if (text == text.Replace("\r", "").Replace("\n", ""))
            {
                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = text,
                };

                if (((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchWordCollection.Contains(item)) return;
                ((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchWordCollection.Add(item);
            }
            else
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = false,
                    Value = new SearchRegex() { IsIgnoreCase = true, Value = Regex.Escape(text) },
                };

                if (((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchRegexCollection.Contains(item)) return;
                ((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchRegexCollection.Add(item);
            }

            this.Update();
        }

        private void _richTextBoxFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as MessageEx;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();
            var signature = MessageConverter.ToSignatureString(message.Value.Certificate);
            if (signature == null) signature = "Anonymous";

            var item = new SearchContains<string>()
            {
                Contains = false,
                Value = signature,
            };

            if (((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchSignatureCollection.Contains(item)) return;
            ((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchSignatureCollection.Add(item);

            this.Update();
        }

        private void _richTextBoxFilterMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as MessageEx;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

            var item = new SearchContains<Message>()
            {
                Contains = false,
                Value = message.Value,
            };

            if (((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchMessageCollection.Contains(item)) return;
            ((CategoryTreeViewItem)list[list.Count - 2]).Value.SearchMessageCollection.Add(item);

            this.Update();
        }

        #endregion

        private void _signatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_isEditing || selectTreeViewItem.Value.Signature == _signatureComboBox.Text) return;

            selectTreeViewItem.Value.Signature = _signatureComboBox.Text;

            this.Update();
        }

        private void _signatureComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (_isEditing || selectTreeViewItem.Value.Signature == _signatureComboBox.Text) return;

            selectTreeViewItem.Value.Signature = _signatureComboBox.Text;

            this.Update();
        }

        private void _signButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var board = selectTreeViewItem.Value;

            SignWindow window = new SignWindow(ref board);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                this.Update();
            }
        }

        private void _newMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            MessageEditWindow window = new MessageEditWindow(selectTreeViewItem.Value, "", _lairManager);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                this.Update(true);
            }
        }

        private void _serachCloseButton_Click(object sender, RoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(0);
            _searchTextBox.Text = "";

            this.Update();
        }

        private void _searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Update();
            }
        }

        #region Sort

        private void Sort()
        {
            List<MessageEx> list = new List<MessageEx>(_listViewItemCollection);

            list.Sort(delegate(MessageEx x, MessageEx y)
            {
                int c = x.Value.CreationTime.CompareTo(y.Value.CreationTime);
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

        private void Execute_New(object sender, ExecutedRoutedEventArgs e)
        {
            _newMessageButton_Click(null, null);
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewDeleteMenuItem_Click(null, null);
            }
            else
            {

            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewCopyMenuItem_Click(null, null);
            }
            else
            {

            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                _treeViewCutMenuItem_Click(null, null);
            }
            else
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            _treeViewPasteMenuItem_Click(null, null);
        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }

    class CategoryTreeViewItem : TreeViewItem
    {
        private Category _value;
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();

        public CategoryTreeViewItem()
            : base()
        {
            this.Value = new Category() { Name = "" };

            base.IsExpanded = true;
            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public CategoryTreeViewItem(Category category)
            : base()
        {
            this.Value = category;

            base.IsExpanded = true;
            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            this.Header = string.Format("{0}", _value.Name);

            List<dynamic> list = new List<dynamic>();

            foreach (var item in this.Value.Categories)
            {
                list.Add(new CategoryTreeViewItem(item));
            }

            foreach (var item in this.Value.Boards)
            {
                list.Add(new BoardTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.Cast<dynamic>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.Cast<dynamic>().Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<object>().ToList();

            list.Sort(delegate(object x, object y)
            {
                if (x is BoardTreeViewItem)
                {
                    if (y is BoardTreeViewItem)
                    {
                        var bx = ((BoardTreeViewItem)x).Value;
                        var by = ((BoardTreeViewItem)y).Value;

                        var xsignature = (bx.Signature == null) ? "" : bx.Signature;
                        var ysignature = (by.Signature == null) ? "" : by.Signature;

                        int c = bx.Channel.Name.CompareTo(by.Channel.Name);
                        if (c != 0) return c;
                        c = Collection.Compare(bx.Channel.Id, by.Channel.Id);
                        if (c != 0) return c;
                        c = xsignature.CompareTo(ysignature);
                        if (c != 0) return c;

                        return bx.GetHashCode().CompareTo(by.GetHashCode());
                    }
                    else if (y is CategoryTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is CategoryTreeViewItem)
                {
                    if (y is CategoryTreeViewItem)
                    {
                        var cx = ((CategoryTreeViewItem)x).Value;
                        var cy = ((CategoryTreeViewItem)y).Value;

                        int c = cx.Name.CompareTo(cy.Name);
                        if (c != 0) return c;

                        return cx.GetHashCode().CompareTo(cy.GetHashCode());
                    }
                    else if (y is BoardTreeViewItem)
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

            foreach (var item in this.Items.OfType<CategoryTreeViewItem>())
            {
                item.Sort();
            }
        }

        public Category Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }
    }

    class BoardTreeViewItem : TreeViewItem
    {
        private Board _value;
        private bool _hit;
        private int _count;

        public BoardTreeViewItem(Board board)
            : base()
        {
            this.Value = board;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            string text = "";

            if (_value.FilterUploadDigitalSignature == null)
            {
                text = string.Format("{0} ({1})", _value.Channel.Name, _count);
            }
            else
            {
                text = string.Format("{0} ({1}) - {2}", _value.Channel.Name, _count, MessageConverter.ToSignatureString(_value.FilterUploadDigitalSignature));
            }

            if (_hit)
            {
                this.Header = new TextBlock() { Text = text, FontWeight = FontWeights.ExtraBlack };
            }
            else
            {
                this.Header = new TextBlock() { Text = text };
            }
        }

        public Board Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                this.Update();
            }
        }

        public bool Hit
        {
            get
            {
                return _hit;
            }
            set
            {
                _hit = value;

                this.Update();
            }
        }

        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;

                this.Update();
            }
        }
    }

    class MessageEx : INotifyPropertyChanged, IEquatable<MessageEx>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private Board _board;
        private Message _value;

        public MessageEx(Board board, Message value)
        {
            _board = board;
            _value = value;
        }

        public override int GetHashCode()
        {
            if (_value == null) return 0;
            else return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MessageEx)) return false;

            return this.Equals((MessageEx)obj);
        }

        public bool Equals(MessageEx other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.IsLock != other.IsLock
                || this.Value != other.Value)
            {
                return false;
            }

            return true;
        }

        public bool IsLock
        {
            get
            {
                return _board.LockMessages.Contains(_value);
            }
            set
            {
                if (value)
                {
                    _board.LockMessages.Add(_value);
                }
                else
                {
                    _board.LockMessages.Remove(_value);
                }

                this.NotifyPropertyChanged("IsLock");
            }
        }

        public Message Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (value != _value)
                {
                    _value = value;

                    this.NotifyPropertyChanged("Value");
                }
            }
        }
    }

    [DataContract(Name = "Category", Namespace = "http://Lair/Windows")]
    class Category : IDeepCloneable<Category>, IThisLock
    {
        private string _name;
        private LockedList<Board> _boards;
        private LockedList<Category> _categories;
        private LockedList<SearchContains<string>> _searchWordCollection;
        private LockedList<SearchContains<SearchRegex>> _searchRegexCollection;
        private LockedList<SearchContains<string>> _searchSignatureCollection;
        private LockedList<SearchContains<Message>> _searchMessageCollection;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "Boards")]
        public LockedList<Board> Boards
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_boards == null)
                        _boards = new LockedList<Board>();

                    return _boards;
                }
            }
        }

        [DataMember(Name = "Categories")]
        public LockedList<Category> Categories
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_categories == null)
                        _categories = new LockedList<Category>();

                    return _categories;
                }
            }
        }

        [DataMember(Name = "SearchWordCollection")]
        public LockedList<SearchContains<string>> SearchWordCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchWordCollection == null)
                        _searchWordCollection = new LockedList<SearchContains<string>>();

                    return _searchWordCollection;
                }
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public LockedList<SearchContains<SearchRegex>> SearchRegexCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchRegexCollection == null)
                        _searchRegexCollection = new LockedList<SearchContains<SearchRegex>>();

                    return _searchRegexCollection;
                }
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public LockedList<SearchContains<string>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new LockedList<SearchContains<string>>();

                    return _searchSignatureCollection;
                }
            }
        }

        [DataMember(Name = "SearchMessageCollection")]
        public LockedList<SearchContains<Message>> SearchMessageCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchMessageCollection == null)
                        _searchMessageCollection = new LockedList<SearchContains<Message>>();

                    return _searchMessageCollection;
                }
            }
        }

        #region IDeepClone<Category>

        public Category DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(Category));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (Category)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "Board", Namespace = "http://Lair/Windows")]
    class Board : IDeepCloneable<Board>, IThisLock
    {
        private Channel _channel;
        private string _signature;
        private DigitalSignature _filterUploadDigitalSignature;
        private LockedHashSet<Message> _lockMessages;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Channel")]
        public Channel Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _channel;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _channel = value;
                }
            }
        }

        [DataMember(Name = "Signature")]
        public string Signature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _signature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _signature = value;
                }
            }
        }

        [DataMember(Name = "FilterUploadDigitalSignature")]
        public DigitalSignature FilterUploadDigitalSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _filterUploadDigitalSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _filterUploadDigitalSignature = value;
                }
            }
        }

        [DataMember(Name = "LockMessages")]
        public LockedHashSet<Message> LockMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_lockMessages == null)
                        _lockMessages = new LockedHashSet<Message>();

                    return _lockMessages;
                }
            }
        }

        #region IDeepClone<Board>

        public Board DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(Category));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (Board)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchContains", Namespace = "http://Lair/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>, IThisLock
    {
        public bool _contains;
        public T _value;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Contains")]
        public bool Contains
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _contains;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _contains = value;
                }
            }
        }

        [DataMember(Name = "Value")]
        public T Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _value;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _value = value;
                }
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                if (this.Value == null) return 0;
                else return this.Value.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchContains<T>)) return false;

            return this.Equals((SearchContains<T>)obj);
        }

        public bool Equals(SearchContains<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Contains != other.Contains
                || !this.Value.Equals(other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("{0} {1}", this.Contains, this.Value);
            }
        }

        #region IDeepClone<SearchContains<T>>

        public SearchContains<T> DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchContains<T>));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchContains<T>)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Lair/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>, IThisLock
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Value")]
        public string Value
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _value;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _value = value;

                    this.RegexUpdate();
                }
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isIgnoreCase;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isIgnoreCase = value;

                    this.RegexUpdate();
                }
            }
        }

        private void RegexUpdate()
        {
            lock (this.ThisLock)
            {
                var o = RegexOptions.Compiled | RegexOptions.Singleline;
                if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

                if (_value != null) _regex = new Regex(_value, o);
                else _regex = null;
            }
        }

        public bool IsMatch(string value)
        {
            lock (this.ThisLock)
            {
                if (_regex == null) return false;

                return _regex.IsMatch(value);
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Value.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRegex)) return false;

            return this.Equals((SearchRegex)obj);
        }

        public bool Equals(SearchRegex other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((this.IsIgnoreCase != other.IsIgnoreCase)
                || (this.Value != other.Value))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("{0} {1}", this.IsIgnoreCase, this.Value);
            }
        }

        #region IDeepClone<SearchRegex>

        public SearchRegex DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchRegex));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchRegex)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
