﻿using System;
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
using a = Library.Net.Amoeba;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for ControlControl.xaml
    /// </summary>
    partial class ControlControl : UserControl
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
        private LockedDictionary<Board, List<Message>> _messages = new LockedDictionary<Board, List<Message>>(new ReferenceEqualityComparer());

        private static Random _random = new Random();

        public ControlControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.ControlControl_Category;
            _treeViewItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(_treeViewItem_PreviewMouseLeftButtonDown);

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
            _searchThread.Name = "ControlControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "ControlControl_CacheThread";
            _cacheThread.Start();

            _filterThread = new Thread(new ThreadStart(this.Filter));
            _filterThread.Priority = ThreadPriority.Highest;
            _filterThread.IsBackground = true;
            _filterThread.Name = "ControlControl_FilterThread";
            _filterThread.Start();

            _lairManager.UnlockChannelsEvent += new UnlockChannelsEventHandler(_lairManager_UnlockChannelsEvent);
            _lairManager.UnlockMessagesEvent += new UnlockMessagesEventHandler(_lairManager_UnlockMessagesEvent);
            _lairManager.UnlockFiltersEvent += new UnlockFiltersEventHandler(_lairManager_UnlockFiltersEvent);

            RichTextBoxHelper.ChannelClickEvent += new ChannelClickEventHandler(RichTextBoxHelper_ChannelClickEvent);
            RichTextBoxHelper.SeedClickEvent += new SeedClickEventHandler(RichTextBoxHelper_SeedClickEvent);
            RichTextBoxHelper.LinkClickEvent += new LinkClickEventHandler(RichTextBoxHelper_LinkClickEvent);
            RichTextBoxHelper.GetMaxHeightEvent += new GetMaxHeightEventHandler(RichTextBoxHelper_GetMaxHeightEvent);

            _searchRowDefinition.Height = new GridLength(0);

            {
                var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);

                view.Filter = delegate(object o)
                {
                    var item = o as MessageEx;
                    if (o == null) return false;

                    if (_onlyUnreadButton.IsChecked.Value)
                    {
                        if (!item.State.HasFlag(MessageState.IsNew)) return false;
                    }

                    return true;
                };
            }
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
                            _onlyUnreadButton.IsEnabled = false;

                            if (App.SelectTab == "Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectitem.Value.Name);
                        }
                        else if (_treeView.SelectedItem is BoardTreeViewItem)
                        {
                            selectTreeViewItem = (BoardTreeViewItem)_treeView.SelectedItem;
                            selectTreeViewItem.Hit = false;

                            _treeViewItem.UpdateColor();
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
                        _onlyUnreadButton.IsEnabled = true;
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
                        ControlControl.Filter(ref newList, item.Value);
                    }

                    ControlControl.Filter(ref newList, selectTreeViewItem.Value);

                    List<Message> sortList = new List<Message>();

                    {
                        var tempList = newList.ToList();

                        tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                        {
                            int c = x.CreationTime.CompareTo(y.CreationTime);
                            if (c != 0) return c;
                            c = x.Content.CompareTo(y.Content);
                            if (c != 0) return c;
                            c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                            if (c != 0) return c;

                            return x.GetHashCode().CompareTo(y.GetHashCode());
                        }));

                        tempList = tempList.Skip(tempList.Count - 1024).ToList();

                        newList.Clear();
                        newList.UnionWith(tempList);

                        foreach (var message in selectTreeViewItem.Value.LockMessages)
                        {
                            if (newList.Contains(message)) continue;

                            tempList.Add(message);
                        }

                        tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                        {
                            int c = x.CreationTime.CompareTo(y.CreationTime);
                            if (c != 0) return c;
                            c = x.Content.CompareTo(y.Content);
                            if (c != 0) return c;
                            c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                            if (c != 0) return c;

                            return x.GetHashCode().CompareTo(y.GetHashCode());
                        }));

                        sortList = tempList.ToList();

                        newList.Clear();
                        newList.UnionWith(sortList);
                    }

                    lock (_messages.ThisLock)
                    {
                        if (!_messages.ContainsKey(selectTreeViewItem.Value))
                        {
                            _messages[selectTreeViewItem.Value] = new List<Message>();
                        }

                        if (!Collection.Equals(sortList, _messages[selectTreeViewItem.Value]))
                        {
                            _messages[selectTreeViewItem.Value].Clear();
                            _messages[selectTreeViewItem.Value].AddRange(sortList);
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

                    int layoutUpdated = 0;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (selectTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        if (removeList.Count > 100)
                        {
                            _listViewItemCollection.Clear();

                            foreach (var item in sortList)
                            {
                                _listViewItemCollection.Add(new MessageEx(selectTreeViewItem.Value, item));
                            }
                        }
                        else
                        {
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

                        lock (selectTreeViewItem.Value.ThisLock)
                        {
                            foreach (var item in _listViewItemCollection)
                            {
                                if (item.State.HasFlag(MessageState.IsNew))
                                {
                                    if (selectTreeViewItem.Value.OldMessages.Contains(item.Value)) item.State &= ~MessageState.IsNew;
                                }
                                else
                                {
                                    if (!selectTreeViewItem.Value.OldMessages.Contains(item.Value)) item.State |= MessageState.IsNew;
                                }
                            }

                            selectTreeViewItem.Value.OldMessages.Clear();
                            selectTreeViewItem.Value.OldMessages.UnionWith(sortList);
                        }

                        selectTreeViewItem.Count = _listViewItemCollection.Count;

                        {
                            string searchText = _searchTextBox.Text;

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var item in _listViewItemCollection.ToArray())
                                {
                                    var text = RichTextBoxHelper.GetMessageToShowString(item.Value).ToLower();

                                    foreach (var word in words)
                                    {
                                        if (!text.Contains(word))
                                        {
                                            _listViewItemCollection.Remove(item);

                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        this.Sort();

                        var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                        view.Refresh();

                        if (_scroll)
                        {
                            if (_listViewItemCollection.Count > 0)
                            {
                                _listView.GoBottom();

                                _listView.UpdateLayout();
                                var topItem = _listViewItemCollection.FirstOrDefault(n => n.State.HasFlag(MessageState.IsNew));
                                if (topItem == null) topItem = _listViewItemCollection.LastOrDefault();
                                if (topItem != null) _listView.ScrollIntoView(topItem);
                            }
                        }

                        layoutUpdated = _layoutUpdated;

                        if (App.SelectTab == "Channel")
                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));
                    }), null);

                    while (_layoutUpdated == layoutUpdated) Thread.Sleep(100);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        _listView.UpdateLayout();
                        var topItem = _listViewItemCollection.FirstOrDefault(n => n.State.HasFlag(MessageState.IsNew));
                        if (topItem == null) topItem = _listViewItemCollection.LastOrDefault();
                        if (topItem != null) _listView.ScrollIntoView(topItem);
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
                                ControlControl.Filter(ref newList, item.Value);
                            }

                            ControlControl.Filter(ref newList, selectTreeViewItem.Value);

                            List<Message> sortList = new List<Message>();

                            {
                                var tempList = newList.ToList();

                                tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                                {
                                    int c = x.CreationTime.CompareTo(y.CreationTime);
                                    if (c != 0) return c;
                                    c = x.Content.CompareTo(y.Content);
                                    if (c != 0) return c;
                                    c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                                    if (c != 0) return c;

                                    return x.GetHashCode().CompareTo(y.GetHashCode());
                                }));

                                tempList = tempList.Skip(tempList.Count - 1024).ToList();

                                newList.Clear();
                                newList.UnionWith(tempList);

                                foreach (var message in selectTreeViewItem.Value.LockMessages)
                                {
                                    if (newList.Contains(message)) continue;

                                    tempList.Add(message);
                                }

                                tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                                {
                                    int c = x.CreationTime.CompareTo(y.CreationTime);
                                    if (c != 0) return c;
                                    c = x.Content.CompareTo(y.Content);
                                    if (c != 0) return c;
                                    c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                                    if (c != 0) return c;

                                    return x.GetHashCode().CompareTo(y.GetHashCode());
                                }));

                                sortList = tempList.ToList();

                                newList.Clear();
                                newList.UnionWith(sortList);
                            }

                            bool updateFlag = false;

                            lock (_messages.ThisLock)
                            {
                                List<Message> mlist = null;

                                if (!_messages.TryGetValue(selectTreeViewItem.Value, out mlist))
                                {
                                    mlist = new List<Message>();

                                    _messages[selectTreeViewItem.Value] = mlist;
                                }

                                if (!Collection.Equals(sortList, mlist))
                                {
                                    if (!newList.IsSubsetOf(selectTreeViewItem.Value.OldMessages))
                                    {
                                        updateFlag = true;
                                    }

                                    HashSet<Message> hmlist = new HashSet<Message>(mlist);
                                    var now = DateTime.UtcNow;

                                    foreach (var m in sortList)
                                    {
                                        if (!hmlist.Contains(m))
                                        {
                                            foreach (var text in m.Content.Split(new string[] { " ", "　", "\t", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                                            {
                                                if (!text.StartsWith("Seed@")) continue;

                                                var seed = a.AmoebaConverter.FromSeedString(text);
                                                if (seed == null) continue;

                                                if ((now - seed.CreationTime).TotalDays <= Settings.Instance.Global_SeedDelete_Expires)
                                                {
                                                    Settings.Instance.Global_Seeds.Add(seed);
                                                }
                                            }
                                        }
                                    }

                                    _messages[selectTreeViewItem.Value].Clear();
                                    _messages[selectTreeViewItem.Value].AddRange(sortList);
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                selectTreeViewItem.Count = sortList.Count;
                            }), null);

                            if (updateFlag)
                            {
                                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                                {
                                    selectTreeViewItem.Hit = true;

                                    _treeViewItem.UpdateColor();
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

                    var now = DateTime.UtcNow;
                    var limit = new TimeSpan(64, 0, 0, 0);

                    foreach (BoardTreeViewItem item in items)
                    {
                        if (item.Value.FilterUploadDigitalSignature != null)
                        {
                            lock (_messages.ThisLock)
                            {
                                if (_messages.ContainsKey(item.Value))
                                {
                                    List<Library.Net.Lair.Key> keys = new List<Library.Net.Lair.Key>();

                                    foreach (var m in _messages[item.Value])
                                    {
                                        if ((now - m.CreationTime) > limit) continue;

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

        class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            new public bool Equals(object x, object y)
            {
                if (x == null && y == null) return true;
                if ((x == null) != (y == null)) return false;

                return object.ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                if (obj == null) return 0;
                else return 1;
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

            foreach (var item in channels
                .Where(n => !items.Contains(n))
                .OrderBy(n => _random.Next()))
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
                    if (item.Value.Channel != channel) continue;

                    items.Add(item);
                }
            }), null);

            if (items.Count == 0)
            {
                IList<Message> messages;
                IList<Filter> filters;

                _lairManager.GetChannelInfomation(channel, out messages, out filters);

                foreach (var item in messages.OrderBy(n => _random.Next()))
                {
                    unlockMessages.Add(item);
                }
            }
            else if (items.Count == 1)
            {
                foreach (var m in this.GetUnlockMessages(items[0]))
                {
                    unlockMessages.Add(m);
                }
            }
            else
            {
                HashSet<Message> messages = new HashSet<Message>();

                messages.UnionWith(this.GetUnlockMessages(items[0]));

                foreach (var item in items.Skip(1))
                {
                    messages.IntersectWith(this.GetUnlockMessages(item));
                }
            }
        }

        private IEnumerable<Message> GetUnlockMessages(BoardTreeViewItem item)
        {
            HashSet<Message> lockMessages = new HashSet<Message>();

            {
                var tempList = item.Value.LockMessages.ToList();

                tempList.Sort(new Comparison<Message>((Message x, Message y) =>
                {
                    int c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Content.CompareTo(y.Content);
                    if (c != 0) return c;
                    c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                    if (c != 0) return c;

                    return x.GetHashCode().CompareTo(y.GetHashCode());
                }));

                lockMessages.UnionWith(tempList.Skip(tempList.Count - (1024 - 128)));
            }

            HashSet<Message> newList = new HashSet<Message>();

            IList<Message> messages;
            IList<Filter> filters;

            _lairManager.GetChannelInfomation(item.Value.Channel, out messages, out filters);

            foreach (var message in item.Value.LockMessages)
            {
                messages.Add(message);
            }

            Filter filter = filters.FirstOrDefault(n => item.Value.Signature == MessageConverter.ToSignatureString(n.Certificate));

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

            HashSet<Message> previewList = new HashSet<Message>();

            {
                HashSet<Message> tempNewList = new HashSet<Message>(newList);

                List<CategoryTreeViewItem> categoryTreeViewItems = new List<CategoryTreeViewItem>();

                this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                {
                    categoryTreeViewItems.AddRange(_treeViewItem.GetLineage(item).OfType<CategoryTreeViewItem>());
                }), null);

                foreach (var categoryTreeViewItem in categoryTreeViewItems)
                {
                    ControlControl.Filter(ref tempNewList, categoryTreeViewItem.Value);
                }

                ControlControl.Filter(ref tempNewList, item.Value);

                previewList.UnionWith(tempNewList);
            }

            {
                List<Message> list1 = new List<Message>();

                foreach (var m in messages)
                {
                    if (lockMessages.Contains(m)) continue;
                    if (previewList.Contains(m)) continue;

                    list1.Add(m);
                }

                list1 = list1
                    .OrderBy(n => _random.Next())
                    .ToList();

                List<Message> list2 = new List<Message>();

                foreach (var m in previewList)
                {
                    list2.Add(m);
                }

                list2.Sort(new Comparison<Message>((Message x, Message y) =>
                {
                    int c = x.CreationTime.CompareTo(y.CreationTime);
                    if (c != 0) return c;
                    c = x.Content.CompareTo(y.Content);
                    if (c != 0) return c;
                    c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                    if (c != 0) return c;

                    return x.GetHashCode().CompareTo(y.GetHashCode());
                }));

                List<Message> unlockMessages = new List<Message>();

                foreach (var m in list1)
                {
                    unlockMessages.Add(m);
                }

                foreach (var m in list2)
                {
                    unlockMessages.Add(m);
                }

                return unlockMessages.Take(unlockMessages.Count - 1024);
            }
        }

        void _lairManager_UnlockFiltersEvent(object sender, Channel channel, ref IList<Filter> unlockFilters)
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
                    if (item.Value.Channel != channel) continue;

                    items.Add(item);
                }
            }), null);

            IList<Message> messages;
            IList<Filter> filters;

            _lairManager.GetChannelInfomation(channel, out messages, out filters);

            var targetFilters = new HashSet<Filter>(filters
                .Where(n => items.Any(m => m.Value.Signature == MessageConverter.ToSignatureString(n.Certificate))));

            foreach (var item in filters.OrderBy(n => _random.Next()))
            {
                if (targetFilters.Contains(item)) continue;

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

            Settings.Instance.Global_ChannelHistorys.Add(channel);
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

            if (File.Exists(Settings.Instance.Global_Amoeba_Path))
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
                    return;
                }

                Settings.Instance.Global_SeedHistorys.Add(seed);
            }
            else
            {
                MessageBox.Show(LanguagesManager.Instance.ControlControl_AmoebaNotFound_Message);

                ViewSettingsWindow window = new ViewSettingsWindow(_bufferManager);
                window.Owner = _mainWindow;
                window.ShowDialog();
            }
        }

        void RichTextBoxHelper_LinkClickEvent(object sender, string link)
        {
            try
            {
                Process.Start(link);
            }
            catch (Exception)
            {
                return;
            }

            Settings.Instance.Global_UrlHistorys.Add(link);
        }

        double RichTextBoxHelper_GetMaxHeightEvent(object sender)
        {
            return _listView.ActualHeight - 18;
        }

        private static void Filter(ref HashSet<Message> messages, Category category)
        {
            lock (category.ThisLock)
            {
                messages.IntersectWith(messages.ToArray().Where(item =>
                {
                    bool flag = true;

                    lock (category.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (category.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                        {
                            flag = category.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchWordCollection.ThisLock)
                    {
                        if (category.SearchWordCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = category.SearchWordCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchSignatureCollection.ThisLock)
                    {
                        if (category.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = MessageConverter.ToSignatureString(item.Certificate);
                            }

                            flag = category.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(signatureText);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchRegexCollection.ThisLock)
                    {
                        if (category.SearchRegexCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = category.SearchRegexCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(item.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));

                messages.ExceptWith(messages.ToArray().Where(item =>
                {
                    if (item.Content.Contains('\uFFFD')) return true;

                    bool flag = false;

                    lock (category.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (category.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = category.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchWordCollection.ThisLock)
                    {
                        if (category.SearchWordCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = category.SearchWordCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchSignatureCollection.ThisLock)
                    {
                        if (category.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = MessageConverter.ToSignatureString(item.Certificate);
                            }

                            flag = category.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(signatureText);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (category.SearchRegexCollection.ThisLock)
                    {
                        if (category.SearchRegexCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = category.SearchRegexCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(item.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));
            }
        }

        private static void Filter(ref HashSet<Message> messages, Board board)
        {
            lock (board.ThisLock)
            {
                DateTime now = DateTime.UtcNow;

                lock (board.ThisLock)
                {
                    foreach (var m in board.SearchMessageCollection.ToArray())
                    {
                        if ((now - m.Value.CreationTime) > new TimeSpan(64, 0, 0, 0))
                            board.SearchMessageCollection.Remove(m);
                    }
                }

                messages.IntersectWith(messages.ToArray().Where(item =>
                {
                    bool flag = true;

                    lock (board.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (board.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                        {
                            flag = board.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchWordCollection.ThisLock)
                    {
                        if (board.SearchWordCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = board.SearchWordCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchSignatureCollection.ThisLock)
                    {
                        if (board.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = MessageConverter.ToSignatureString(item.Certificate);
                            }

                            flag = board.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(signatureText);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchMessageCollection.ThisLock)
                    {
                        if (board.SearchMessageCollection.Any(n => n.Contains == true))
                        {
                            flag = board.SearchMessageCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item == searchContains.Value;

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchRegexCollection.ThisLock)
                    {
                        if (board.SearchRegexCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = board.SearchRegexCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.IsMatch(item.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));

                messages.ExceptWith(messages.ToArray().Where(item =>
                {
                    if (item.Content.Contains('\uFFFD')) return true;

                    bool flag = false;

                    lock (board.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (board.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = board.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchWordCollection.ThisLock)
                    {
                        if (board.SearchWordCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = board.SearchWordCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchSignatureCollection.ThisLock)
                    {
                        if (board.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = MessageConverter.ToSignatureString(item.Certificate);
                            }

                            flag = board.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(signatureText);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchMessageCollection.ThisLock)
                    {
                        if (board.SearchMessageCollection.Any(n => n.Contains == false))
                        {
                            flag = board.SearchMessageCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item == searchContains.Value;

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (board.SearchRegexCollection.ThisLock)
                    {
                        if (board.SearchRegexCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = board.SearchRegexCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(item.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));
            }
        }

        private void Update(bool scroll = false)
        {
            Settings.Instance.ControlControl_Category = _treeViewItem.Value;

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
                else if (s.Value is Board && !t.Value.Boards.Any(n => n == s.Value)) t.Value.Boards.Add(s.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();
                t.Update();

                this.Update();
            }
        }

        private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void _treeViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null)
            {
                _startPoint = new Point(-1, -1);

                return;
            }

            if (item.IsSelected == true)
            {
                _startPoint = e.GetPosition(null);
                _treeView_SelectedItemChanged(null, null);
            }
        }

        private void _treeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = new Point(-1, -1);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
            _scroll = (sender != null);

            _treeViewItem.UpdateColor();
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
                    var boards = Clipboard.GetBoards();

                    _treeViewPasteMenuItem.IsEnabled = (categories.Count() + channels.Count() + boards.Count()) > 0 ? true : false;
                }

                _treeViewMarkAllMessagesReadMenuItem.IsEnabled = selectTreeViewItem.Hit;
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                _treeViewNewChannelMenuItem.IsEnabled = false;
                _treeViewNewCategoryMenuItem.IsEnabled = false;
                _treeViewEditMenuItem.IsEnabled = true;
                _treeViewDeleteMenuItem.IsEnabled = true;
                _treeViewCutMenuItem.IsEnabled = true;
                _treeViewCopyMenuItem.IsEnabled = true;
                _treeViewPasteMenuItem.IsEnabled = false;
                _treeViewMarkAllMessagesReadMenuItem.IsEnabled = false;
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
            if (_treeView.SelectedItem is CategoryTreeViewItem)
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
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                Board board = selectTreeViewItem.Value;

                BoardEditWindow window = new BoardEditWindow(ref board);
                window.Owner = _mainWindow;

                if (window.ShowDialog() == true)
                {
                    selectTreeViewItem.Update();
                }

                this.Update();
            }
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

                Clipboard.SetBoards(new Board[] { selectTreeViewItem.Value });

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

                Clipboard.SetBoards(new Board[] { selectTreeViewItem.Value });
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

            foreach (var board in Clipboard.GetBoards())
            {
                if (selectTreeViewItem.Value.Boards.Any(n => n == board)) continue;
                selectTreeViewItem.Value.Boards.Add(board);
            }

            foreach (var channel in Clipboard.GetChannels())
            {
                if (channel == null || channel.Name == null || channel.Id == null) continue;
                if (selectTreeViewItem.Value.Boards.Any(n => n.Channel == channel)) continue;

                selectTreeViewItem.Value.Boards.Add(new Board() { Channel = channel });
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _treeViewMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.ControlControl_MarkAllMessagesRead_Message, "Channel", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                List<Board> boards = new List<Board>();
                List<Category> categoryList = new List<Category>();

                categoryList.Add(selectTreeViewItem.Value);

                for (int i = 0; i < categoryList.Count; i++)
                {
                    categoryList.AddRange(categoryList[i].Categories);
                    boards.AddRange(categoryList[i].Boards);
                }

                lock (_messages.ThisLock)
                {
                    foreach (var board in boards)
                    {
                        lock (board.ThisLock)
                        {
                            board.OldMessages.Clear();
                            board.OldMessages.UnionWith(_messages[board]);
                        }
                    }
                }

                {
                    var list = new List<TreeViewItem>();
                    list.Add(selectTreeViewItem);

                    for (int i = 0; i < list.Count; i++)
                    {
                        foreach (TreeViewItem item in list[i].Items)
                        {
                            list.Add(item);
                        }
                    }

                    foreach (BoardTreeViewItem item in list.OfType<BoardTreeViewItem>())
                    {
                        item.Hit = false;
                    }

                    selectTreeViewItem.UpdateColor();
                }
            }
        }

        private void _treeViewImportLockedMessagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.RestoreDirectory = true;
                dialog.DefaultExt = ".messages";
                dialog.Filter = "Messages (*.messages)|*.messages";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filePath in dialog.FileNames)
                    {
                        try
                        {
                            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                            {
                                foreach (var message in LairConverter.FromMessagesStream(stream))
                                {
                                    if (message == null || message.Channel == null || message.Content == null) continue;
                                    if (message.Channel != selectTreeViewItem.Value.Channel) continue;

                                    selectTreeViewItem.Value.LockMessages.Add(message);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    selectTreeViewItem.Update();
                    this.Update();
                }
            }
        }

        private void _treeViewExportLockedMessagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                dialog.RestoreDirectory = true;
                dialog.FileName = selectTreeViewItem.Value.Channel.Name;
                dialog.DefaultExt = ".messages";
                dialog.Filter = "Messages (*.messages)|*.messages";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var fileName = dialog.FileName;

                    using (FileStream stream = new FileStream(fileName, FileMode.Create))
                    using (Stream directoryStream = LairConverter.ToMessagesStream(selectTreeViewItem.Value.LockMessages))
                    {
                        int i = -1;
                        byte[] buffer = _bufferManager.TakeBuffer(1024);

                        while ((i = directoryStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            stream.Write(buffer, 0, i);
                        }

                        _bufferManager.ReturnBuffer(buffer);
                    }
                }
            }
        }

        #endregion

        #region _listView

        private volatile int _layoutUpdated = 0;

        private void _listView_LayoutUpdated(object sender, EventArgs e)
        {
            _layoutUpdated++;
        }

        private void _listView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Home)
            {
                _listView.GoTop();
            }
            else if (e.Key == System.Windows.Input.Key.End)
            {
                _listView.GoBottom();
            }
        }

        private void _listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
            var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

            _gridViewColumn.Width = Math.Max(0, _listView.ActualWidth - 21);

            _listView.Items.Refresh();
        }

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            var selectItem = _listView.SelectedItem as MessageEx;
            if (selectItem == null) return;

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
                    item.IsEnabled = !selectItem.State.HasFlag(MessageState.IsLock);
                }
                else if (item.Name == "_richTextBoxUnlockThisMenuItem")
                {
                    item.IsEnabled = selectItem.State.HasFlag(MessageState.IsLock);
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

                var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                var mx = view.OfType<MessageEx>().ToArray()[index];

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                var item = new SearchContains<Message>()
                {
                    Contains = false,
                    Value = mx.Value,
                };

                if (!selectTreeViewItem.Value.SearchMessageCollection.Contains(item))
                    selectTreeViewItem.Value.SearchMessageCollection.Add(item);

                _listViewItemCollection.Remove(mx);

                view.Refresh();
            }

            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl) && e.ChangedButton == MouseButton.Left)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                var mx = view.OfType<MessageEx>().ToArray()[index];

                if (mx.State.HasFlag(MessageState.IsLock))
                {
                    mx.State &= ~MessageState.IsLock;
                }
                else
                {
                    mx.State |= MessageState.IsLock;
                }
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
            item.State |= MessageState.IsLock;
        }

        private void _richTextBoxUnlockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var item = (MessageEx)_listView.SelectedItem;
            item.State &= ~MessageState.IsLock;
        }

        private void _richTextBoxLockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.LockMessages.Clear();

            foreach (var item in _listViewItemCollection)
            {
                item.State |= MessageState.IsLock;
            }
        }

        private void _richTextBoxUnlockAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.LockMessages.Clear();

            foreach (var item in _listViewItemCollection)
            {
                item.State &= ~MessageState.IsLock;
            }
        }

        private void _richTextBoxFilterWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            if (richTextBox.Selection.IsEmpty) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var text = richTextBox.Selection.Text.Trim('\r', '\n');
            if (text.Length == 0) return;

            if (text == text.Replace("\r", "").Replace("\n", ""))
            {
                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = text,
                };

                if (selectTreeViewItem.Value.SearchWordCollection.Contains(item)) return;
                selectTreeViewItem.Value.SearchWordCollection.Add(item);
            }
            else
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = false,
                    Value = new SearchRegex() { IsIgnoreCase = true, Value = Regex.Escape(text) },
                };

                if (selectTreeViewItem.Value.SearchRegexCollection.Contains(item)) return;
                selectTreeViewItem.Value.SearchRegexCollection.Add(item);
            }

            this.Update();
        }

        private void _richTextBoxFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as MessageEx;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var signature = MessageConverter.ToSignatureString(message.Value.Certificate);
            if (signature == null) signature = "Anonymous";

            var item = new SearchContains<SearchRegex>()
            {
                Contains = false,
                Value = new SearchRegex()
                {
                    IsIgnoreCase = false,
                    Value = Regex.Escape(signature),
                },
            };

            if (selectTreeViewItem.Value.SearchSignatureCollection.Contains(item)) return;
            selectTreeViewItem.Value.SearchSignatureCollection.Add(item);

            this.Update();
        }

        private void _richTextBoxFilterMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as MessageEx;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            var item = new SearchContains<Message>()
            {
                Contains = false,
                Value = message.Value,
            };

            if (selectTreeViewItem.Value.SearchMessageCollection.Contains(item)) return;
            selectTreeViewItem.Value.SearchMessageCollection.Add(item);

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

        private void _onlyUnreadButton_Checked(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
            view.Refresh();

            _listView.UpdateLayout();
            _listView.GoBottom();
        }

        private void _onlyUnreadButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
            view.Refresh();

            _listView.UpdateLayout();
            _listView.GoBottom();
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

    class ChartTreeViewItem : TreeViewItem
    {
        private int _hit;
        private SearchTreeItem _value;
        private ObservableCollection<SearchTreeViewItem> _listViewItemCollection = new ObservableCollection<SearchTreeViewItem>();

        public SearchTreeViewItem()
            : base()
        {
            this.Value = new SearchTreeItem()
            {
                SearchItem = new SearchItem()
                {
                    Name = "",
                },
            };

            base.ItemsSource = _listViewItemCollection;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public SearchTreeViewItem(SearchTreeItem searchTreeItem)
            : base()
        {
            this.Value = searchTreeItem;

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

        protected override void OnExpanded(RoutedEventArgs e)
        {
            base.OnExpanded(e);

            this.Value.IsExpanded = true;
        }

        protected override void OnCollapsed(RoutedEventArgs e)
        {
            base.OnCollapsed(e);

            this.Value.IsExpanded = false;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.SearchItem.Name, _hit);

            base.IsExpanded = this.Value.IsExpanded;

            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

            foreach (var item in this.Value.Items)
            {
                list.Add(new SearchTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.OfType<SearchTreeViewItem>().ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.OfType<SearchTreeViewItem>().Any(n => object.ReferenceEquals(n.Value.SearchItem, item.Value.SearchItem)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<SearchTreeViewItem>().ToList();

            list.Sort(delegate(SearchTreeViewItem x, SearchTreeViewItem y)
            {
                int c = x.Value.SearchItem.Name.CompareTo(y.Value.SearchItem.Name);
                if (c != 0) return c;
                c = x.Hit.CompareTo(y.Hit);
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<SearchTreeViewItem>())
            {
                item.Sort();
            }
        }

        public SearchTreeItem Value
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

        public int Hit
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
    }
}
