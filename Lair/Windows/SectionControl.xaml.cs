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
    /// Interaction logic for SectionControl.xaml
    /// </summary>
    partial class SectionControl : UserControl
    {
        private MainWindow _mainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private Thread _searchThread = null;
        private Thread _cacheThread = null;

        private volatile bool _refresh = false;

        private ObservableCollection<MessageWrapper> _listViewItemCollection = new ObservableCollection<MessageWrapper>();

        private static Random _random = new Random();

        public SectionControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems)
            {
                _treeView.Items.Add(item);
            }

            _listView.ItemsSource = _listViewItemCollection;

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (App.SelectTab != "Section" || _refresh) return;

                if (_treeView.SelectedItem is SectionTreeViewItem)
                {
                    var selectItem = (SectionTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToSectionString(selectItem.Value.Section));
                }
                else if (_treeView.SelectedItem is SearchTreeViewItem)
                {
                    var selectItem = (SearchTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.SearchItem.Name);
                }
                else if (_treeView.SelectedItem is ChannelTreeViewItem)
                {
                    var selectItem = (ChannelTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.Channel.Name);
                }
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SectionControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "SectionControl_CacheThread";
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

                    ChannelTreeViewItem selectTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (App.SelectTab != "Section") return;

                        if (_treeView.SelectedItem is SectionTreeViewItem)
                        {
                            var selectItem = (SectionTreeViewItem)_treeView.SelectedItem;

                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToSectionString(selectItem.Value.Section));
                        }
                        else if (_treeView.SelectedItem is SearchTreeViewItem)
                        {
                            var selectItem = (SearchTreeViewItem)_treeView.SelectedItem;

                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectItem.Value.SearchItem.Name);
                        }

                        if ((_treeView.SelectedItem is SectionTreeViewItem) || (_treeView.SelectedItem is SearchTreeViewItem))
                        {
                            _refresh = false;

                            _listViewItemCollection.Clear();
                        }
                        else if (_treeView.SelectedItem is ChannelTreeViewItem)
                        {
                            selectTreeViewItem = (ChannelTreeViewItem)_treeView.SelectedItem;
                        }
                    }), null);

                    if (selectTreeViewItem == null)
                    {
                        continue;
                    }

                    var newList = new HashSet<MessageWrapper>();
                    var oldList = new HashSet<MessageWrapper>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        oldList.UnionWith(_listViewItemCollection.OfType<MessageWrapper>().ToArray());
                    }), null);

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        foreach (var item in selectTreeViewItem.Value.Messages)
                        {
                            newList.Add(new MessageWrapper(item.Key, item.Value));
                        }
                    }), null);

                    List<MessageWrapper> sortList = new List<MessageWrapper>();

                    {
                        var tempList = newList.ToList();

                        tempList.Sort(new Comparison<MessageWrapper>((MessageWrapper x, MessageWrapper y) =>
                        {
                            int c = x.Value.CreationTime.CompareTo(y.Value.CreationTime);
                            if (c != 0) return c;
                            c = x.Value.Content.CompareTo(y.Value.Content);
                            if (c != 0) return c;
                            c = Collection.Compare(x.Value.GetHash(HashAlgorithm.Sha512), y.Value.GetHash(HashAlgorithm.Sha512));
                            if (c != 0) return c;

                            return x.GetHashCode().CompareTo(y.GetHashCode());
                        }));

                        tempList = tempList.Skip(tempList.Count - 1024).ToList();
                        tempList.Reverse();

                        sortList = tempList.ToList();

                        newList.Clear();
                        newList.UnionWith(sortList);
                    }

                    var removeList = new List<MessageWrapper>();
                    var addList = new List<MessageWrapper>();

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

                        if (App.SelectTab == "Section")
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
                foreach (var sectionTreeViewItem in _treeView.Items.OfType<SectionTreeViewItem>())
                {
                    var trustSignatures = new HashSet<string>();

                    {
                        List<Leader> leaders = new List<Leader>(_lairManager.GetLeaders(sectionTreeViewItem.Value.Section));
                        List<Manager> managers = new List<Manager>(_lairManager.GetManagers(sectionTreeViewItem.Value.Section));

                        var leader = leaders.FirstOrDefault(n => n.Certificate.ToString() == sectionTreeViewItem.Value.SectionLeaderSignature);
                        if (leader == null) goto End;

                        var managerSignatrues = new HashSet<string>(leader.ManagerSignatures);

                        foreach (var manager in managers)
                        {
                            if (!managerSignatrues.Contains(manager.Certificate.ToString())) continue;

                            trustSignatures.UnionWith(manager.TrustSignatures);
                        }
                    End: ;
                    }

                    List<ChannelTreeViewItem> items = new List<ChannelTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        var list = new List<TreeViewItem>();
                        list.AddRange(sectionTreeViewItem.Items.Cast<TreeViewItem>());

                        for (int i = 0; i < list.Count; i++)
                        {
                            foreach (TreeViewItem item in list[i].Items)
                            {
                                list.Add(item);
                            }
                        }

                        foreach (var item in list.OfType<ChannelTreeViewItem>())
                        {
                            items.Add(item);
                        }
                    }), null);

                    foreach (var selectTreeViewItem in items)
                    {
                        HashSet<Message> newList = new HashSet<Message>(_lairManager.GetMessages(selectTreeViewItem.Value.Channel));

                        List<SearchItem> searchItems = new List<SearchItem>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            searchItems.AddRange(_treeView.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>().Select(n => n.Value.SearchItem));
                        }), null);

                        foreach (var item in searchItems)
                        {
                            SectionControl.Filter(ref newList, item);
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

                for (; ; )
                {
                    List<ChannelTreeViewItem> channelTreeViewItems = new List<ChannelTreeViewItem>();

                    for (; ; )
                    {
                        List<ChannelTreeViewItem> items = new List<ChannelTreeViewItem>();

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

                            foreach (var item in list.OfType<ChannelTreeViewItem>())
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
                                SectionControl.Filter(ref newList, item.SearchItem);
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

        private static HashSet<string> GetTrustSignatures(Section section, string leaderSignature, LairManager lairManager)
        {
            IList<Leader> leaders;
            IList<Manager> managers;
            IList<Creator> creators;

            lairManager.GetSectionInfomation(section, out leaders, out managers, out creators);

            var leader = leaders.FirstOrDefault(n => n.Certificate.ToString() == leaderSignature);
            if (leader == null) return null;

            var managerSignatrues = new HashSet<string>(leader.ManagerSignatures);
            var trustSignatures = new HashSet<string>();

            foreach (var manager in managers)
            {
                if (!managerSignatrues.Contains(manager.Certificate.ToString())) continue;

                trustSignatures.UnionWith(manager.TrustSignatures);
            }

            return trustSignatures;
        }

        void RichTextBoxHelper_ChannelClickEvent(object sender, Channel channel)
        {
            var selectChannelTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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

                foreach (var item in itemList.OfType<ChannelTreeViewItem>())
                {
                    channelList.Add(item.Value.Channel);
                }
            }

            if (channelList.Contains(channel)) return;

            var list2 = _treeView.GetLineage(selectChannelTreeViewItem).OfType<TreeViewItem>().ToList();
            var selectCategoryTreeViewItem = ((SearchTreeViewItem)list2[list2.Count - 2]) as SearchTreeViewItem;

            selectCategoryTreeViewItem.Value.ChannelTreeItems.Add(new ChannelTreeItem() { Channel = channel });
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
                MessageBox.Show(LanguagesManager.Instance.SectionControl_AmoebaNotFound_Message);

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

        private static void Filter(ref HashSet<MessageWrapper> messages, SearchItem filterItem)
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
                                if (searchContains.Contains) return searchContains.Value.Verify(item.Value.CreationTime);

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
                                if (searchContains.Contains) return item.Value.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchSignatureCollection.ThisLock)
                    {
                        if (filterItem.SearchSignatureCollection.Any(n => n.Contains == true))
                        {
                            flag = filterItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (searchContains.Contains) return searchContains.Value == item.Value.Certificate.ToString();

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
                                if (searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));

                messages.ExceptWith(messages.ToArray().Where(item =>
                {
                    if (item.Value.Content.Contains('\uFFFD')) return true;

                    bool flag = false;

                    lock (filterItem.SearchCreationTimeRangeCollection.ThisLock)
                    {
                        if (filterItem.SearchCreationTimeRangeCollection.Any(n => n.Contains == false))
                        {
                            flag = filterItem.SearchCreationTimeRangeCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value.Verify(item.Value.CreationTime);

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
                                if (!searchContains.Contains) return item.Value.Content.Contains(searchContains.Value);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    lock (filterItem.SearchSignatureCollection.ThisLock)
                    {
                        if (filterItem.SearchSignatureCollection.Any(n => n.Contains == false))
                        {
                            flag = filterItem.SearchSignatureCollection.Any(searchContains =>
                            {
                                if (!searchContains.Contains) return searchContains.Value == item.Value.Certificate.ToString();

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
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Content);

                                return false;
                            });
                            if (flag) return true;
                        }
                    }

                    return flag;
                }));
            }
        }

        private void Update()
        {
            Settings.Instance.SectionControl_SectionTreeItems = _treeView.Items.OfType<SectionTreeViewItem>().Select(n => n.Value).ToLockedList();

            _treeView_SelectedItemChanged(null, null);

            foreach (var item in _treeView.Items.OfType<SectionTreeViewItem>())
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
                    if (_treeView.SelectedItem is SectionTreeViewItem) return;

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
                var t = _treeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;
                if (t == null || t.Equals(s)
                    || t.Value.SearchTreeItems.Any(n => object.ReferenceEquals(n, s.Value))
                    || t.Value.ChannelTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                var list = _treeView.GetLineage((TreeViewItem)s).OfType<TreeViewItem>().ToList();

                t.IsSelected = true;

                if (s.Value is SearchTreeItem)
                {
                    var tItems = ((SearchTreeViewItem)list[list.Count - 2]).Value.SearchTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((SearchTreeViewItem)list[list.Count - 2]).Value.SearchTreeItems.Clear();
                    ((SearchTreeViewItem)list[list.Count - 2]).Value.SearchTreeItems.AddRange(tItems);
                }
                else if (s.Value is ChannelTreeItem)
                {
                    var tItems = ((SearchTreeViewItem)list[list.Count - 2]).Value.ChannelTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    ((SearchTreeViewItem)list[list.Count - 2]).Value.ChannelTreeItems.Clear();
                    ((SearchTreeViewItem)list[list.Count - 2]).Value.ChannelTreeItems.AddRange(tItems);
                }

                if (s.Value is SearchTreeItem) t.Value.SearchTreeItems.Add(s.Value);
                else if (s.Value is ChannelTreeItem && !t.Value.ChannelTreeItems.Any(n => n == s.Value)) t.Value.ChannelTreeItems.Add(s.Value);

                ((SearchTreeViewItem)list[list.Count - 2]).Update();
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
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _sectionTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _searchTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region _listView

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
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var message = _listView.SelectedItem as Message;
            if (message == null) return;

            selectTreeViewItem.Value.TrustSignatures.Add(message.Certificate.ToString());
        }

        private void _richTextBoxTrustMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
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
            List<MessageWrapper> list = new List<MessageWrapper>(_listViewItemCollection);

            list.Sort(delegate(MessageWrapper x, MessageWrapper y)
            {
                int c = y.Value.CreationTime.CompareTo(x.Value.CreationTime);
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

    [Flags]
    [DataContract(Name = "MessageState", Namespace = "http://Lair/Windows")]
    enum MessageState
    {
        [EnumMember(Value = "New")]
        New = 0x1
    }

    class MessageWrapper : INotifyPropertyChanged, IEquatable<MessageWrapper>
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private Message _value;
        private MessageState _state;

        public MessageWrapper(Message value, MessageState state)
        {
            _value = value;
            _state = state;
        }

        public override int GetHashCode()
        {
            if (_value == null) return 0;
            else return _value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MessageWrapper)) return false;

            return this.Equals((MessageWrapper)obj);
        }

        public bool Equals(MessageWrapper other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.State != other.State
                || this.Value != other.Value)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "State")]
        public MessageState State
        {
            get
            {
                return _state;
            }
            set
            {
                if (value != _state)
                {
                    _state = value;

                    this.NotifyPropertyChanged("State");
                }
            }
        }

        [DataMember(Name = "Value")]
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

    [DataContract(Name = "LeaderInfo", Namespace = "http://Lair/Windows")]
    class LeaderInfo : IDeepCloneable<LeaderInfo>, IThisLock
    {
        private SignatureCollection _creatorSignatures = null;
        private SignatureCollection _managerSignatures = null;
        private string _comment = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public LeaderInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_comment == null) return 0;
            else return _comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is LeaderInfo)) return false;

            return this.Equals((LeaderInfo)obj);
        }

        public override bool Equals(LeaderInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            if (this.CreatorSignatures != null && other.CreatorSignatures != null)
            {
                if (this.CreatorSignatures.Count != other.CreatorSignatures.Count) return false;

                for (int i = 0; i < this.CreatorSignatures.Count; i++) if (this.CreatorSignatures[i] != other.CreatorSignatures[i]) return false;
            }

            if (this.ManagerSignatures != null && other.ManagerSignatures != null)
            {
                if (this.ManagerSignatures.Count != other.ManagerSignatures.Count) return false;

                for (int i = 0; i < this.ManagerSignatures.Count; i++) if (this.ManagerSignatures[i] != other.ManagerSignatures[i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return _comment;
        }

        [DataMember(Name = "CreatorSignatures")]
        public SignatureCollection CreatorSignatures
        {
            get
            {
                if (_creatorSignatures == null)
                    _creatorSignatures = new SignatureCollection(Leader.MaxCreatorSignaturesCount);

                return _creatorSignatures;
            }
        }

        [DataMember(Name = "ManagerSignatures")]
        public SignatureCollection ManagerSignatures
        {
            get
            {
                if (_managerSignatures == null)
                    _managerSignatures = new SignatureCollection(Leader.MaxManagerSignaturesCount);

                return _managerSignatures;
            }
        }

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (value != null && value.Length > Leader.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        #region IDeepClone<LeaderInfo>

        public LeaderInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(LeaderInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (LeaderInfo)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "ManagerInfo", Namespace = "http://Lair/Windows")]
    class ManagerInfo : IDeepCloneable<ManagerInfo>, IThisLock
    {
        private SignatureCollection _trustSignatures = null;
        private string _comment = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public ManagerInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_comment == null) return 0;
            else return _comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ManagerInfo)) return false;

            return this.Equals((ManagerInfo)obj);
        }

        public override bool Equals(ManagerInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            if (this.TrustSignatures != null && other.TrustSignatures != null)
            {
                if (this.TrustSignatures.Count != other.TrustSignatures.Count) return false;

                for (int i = 0; i < this.TrustSignatures.Count; i++) if (this.TrustSignatures[i] != other.TrustSignatures[i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return _comment;
        }

        [DataMember(Name = "TrustSignatures")]
        public SignatureCollection TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new SignatureCollection(Manager.MaxTrustSignaturesCount);

                return _trustSignatures;
            }
        }

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (value != null && value.Length > Manager.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        #region IDeepClone<ManagerInfo>

        public ManagerInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ManagerInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ManagerInfo)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "CreatorInfo", Namespace = "http://Lair/Windows")]
    class CreatorInfo : IDeepCloneable<CreatorInfo>, IThisLock
    {
        private ChannelCollection _channels = null;
        private string _comment = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public CreatorInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_comment == null) return 0;
            else return _comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is CreatorInfo)) return false;

            return this.Equals((CreatorInfo)obj);
        }

        public override bool Equals(CreatorInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            if (this.Channels != null && other.Channels != null)
            {
                if (this.Channels.Count != other.Channels.Count) return false;

                for (int i = 0; i < this.Channels.Count; i++) if (this.Channels[i] != other.Channels[i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return _comment;
        }

        [DataMember(Name = "Channels")]
        public ChannelCollection Channels
        {
            get
            {
                if (_channels == null)
                    _channels = new ChannelCollection(Creator.MaxChannelsCount);

                return _channels;
            }
        }

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (value != null && value.Length > Creator.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        #region IDeepClone<CreatorInfo>

        public CreatorInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(CreatorInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (CreatorInfo)ds.ReadObject(textDictionaryReader);
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

    class SectionTreeViewItem : TreeViewItem
    {
        private ObservableCollection<SearchTreeViewItem> _listViewItemCollection = new ObservableCollection<SearchTreeViewItem>();
        private SectionTreeItem _value;

        public SectionTreeViewItem(SectionTreeItem sectionTreeItem)
            : base()
        {
            this.Value = sectionTreeItem;

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
            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in this.Value.SearchTreeItems)
            {
                list.Add(new SearchTreeViewItem(item));
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

            list.Sort(delegate(SearchTreeViewItem x, SearchTreeViewItem y)
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

        public SectionTreeItem Value
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

    class SearchTreeViewItem : TreeViewItem
    {
        private ObservableCollection<dynamic> _listViewItemCollection = new ObservableCollection<dynamic>();
        private SearchTreeItem _value;

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
            List<dynamic> list = new List<dynamic>();

            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in this.Value.SearchTreeItems)
            {
                list.Add(new SearchTreeViewItem(item));
            }

            foreach (var item in this.Value.ChannelTreeItems)
            {
                list.Add(new ChannelTreeViewItem(item));
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
            typeSortItems[typeof(SearchTreeViewItem)] = 0;
            typeSortItems[typeof(ChannelTreeViewItem)] = 1;

            list.Sort(delegate(object x, object y)
            {
                int tx = typeSortItems[x.GetType()];
                int ty = typeSortItems[y.GetType()];

                int c = tx.CompareTo(ty);
                if (c != 0) return c;

                if (x is ChannelTreeViewItem && y is ChannelTreeViewItem)
                {
                    var cx = ((ChannelTreeViewItem)x).Value.Channel;
                    var cy = ((ChannelTreeViewItem)y).Value.Channel;

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
    }

    class ChannelTreeViewItem : TreeViewItem
    {
        private ChannelTreeItem _value;

        public ChannelTreeViewItem(ChannelTreeItem channelTreeItem)
            : base()
        {
            this.Value = channelTreeItem;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.Channel.Name, _value.Messages.Count);
        }

        public ChannelTreeItem Value
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

    [DataContract(Name = "SectionTreeItem", Namespace = "http://Lair/Windows")]
    class SectionTreeItem : IDeepCloneable<SectionTreeItem>, IThisLock
    {
        private Section _section;
        private string _sectionLeaderSignature;

        private string _uploadSignature;

        private LeaderInfo _leaderInfo;
        private ManagerInfo _managerInfo;
        private CreatorInfo _creatorInfo;

        private List<SearchTreeItem> _searchTreeItems;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Section")]
        public Section Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _section;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _section = value;
                }
            }
        }

        [DataMember(Name = "SectionLeaderSignature")]
        public string SectionLeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _sectionLeaderSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _sectionLeaderSignature = value;
                }
            }
        }

        [DataMember(Name = "UploadSignature")]
        public string UploadSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _uploadSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _uploadSignature = value;
                }
            }
        }

        [DataMember(Name = "LeaderInfo")]
        public LeaderInfo LeaderInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _leaderInfo;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _leaderInfo = value;
                }
            }
        }

        [DataMember(Name = "ManagerInfo")]
        public ManagerInfo ManagerInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _managerInfo;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _managerInfo = value;
                }
            }
        }

        [DataMember(Name = "CreatorInfo")]
        public CreatorInfo CreatorInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _creatorInfo;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _creatorInfo = value;
                }
            }
        }

        [DataMember(Name = "SearchTreeItems")]
        public List<SearchTreeItem> SearchTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchTreeItems == null)
                        _searchTreeItems = new List<SearchTreeItem>();

                    return _searchTreeItems;
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

        #region IDeepClone<SearchTreeItem>

        public SearchTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchTreeItem)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "SearchTreeItem", Namespace = "http://Lair/Windows")]
    class SearchTreeItem : IDeepCloneable<SearchTreeItem>, IThisLock
    {
        private SearchItem _searchItem;
        private List<ChannelTreeItem> _channelTreeItems;
        private List<SearchTreeItem> _searchTreeItems;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

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

        [DataMember(Name = "ChannelTreeItems")]
        public List<ChannelTreeItem> ChannelTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_channelTreeItems == null)
                        _channelTreeItems = new List<ChannelTreeItem>();

                    return _channelTreeItems;
                }
            }
        }

        [DataMember(Name = "SearchTreeItems")]
        public List<SearchTreeItem> SearchTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchTreeItems == null)
                        _searchTreeItems = new List<SearchTreeItem>();

                    return _searchTreeItems;
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

        #region IDeepClone<SearchTreeItem>

        public SearchTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SearchTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SearchTreeItem)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "ChannelTreeItem", Namespace = "http://Lair/Windows")]
    class ChannelTreeItem : IDeepCloneable<ChannelTreeItem>, IThisLock
    {
        private Channel _channel;
        private bool _isFiltering;
        private LockedDictionary<Message, MessageState> _messages;

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

        [DataMember(Name = "IsFiltering")]
        public bool IsFiltering
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isFiltering;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isFiltering = value;
                }
            }
        }

        [DataMember(Name = "Messages")]
        public LockedDictionary<Message, MessageState> Messages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_messages == null)
                        _messages = new LockedDictionary<Message, MessageState>();

                    return _messages;
                }
            }
        }

        #region IDeepClone<ChannelTreeItem>

        public ChannelTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ChannelTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ChannelTreeItem)ds.ReadObject(textDictionaryReader);
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

    [DataContract(Name = "SearchItem", Namespace = "http://Lair/Windows")]
    class SearchItem : IDeepCloneable<SearchItem>, IThisLock
    {
        private string _name;
        private LockedList<SearchContains<string>> _searchWordCollection;
        private LockedList<SearchContains<SearchRegex>> _searchRegexCollection;
        private LockedList<SearchContains<string>> _searchSignatureCollection;
        private LockedList<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;

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

        public override string ToString()
        {
            lock (this.ThisLock)
            {
                return string.Format("Name = {0}", this.Name);
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
