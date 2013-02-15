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
    /// Interaction logic for ControlChannelControl.xaml
    /// </summary>
    partial class ControlChannelControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private Thread _searchThread = null;
        private Thread _cacheThread = null;
        private volatile bool _refresh = false;
        private volatile bool _scroll = false;

        private ObservableCollection<Message> _listViewItemCollection = new ObservableCollection<Message>();
        private LockedDictionary<Channel, List<Message>> _messages = new LockedDictionary<Channel, List<Message>>();

        private static Random _random = new Random();

        public ControlChannelControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            foreach (var item in Settings.Instance.ControlChannelControl_FilterRoots)
            {
                _treeView.Items.Add(item);
            }

            _listView.ItemsSource = _listViewItemCollection;

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (App.SelectTab != "Control/Channel") return;

                if (_treeView.SelectedItem is FilterRootTreeViewItem)
                {
                    var selectItem = (FilterRootTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Name);
                }
                else if (_treeView.SelectedItem is FilterCategoryTreeViewItem)
                {
                    var selectItem = (FilterCategoryTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Name);
                }
                else if (_treeView.SelectedItem is FilterChannelTreeViewItem)
                {
                    var selectItem = (FilterChannelTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Channel.Name);
                }
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "ControlChannelControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "ControlChannelControl_CacheThread";
            _cacheThread.Start();

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

                    FilterChannelTreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (_treeView.SelectedItem is FilterRootTreeViewItem)
                        {
                            var selectItem = (FilterRootTreeViewItem)_treeView.SelectedItem;

                            _listViewItemCollection.Clear();

                            if (App.SelectTab == "Control/Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Name);
                        }
                        else if (_treeView.SelectedItem is FilterCategoryTreeViewItem)
                        {
                            var selectItem = (FilterCategoryTreeViewItem)_treeView.SelectedItem;

                            _listViewItemCollection.Clear();

                            if (App.SelectTab == "Control/Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Name);
                        }
                        else if (_treeView.SelectedItem is FilterChannelTreeViewItem)
                        {
                            var selectItem = (FilterChannelTreeViewItem)_treeView.SelectedItem;

                            _listViewItemCollection.Clear();

                            if (App.SelectTab == "Control/Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Channel.Name);
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
                        oldList.UnionWith(_listViewItemCollection.OfType<Message>().ToArray());
                    }), null);

                    IList<Message> messages;
                    IList<Filter> filters;

                    _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        //_signButton.IsEnabled = true;
                        //_newMessageButton.IsEnabled = true;
                        //_onlyUnreadButton.IsEnabled = true;
                    }), null);

                    newList.Union(messages);

                    List<ISearchItem> searchItems = new List<ISearchItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        searchItems.AddRange(_treeView.GetLineage(selectTreeViewItem).OfType<ISearchItem>());
                    }), null);

                    foreach (var item in searchItems)
                    {
                        ControlChannelControl.Filter(ref newList, item.SearchItem);
                    }

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
                        tempList.Reverse();

                        sortList = tempList.ToList();

                        newList.Clear();
                        newList.UnionWith(sortList);
                    }

                    lock (_messages.ThisLock)
                    {
                        List<Message> tempMessages = null;

                        if (!_messages.TryGetValue(selectTreeViewItem.Value.Channel, out tempMessages))
                        {
                            tempMessages = new List<Message>();
                            _messages[selectTreeViewItem.Value.Channel] = tempMessages;
                        }

                        if (!Collection.Equals(sortList, tempMessages))
                        {
                            tempMessages.Clear();
                            tempMessages.AddRange(sortList);
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
                                _listViewItemCollection.Add(item);
                            }
                        }
                        else
                        {
                            foreach (var item in addList)
                            {
                                _listViewItemCollection.Add(item);
                            }

                            var tempList = _listViewItemCollection.Where(n => removeList.Contains(n)).ToArray();

                            foreach (var item in tempList)
                            {
                                _listViewItemCollection.Remove(item);
                            }
                        }

                        selectTreeViewItem.HitCount = _listViewItemCollection.Count;

                        {
                            string searchText = _searchTextBox.Text;

                            if (!string.IsNullOrWhiteSpace(searchText))
                            {
                                var words = searchText.ToLower().Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var item in _listViewItemCollection.ToArray())
                                {
                                    var text = RichTextBoxHelper.GetMessageToShowString(item).ToLower();

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
                            _listView.GoTop();
                        }

                        layoutUpdated = _layoutUpdated;

                        if (App.SelectTab == "Channel")
                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));
                    }), null);

                    while (_layoutUpdated == layoutUpdated) Thread.Sleep(100);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        _listView.GoTop();
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
                    List<FilterChannelTreeViewItem> channelTreeViewItems = new List<FilterChannelTreeViewItem>();

                    for (; ; )
                    {
                        List<FilterChannelTreeViewItem> items = new List<FilterChannelTreeViewItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            var list = new List<TreeViewItem>();
                            list.AddRange(_treeView.Items.Cast<TreeViewItem>());

                            for (int i = 0; i < list.Count; i++)
                            {
                                foreach (TreeViewItem item in list[i].Items)
                                {
                                    list.Add(item);
                                }
                            }

                            foreach (var item in list.OfType<FilterChannelTreeViewItem>())
                            {
                                items.Add(item);
                            }
                        }), null);

                        var selectTreeViewItem = items.FirstOrDefault(n => !channelTreeViewItems.Any(m => n == m));
                        if (selectTreeViewItem == null) break;

                        channelTreeViewItems.Add(selectTreeViewItem);

                        {
                            IList<Message> messages;
                            IList<Filter> filters;

                            _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                            HashSet<Message> newList = new HashSet<Message>(messages);

                            List<ISearchItem> searchItems = new List<ISearchItem>();

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                searchItems.AddRange(_treeView.GetLineage(selectTreeViewItem).OfType<ISearchItem>());
                            }), null);

                            foreach (var item in searchItems)
                            {
                                ControlChannelControl.Filter(ref newList, item.SearchItem);
                            }

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
                                tempList.Reverse();

                                sortList = tempList.ToList();

                                newList.Clear();
                                newList.UnionWith(sortList);
                            }

                            lock (_messages.ThisLock)
                            {
                                List<Message> tempMessages = null;

                                if (!_messages.TryGetValue(selectTreeViewItem.Value.Channel, out tempMessages))
                                {
                                    tempMessages = new List<Message>();
                                    _messages[selectTreeViewItem.Value.Channel] = tempMessages;
                                }

                                if (!Collection.Equals(sortList, tempMessages))
                                {
                                    tempMessages.Clear();
                                    tempMessages.AddRange(sortList);
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                selectTreeViewItem.HitCount = sortList.Count;
                            }), null);
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

        void RichTextBoxHelper_ChannelClickEvent(object sender, Channel channel)
        {
            var selectChannelTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectChannelTreeViewItem == null) return;

            if (channel.Name == null || channel.Id == null) return;

            HashSet<Channel> channelList = new HashSet<Channel>();

            {
                List<TreeViewItem> itemList = new List<TreeViewItem>();
                itemList.AddRange(_treeView.Items.Cast<TreeViewItem>());

                for (int i = 0; i < itemList.Count; i++)
                {
                    itemList.AddRange(itemList[i].Items.Cast<TreeViewItem>());
                }

                foreach (var item in itemList.OfType<FilterChannelTreeViewItem>())
                {
                    channelList.Add(item.Value.Channel);
                }
            }

            if (channelList.Contains(channel)) return;

            var list2 = _treeView.GetLineage(selectChannelTreeViewItem).OfType<TreeViewItem>().ToList();
            var selectCategoryTreeViewItem = ((FilterCategoryTreeViewItem)list2[list2.Count - 2]) as FilterCategoryTreeViewItem;

            selectCategoryTreeViewItem.Value.FilterChannels.Add(new FilterChannel() { Channel = channel });
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
                MessageBox.Show(LanguagesManager.Instance.ControlChannelControl_AmoebaNotFound_Message);

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

        private static void Filter(ref HashSet<Message> messages, SearchItem filterItem)
        {
            lock (filterItem.ThisLock)
            {
                messages.IntersectWith(messages.ToArray().Where(item =>
                {
                    bool flag = true;

                    lock (filterItem.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (filterItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == true))
                        {
                            flag = filterItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchWordCollection.ThisLock)
                    {
                        if (filterItem.SearchWordCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = filterItem.SearchWordCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchSignatureCollection.ThisLock)
                    {
                        if (filterItem.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = item.Certificate.ToString();
                            }

                            flag = filterItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return signatureText == searchContains.Value;

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchRegexCollection.ThisLock)
                    {
                        if (filterItem.SearchRegexCollection.Any(n => n.Contains == true))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = filterItem.SearchRegexCollection.Any(searchContains =>
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

                    lock (filterItem.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (filterItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = filterItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.CreationTime);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchWordCollection.ThisLock)
                    {
                        if (filterItem.SearchWordCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = filterItem.SearchWordCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return item.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchSignatureCollection.ThisLock)
                    {
                        if (filterItem.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            string signatureText = null;

                            if (item.Certificate == null)
                            {
                                signatureText = "Anonymous";
                            }
                            else
                            {
                                signatureText = item.Certificate.ToString();
                            }

                            flag = filterItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value == signatureText;

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchRegexCollection.ThisLock)
                    {
                        if (filterItem.SearchRegexCollection.Any(n => n.Contains == false))
                        {
                            //var messageText = RichTextBoxHelper.GetMessageToShowString(item);

                            flag = filterItem.SearchRegexCollection.Any(searchContains =>
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
            Settings.Instance.ControlChannelControl_FilterRoots = _treeView.Items.OfType<FilterRootTreeViewItem>().Select(n => n.Value).ToLockedList();

            if (scroll) _treeView_SelectedItemChanged(this, null);
            else _treeView_SelectedItemChanged(null, null);

            foreach (var item in _treeView.Items.OfType<FilterRootTreeViewItem>())
            {
                item.Sort();
            }
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
                    if (_treeView.SelectedItem is FilterRootTreeViewItem) return;

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
                var t = _treeView.GetCurrentItem(e.GetPosition) as FilterCategoryTreeViewItem;
                if (t == null || t.Equals(s)
                    || t.Value.FilterCategorys.Any(n => object.ReferenceEquals(n, s.Value))
                    || t.Value.FilterChannels.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                var list = _treeView.GetLineage((TreeViewItem)s).OfType<TreeViewItem>().ToList();

                t.IsSelected = true;

                if (s.Value is FilterCategory)
                {
                    var tItems = ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterCategorys.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterCategorys.Clear();
                    ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterCategorys.AddRange(tItems);
                }
                else if (s.Value is FilterChannel)
                {
                    var tItems = ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterChannels.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterChannels.Clear();
                    ((FilterCategoryTreeViewItem)list[list.Count - 2]).Value.FilterChannels.AddRange(tItems);
                }

                if (s.Value is FilterCategory) t.Value.FilterCategorys.Add(s.Value);
                else if (s.Value is FilterChannel && !t.Value.FilterChannels.Any(n => n == s.Value)) t.Value.FilterChannels.Add(s.Value);

                ((FilterCategoryTreeViewItem)list[list.Count - 2]).Update();
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
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterRootTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        
        private void _filterRootTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterRootTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemNewChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterCategoryTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterChannelTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterChannelTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterChannelTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterChannelTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _filterChannelTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {

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
            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var richTextBox = sender as RichTextBox;
            if (richTextBox == null) return;

            var selectItem = _listView.SelectedItem as Message;
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
            }
        }

        private void _listView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl | System.Windows.Input.Key.RightCtrl) && e.ChangedButton == MouseButton.Left)
            {
                var index = _listView.GetCurrentIndex(e.GetPosition);
                if (index == -1) return;

                var view = CollectionViewSource.GetDefaultView(_listView.ItemsSource);
                var mx = view.OfType<Message>().ToArray()[index];

                selectTreeViewItem.Value.TrustMessages.Add(mx);

                view.Refresh();
            }
        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var selectItem = _listView.SelectedItem as Message;
            if (selectItem == null) return;

            StringBuilder builder = new StringBuilder();

            if (!richTextBox.Selection.IsEmpty)
            {
                string text = richTextBox.Selection.Text;

                builder.AppendLine();
                builder.AppendLine();

                foreach (var line in text.Trim('\r', '\n').Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
                {
                    builder.AppendLine("> " + line);
                }

                richTextBox.Selection.Select(richTextBox.Document.ContentStart, richTextBox.Document.ContentStart);
            }

            var responsMessages = _listView.SelectedItems.OfType<Message>();

            MessageEditWindow window = new MessageEditWindow(selectTreeViewItem.Value.Channel, builder.ToString(), responsMessages, _lairManager);
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

            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            string text = richTextBox.Selection.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                text = RichTextBoxHelper.GetMessageToString(message);
            }

            Clipboard.SetText(text);
        }

        private void _richTextBoxTrustSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            selectTreeViewItem.Value.TrustSignatures.Add(message.Certificate.ToString());
        }

        private void _richTextBoxTrustMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            selectTreeViewItem.Value.TrustMessages.Add(message);
        }

        private void _richTextBoxFilterWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            if (richTextBox.Selection.IsEmpty) return;

            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
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

                if (selectTreeViewItem.Value.SearchItem.SearchWordCollection.Contains(item)) return;
                selectTreeViewItem.Value.SearchItem.SearchWordCollection.Add(item);
            }
            else
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = false,
                    Value = new SearchRegex() { IsIgnoreCase = true, Value = Regex.Escape(text) },
                };

                if (selectTreeViewItem.Value.SearchItem.SearchRegexCollection.Contains(item)) return;
                selectTreeViewItem.Value.SearchItem.SearchRegexCollection.Add(item);
            }

            this.Update();
        }

        private void _richTextBoxFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            string signature = null;

            if (message.Certificate != null) signature = message.Certificate.ToString();
            else signature = "Anonymous";

            var item = new SearchContains<string>()
            {
                Contains = false,
                Value = signature,
            };

            if (selectTreeViewItem.Value.SearchItem.SearchSignatureCollection.Contains(item)) return;
            selectTreeViewItem.Value.SearchItem.SearchSignatureCollection.Add(item);

            this.Update();
        }

        private void _richTextBoxFilterMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as FilterChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var item = new SearchContains<Message>()
            {
                Contains = false,
                Value = message,
            };

            if (selectTreeViewItem.Value.SearchItem.SearchMessageCollection.Contains(item)) return;
            selectTreeViewItem.Value.SearchItem.SearchMessageCollection.Add(item);

            this.Update();
        }

        #endregion

        #region Search

        private void _searchCloseButton_Click(object sender, RoutedEventArgs e)
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

        #endregion

        #region Sort

        private void Sort()
        {
            List<Message> list = new List<Message>(_listViewItemCollection);

            list.Sort(delegate(Message x, Message y)
            {
                int c = y.CreationTime.CompareTo(x.CreationTime);
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

        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {

            }
            else
            {

            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {

            }
            else
            {

            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_listView.SelectedItems.Count == 0)
            {

            }
            else
            {

            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void Execute_Search(object sender, ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }

    class FilterRootTreeViewItem : TreeViewItem
    {
        private ObservableCollection<FilterCategoryTreeViewItem> _listViewItemCollection = new ObservableCollection<FilterCategoryTreeViewItem>();
        private FilterRoot _value;

        public FilterRootTreeViewItem(FilterRoot filterRoot)
            : base()
        {
            this.Value = filterRoot;

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
            List<FilterCategoryTreeViewItem> list = new List<FilterCategoryTreeViewItem>();

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in this.Value.FilterCategorys)
            {
                list.Add(new FilterCategoryTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.ToList();

            list.Sort(delegate(FilterCategoryTreeViewItem x, FilterCategoryTreeViewItem y)
            {
                int c = x.Name.CompareTo(y.Name);
                if (c != 0) return c;

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }
        }

        public FilterRoot Value
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

    class FilterCategoryTreeViewItem : TreeViewItem
    {
        private ObservableCollection<dynamic> _listViewItemCollection = new ObservableCollection<dynamic>();
        private FilterCategory _value;

        public FilterCategoryTreeViewItem(FilterCategory filterCategory)
            : base()
        {
            this.Value = filterCategory;

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
            List<dynamic> list = new List<dynamic>();

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in this.Value.FilterCategorys)
            {
                list.Add(new FilterCategoryTreeViewItem(item));
            }

            foreach (var item in this.Value.FilterChannels)
            {
                list.Add(new FilterChannelTreeViewItem(item));
            }

            foreach (var item in _listViewItemCollection.ToArray())
            {
                if (!list.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in list)
            {
                if (!_listViewItemCollection.Any(n => object.ReferenceEquals(n.Value, item.Value)))
                {
                    _listViewItemCollection.Add(item);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.Cast<object>().ToList();

            Dictionary<Type, int> typeSortItems = new Dictionary<Type, int>();
            typeSortItems[typeof(FilterCategoryTreeViewItem)] = 0;
            typeSortItems[typeof(FilterChannelTreeViewItem)] = 1;

            list.Sort(delegate(object x, object y)
            {
                int tx = typeSortItems[x.GetType()];
                int ty = typeSortItems[y.GetType()];

                int c = tx.CompareTo(ty);
                if (c != 0) return c;

                if (x is FilterChannelTreeViewItem && y is FilterChannelTreeViewItem)
                {
                    var cx = ((FilterChannelTreeViewItem)x).Value.Channel;
                    var cy = ((FilterChannelTreeViewItem)y).Value.Channel;

                    c = cx.Name.CompareTo(cy.Name);
                    if (c != 0) return c;
                    c = Collection.Compare(cx.Id, cy.Id);
                    if (c != 0) return c;
                }

                return 0;
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }
        }

        public FilterCategory Value
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

    class FilterChannelTreeViewItem : TreeViewItem
    {
        private FilterChannel _value;
        private bool _hit;
        private int _hitCount;

        public FilterChannelTreeViewItem(FilterChannel filterChannel)
            : base()
        {
            this.Value = filterChannel;

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
            base.Header = string.Format("{0} ({1})", _value.Channel.Name, _hit);
        }

        public FilterChannel Value
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

        [DataMember(Name = "Hit")]
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

        [DataMember(Name = "HitCount")]
        public int HitCount
        {
            get
            {
                return _hitCount;
            }
            set
            {
                _hitCount = value;

                this.Update();
            }
        }
    }

    [DataContract(Name = "FilterRoot", Namespace = "http://Lair/Windows")]
    class FilterRoot : IDeepCloneable<FilterRoot>, ISearchItem, IThisLock
    {
        private string _name;
        private DigitalSignature _filterDigitalSignature;
        private SearchItem _searchItem;
        private LockedList<string> _trustSignatures;
        private LockedList<FilterCategory> _filterCategorys;
        private bool _isExpanded = true;

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

        [DataMember(Name = "FilterDigitalSignature")]
        public DigitalSignature FilterDigitalSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _filterDigitalSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _filterDigitalSignature = value;
                }
            }
        }

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _searchItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _searchItem = value;
                }
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public LockedList<string> TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new LockedList<string>();

                    return _trustSignatures;
                }
            }
        }

        [DataMember(Name = "FilterCategorys")]
        public LockedList<FilterCategory> FilterCategorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_filterCategorys == null)
                        _filterCategorys = new LockedList<FilterCategory>();

                    return _filterCategorys;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        #region IDeepClone<ChannelFilterTreeItem>

        public FilterRoot DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(FilterRoot));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (FilterRoot)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "FilterCategory", Namespace = "http://Lair/Windows")]
    class FilterCategory : IDeepCloneable<FilterCategory>, ISearchItem, IThisLock
    {
        private string _name;
        private SearchItem _searchItem;
        private LockedList<string> _trustSignatures;
        private LockedList<FilterChannel> _filterChannels;
        private LockedList<FilterCategory> _filterCategorys;
        private bool _isExpanded = true;

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

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _searchItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _searchItem = value;
                }
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public LockedList<string> TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new LockedList<string>();

                    return _trustSignatures;
                }
            }
        }

        [DataMember(Name = "FilterChannels")]
        public LockedList<FilterChannel> FilterChannels
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_filterChannels == null)
                        _filterChannels = new LockedList<FilterChannel>();

                    return _filterChannels;
                }
            }
        }

        [DataMember(Name = "Items")]
        public LockedList<FilterCategory> FilterCategorys
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_filterCategorys == null)
                        _filterCategorys = new LockedList<FilterCategory>();

                    return _filterCategorys;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        #region IDeepClone<ChannelFilterTreeItem>

        public FilterCategory DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(FilterCategory));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (FilterCategory)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "FilterChannel", Namespace = "http://Lair/Windows")]
    class FilterChannel : IDeepCloneable<FilterChannel>, ISearchItem, IThisLock
    {
        private Channel _channel;
        private SearchItem _searchItem;
        private LockedList<string> _trustSignatures;
        private LockedList<Message> _trustMessages;

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

        [DataMember(Name = "SearchItem")]
        public SearchItem SearchItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _searchItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _searchItem = value;
                }
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public LockedList<string> TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new LockedList<string>();

                    return _trustSignatures;
                }
            }
        }

        [DataMember(Name = "TrustMessages")]
        public LockedList<Message> TrustMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _trustMessages;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _trustMessages = value;
                }
            }
        }

        #region IDeepClone<ChannelFilterTreeItem>

        public FilterChannel DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(FilterChannel));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (FilterChannel)ds.ReadObject(textDictionaryReader);
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

    interface ISearchItem
    {
        SearchItem SearchItem { get; }
    }

    [DataContract(Name = "SearchItem", Namespace = "http://Lair/Windows")]
    class SearchItem : IDeepCloneable<SearchItem>, IThisLock
    {
        private LockedList<SearchContains<string>> _searchWordCollection;
        private LockedList<SearchContains<SearchRegex>> _searchRegexCollection;
        private LockedList<SearchContains<string>> _searchSignatureCollection;
        private LockedList<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;
        private LockedList<SearchContains<Message>> _searchMessageCollection;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

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

        [DataMember(Name = "SearchCreationTimeRangeCollection")]
        public LockedList<SearchContains<SearchRange<DateTime>>> SearchCreationTimeRangeCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchCreationTimeRangeCollection == null)
                        _searchCreationTimeRangeCollection = new LockedList<SearchContains<SearchRange<DateTime>>>();

                    return _searchCreationTimeRangeCollection;
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

        #region IDeepClone<SearchItem>

        public SearchItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchItem)ds.ReadObject(textDictionaryReader);
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
        private bool _contains;
        private T _value;

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
                return this.Value.GetHashCode();
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

            if ((this.Contains != other.Contains)
                || (!this.Value.Equals(other.Value)))
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

    [DataContract(Name = "SearchRange", Namespace = "http://Lair/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, IDeepCloneable<SearchRange<T>>, IThisLock
        where T : IComparable
    {
        T _max;
        T _min;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Max")]
        public T Max
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _max;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _max = value;
                    _min = (_min.CompareTo(_max) > 0) ? _max : _min;
                }
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _min;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _min = value;
                    _max = (_max.CompareTo(_min) < 0) ? _min : _max;
                }
            }
        }

        public bool Verify(T value)
        {
            lock (this.ThisLock)
            {
                if (value.CompareTo(this.Min) < 0 || value.CompareTo(this.Max) > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public override int GetHashCode()
        {
            lock (this.ThisLock)
            {
                return this.Min.GetHashCode();
            }
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SearchRange<T>)) return false;

            return this.Equals((SearchRange<T>)obj);
        }

        public bool Equals(SearchRange<T> other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if ((!this.Min.Equals(other.Min))
                || (!this.Max.Equals(other.Max)))
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
            }
        }

        #region IDeepClone<SearchRange<T>>

        public SearchRange<T> DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchRange<T>));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchRange<T>)ds.ReadObject(textDictionaryReader);
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
