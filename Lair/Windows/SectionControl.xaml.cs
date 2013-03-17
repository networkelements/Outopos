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
        private volatile bool _update = false;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

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
                _treeView.Items.Add(new SectionTreeViewItem(item));
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

            RichTextBoxHelper.SectionClickEvent += new SectionClickEventHandler(RichTextBoxHelper_SectionClickEvent);
            RichTextBoxHelper.ChannelClickEvent += new ChannelClickEventHandler(RichTextBoxHelper_ChannelClickEvent);
            RichTextBoxHelper.SeedClickEvent += new SeedClickEventHandler(RichTextBoxHelper_SeedClickEvent);
            RichTextBoxHelper.LinkClickEvent += new LinkClickEventHandler(RichTextBoxHelper_LinkClickEvent);

            RichTextBoxHelper.GetAnchorMessageEvent = new GetAnchorMessageEventHandler(RichTextBoxHelper_GetAnchorMessageEvent);

            RichTextBoxHelper.GetMaxHeightEvent = new GetMaxHeightEventHandler(RichTextBoxHelper_GetMaxHeightEvent);

            _searchRowDefinition.Height = new GridLength(0);

            _trustToggleButton.IsEnabled = false;
            _topicUploadToggleButton.IsEnabled = false;
            _messageUploadButton.IsEnabled = false;

            this.Update();
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

                            _trustToggleButton.IsEnabled = false;
                            _trustToggleButton.IsChecked = false;
                            _topicUploadToggleButton.IsEnabled = false;
                            _messageUploadButton.IsEnabled = false;
                        }
                        else if (_treeView.SelectedItem is ChannelTreeViewItem)
                        {
                            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
                            selectTreeViewItem = (ChannelTreeViewItem)_treeView.SelectedItem;

                            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == sectionTreeViewItem.Value.UploadSignature);

                            _trustToggleButton.IsEnabled = (selectTreeViewItem.CreatorSignature != null);
                            _trustToggleButton.IsChecked = selectTreeViewItem.Value.IsFilterEnabled;
                            _messageUploadButton.IsEnabled = (digitalSignature != null);

                            {
                                Topic topTopic = null;

                                if (selectTreeViewItem.CreatorSignature != null)
                                {
                                    Dictionary<string, Topic> topicDictionary = new Dictionary<string, Topic>();

                                    foreach (var topic in _lairManager.GetTopics(selectTreeViewItem.Value.Channel))
                                    {
                                        topicDictionary[topic.Certificate.ToString()] = topic;
                                    }

                                    topicDictionary.TryGetValue(selectTreeViewItem.CreatorSignature, out topTopic);
                                }

                                if ((digitalSignature != null && digitalSignature.ToString() == selectTreeViewItem.CreatorSignature && sectionTreeViewItem.Value.CreatorInfo.Channels.Contains(selectTreeViewItem.Value.Channel)))
                                {
                                    _topicUploadToggleButton.IsEnabled = true;

                                    if ((topTopic != null && topTopic.Content != null))
                                    {
                                        _topicUploadToggleButton.IsChecked = true;
                                    }
                                    else
                                    {
                                        _topicUploadToggleButton.IsChecked = false;
                                    }
                                }
                                else if ((topTopic != null && topTopic.Content != null))
                                {
                                    _topicUploadToggleButton.IsEnabled = true;
                                    _topicUploadToggleButton.IsChecked = false;
                                }
                                else
                                {
                                    _topicUploadToggleButton.IsEnabled = false;
                                    _topicUploadToggleButton.IsChecked = false;
                                }
                            }
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

                    lock (selectTreeViewItem.Value.ThisLock)
                    {
                        lock (selectTreeViewItem.Value.Messages.ThisLock)
                        {
                            foreach (var item in selectTreeViewItem.Value.Messages)
                            {
                                newList.Add(new MessageWrapper(item.Key, item.Value));
                            }

                            foreach (var item in selectTreeViewItem.Value.Messages.ToArray())
                            {
                                selectTreeViewItem.Value.Messages[item.Key] = item.Value & ~MessageState.New;
                            }
                        }
                    }

                    {
                        LockedHashSet<Message> lockedMessages;

                        if (Settings.Instance.Global_LockedMessages.TryGetValue(selectTreeViewItem.Value.Channel, out lockedMessages))
                        {
                            var tempList = new HashSet<Message>(lockedMessages);
                            tempList.ExceptWith(newList.Select(n => n.Value));

                            foreach (var message in tempList)
                            {
                                newList.Add(new MessageWrapper(message, 0));
                            }
                        }
                    }

                    List<SearchItem> searchItems = new List<SearchItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        searchItems.AddRange(_treeView.GetLineage(selectTreeViewItem).OfType<SearchTreeViewItem>().Select(n => n.Value.SearchItem));
                    }), null);

                    foreach (var item in searchItems)
                    {
                        SectionControl.Filter(ref newList, item);
                    }

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
                        if (!newList.Contains(item)) removeList.Add(item);
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

                            foreach (var item in removeList)
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
                                    var text = RichTextBoxHelper.GetMessageToString(item.Value).ToLower();

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

                        if (_listViewItemCollection.Count > 0)
                        {
                            _listView.UpdateLayout();
                            var topItem = _listViewItemCollection.LastOrDefault(n => n.State.HasFlag(MessageState.New));
                            if (topItem == null) topItem = _listViewItemCollection.FirstOrDefault();
                            if (topItem != null) _listView.ScrollIntoView(topItem);
                        }

                        if (App.SelectTab == "Section")
                            _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));

                        this.Update_TreeView_Color();
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
                    foreach (var sectionTreeViewItem in _treeView.Items.OfType<SectionTreeViewItem>())
                    {
                        if (sectionTreeViewItem.Value.LeaderSignature == null) continue;

                        var trustSignatures = new HashSet<string>();
                        var channelToCreatorSignatures = new Dictionary<Channel, string>();

                        {
                            Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                            foreach (var leader in _lairManager.GetLeaders(sectionTreeViewItem.Value.Section))
                            {
                                leaderDictionary[leader.Certificate.ToString()] = leader;
                            }

                            {
                                Leader leader;

                                if (leaderDictionary.TryGetValue(sectionTreeViewItem.Value.LeaderSignature, out leader))
                                {
                                    Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                                    foreach (var creator in _lairManager.GetCreators(sectionTreeViewItem.Value.Section))
                                    {
                                        creatorDictionary[creator.Certificate.ToString()] = creator;
                                    }

                                    foreach (var creatorSignature in leader.CreatorSignatures.Reverse())
                                    {
                                        Creator creator;

                                        if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                                        {
                                            foreach (var channel in creator.Channels)
                                            {
                                                channelToCreatorSignatures[channel] = creator.Certificate.ToString();
                                            }
                                        }
                                    }

                                    Dictionary<string, Manager> managerDictionary = new Dictionary<string, Manager>();

                                    foreach (var manager in _lairManager.GetManagers(sectionTreeViewItem.Value.Section))
                                    {
                                        managerDictionary[manager.Certificate.ToString()] = manager;
                                    }

                                    foreach (var managerSignature in leader.ManagerSignatures)
                                    {
                                        Manager manager;

                                        if (managerDictionary.TryGetValue(managerSignature, out manager))
                                        {
                                            trustSignatures.UnionWith(manager.TrustSignatures);
                                        }
                                    }
                                }
                            }
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
                            var oldList = new HashSet<Message>();

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                oldList.UnionWith(selectTreeViewItem.Value.Messages.Keys);
                            }), null);

                            HashSet<Message> newList = new HashSet<Message>(_lairManager.GetMessages(selectTreeViewItem.Value.Channel));
                            newList.UnionWith(oldList);

                            List<Message> sortList = new List<Message>();

                            {
                                var tempList = new List<Message>();

                                if (channelToCreatorSignatures.ContainsKey(selectTreeViewItem.Value.Channel) && selectTreeViewItem.Value.IsFilterEnabled)
                                {
                                    foreach (var message in newList)
                                    {
                                        if (!trustSignatures.Contains(message.Certificate.ToString())) continue;

                                        tempList.Add(message);
                                    }
                                }
                                else
                                {
                                    foreach (var message in newList)
                                    {
                                        tempList.Add(message);
                                    }
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

                                tempList = tempList.Skip(tempList.Count - 1024).ToList();
                                tempList.Reverse();

                                sortList = tempList.ToList();

                                newList.Clear();
                                newList.UnionWith(sortList);
                            }

                            var removeList = new List<Message>();
                            var addList = new List<Message>();

                            foreach (var item in oldList)
                            {
                                if (!newList.Contains(item)) removeList.Add(item);
                            }

                            foreach (var item in sortList)
                            {
                                if (!oldList.Contains(item)) addList.Add(item);
                            }

                            lock (selectTreeViewItem.Value.ThisLock)
                            {
                                lock (selectTreeViewItem.Value.Messages.ThisLock)
                                {
                                    foreach (var item in addList)
                                    {
                                        selectTreeViewItem.Value.Messages.Add(item, MessageState.New);
                                    }

                                    foreach (var item in removeList)
                                    {
                                        selectTreeViewItem.Value.Messages.Remove(item);
                                    }
                                }
                            }

                            this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                            {
                                if (channelToCreatorSignatures.ContainsKey(selectTreeViewItem.Value.Channel))
                                {
                                    selectTreeViewItem.CreatorSignature = channelToCreatorSignatures[selectTreeViewItem.Value.Channel];
                                }
                                else
                                {
                                    selectTreeViewItem.CreatorSignature = null;
                                }

                                if (addList.Count != 0 || removeList.Count != 0)
                                {
                                    selectTreeViewItem.Update();
                                }
                            }), null);
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                    {
                        if (_update)
                        {
                            _update = false;
                            this.Update();
                        }
                        else
                        {
                            this.Update_TreeView_Color();
                        }
                    }), null);

                    _autoResetEvent.WaitOne(1000 * 60);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        void RichTextBoxHelper_SectionClickEvent(object sender, Section section)
        {
            if (section.Name == null || section.Id == null) return;

            Settings.Instance.Global_SectionHistorys.Add(section);

            HashSet<Section> sectionList = new HashSet<Section>();

            {
                List<TreeViewItem> itemList = new List<TreeViewItem>();
                itemList.AddRange(_treeView.Items.Cast<TreeViewItem>());

                for (int i = 0; i < itemList.Count; i++)
                {
                    itemList.AddRange(itemList[i].Items.Cast<TreeViewItem>());
                }

                foreach (var item in itemList.OfType<SectionTreeViewItem>())
                {
                    sectionList.Add(item.Value.Section);
                }
            }

            if (sectionList.Contains(section)) return;

            var sectionTreeItem = new SectionTreeItem();
            sectionTreeItem.Section = section;
            sectionTreeItem.LeaderInfo = new LeaderInfo();
            sectionTreeItem.CreatorInfo = new CreatorInfo();
            sectionTreeItem.ManagerInfo = new ManagerInfo();

            _treeView.Items.Add(new SectionTreeViewItem(sectionTreeItem));
        }

        void RichTextBoxHelper_ChannelClickEvent(object sender, Channel channel)
        {
            var selectChannelTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectChannelTreeViewItem == null) return;

            if (channel.Name == null || channel.Id == null) return;

            Settings.Instance.Global_ChannelHistorys.Add(channel);

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

        Message RichTextBoxHelper_GetAnchorMessageEvent(object sender, Channel channel, Key key)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return null;
            if (selectTreeViewItem.Value.Channel != channel) return null;

            foreach (var message in selectTreeViewItem.Value.Messages.Keys)
            {
                if (message.VerifyHash(key.Hash, key.HashAlgorithm))
                {
                    return message;
                }
            }

            {
                LockedHashSet<Message> lockedMessages;

                if (Settings.Instance.Global_LockedMessages.TryGetValue(selectTreeViewItem.Value.Channel, out lockedMessages))
                {
                    foreach (var message in lockedMessages)
                    {
                        if (message.VerifyHash(key.Hash, key.HashAlgorithm))
                        {
                            return message;
                        }
                    }
                }
            }

            return null;
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
                                if (searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Certificate.ToString());

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
                                if (!searchContains.Contains) return searchContains.Value.IsMatch(item.Value.Certificate.ToString());

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

            foreach (var item in _treeView.Items.OfType<SectionTreeViewItem>())
            {
                item.Sort();
            }

            this.Update_TreeView_Color();

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
        }

        private void Update_Cache()
        {
            _update = true;
            _autoResetEvent.Set();
        }

        private void Update_TreeView_Color()
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;

            {
                var items = new List<TreeViewItem>();
                items.AddRange(_treeView.Items.OfType<TreeViewItem>());

                for (int i = 0; i < items.Count; i++)
                {
                    foreach (TreeViewItem item in items[i].Items)
                    {
                        items.Add(item);
                    }
                }

                var hitItems = new HashSet<TreeViewItem>();

                foreach (var item in items.OfType<ChannelTreeViewItem>().Where(n => n.Value.Messages.Any(m => m.Value.HasFlag(MessageState.New))))
                {
                    hitItems.UnionWith(_treeView.GetLineage(item));
                }

                foreach (var item in items)
                {
                    var textBlock = (TextBlock)item.Header;

                    if (hitItems.Contains(item))
                    {
                        textBlock.FontWeight = FontWeights.ExtraBlack;

                        if (selectTreeViewItem != item)
                        {
                            textBlock.Foreground = new SolidColorBrush(Settings.Instance.Color_Tree_Hit);
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

        public void Refresh()
        {
            _listView.Items.Refresh();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _lairManager.RemoveSectionsEvent = _lairManager_RemoveSectionsEvent;
            _lairManager.RemoveLeadersEvent = _lairManager_RemoveLeadersEvent;
            _lairManager.RemoveCreatorsEvent = _lairManager_RemoveCreatorsEvent;
            _lairManager.RemoveManagersEvent = _lairManager_RemoveManagersEvent;

            _lairManager.RemoveChannelsEvent = _lairManager_RemoveChannelsEvent;
            _lairManager.RemoveTopicsEvent = _lairManager_RemoveTopicsEvent;
            _lairManager.RemoveMessagesEvent = _lairManager_RemoveMessagesEvent;
        }

        // 閲覧中のSectionを除外
        SectionCollection _lairManager_RemoveSectionsEvent(object sender)
        {
            HashSet<Section> lockedSections = new HashSet<Section>();

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems)
            {
                lockedSections.Add(item.Section);
            }

            HashSet<Section> removeSections = new HashSet<Section>(_lairManager.GetSections());
            removeSections.ExceptWith(lockedSections);

            int capacity = Math.Max(lockedSections.Count * 2, 256);
            return new SectionCollection(removeSections.Randomize().Skip(capacity));
        }

        // 閲覧中のSectionのLeaderを除外
        SignatureCollection _lairManager_RemoveLeadersEvent(object sender, Section section)
        {
            HashSet<string> lockedLeaderSignatures = new HashSet<string>();

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems.Where(n => n.Section == section))
            {
                if (item.LeaderSignature == null) continue;

                lockedLeaderSignatures.Add(item.LeaderSignature);
            }

            HashSet<string> removeLeaderSignatures = new HashSet<string>(_lairManager.GetLeaders(section).Select(n => n.Certificate.ToString()));
            removeLeaderSignatures.ExceptWith(lockedLeaderSignatures);

            int capacity = Math.Max(lockedLeaderSignatures.Count * 2, 256);
            return new SignatureCollection(removeLeaderSignatures.Randomize().Skip(capacity));
        }

        // 閲覧中のSectionのCreatorを除外
        SignatureCollection _lairManager_RemoveCreatorsEvent(object sender, Section section)
        {
            HashSet<string> lockedCreatorSignatures = new HashSet<string>();

            Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

            foreach (var leader in _lairManager.GetLeaders(section))
            {
                leaderDictionary[leader.Certificate.ToString()] = leader;
            }

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems.Where(n => n.Section == section))
            {
                if (item.LeaderSignature == null) continue;

                Leader leader;

                if (leaderDictionary.TryGetValue(item.LeaderSignature, out leader))
                {
                    lockedCreatorSignatures.UnionWith(leader.CreatorSignatures);
                }
            }

            HashSet<string> removeCreatorSignatures = new HashSet<string>(_lairManager.GetCreators(section).Select(n => n.Certificate.ToString()));
            removeCreatorSignatures.ExceptWith(lockedCreatorSignatures);

            int capacity = Math.Max(lockedCreatorSignatures.Count * 2, 256);
            return new SignatureCollection(removeCreatorSignatures.Randomize().Skip(capacity));
        }

        // 閲覧中のSectionのManagerを除外
        SignatureCollection _lairManager_RemoveManagersEvent(object sender, Section section)
        {
            HashSet<string> lockedManagerSignatures = new HashSet<string>();

            Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

            foreach (var leader in _lairManager.GetLeaders(section))
            {
                leaderDictionary[leader.Certificate.ToString()] = leader;
            }

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems.Where(n => n.Section == section))
            {
                if (item.LeaderSignature == null) continue;

                Leader leader;

                if (leaderDictionary.TryGetValue(item.LeaderSignature, out leader))
                {
                    lockedManagerSignatures.UnionWith(leader.ManagerSignatures);
                }
            }

            HashSet<string> removeManagerSignatures = new HashSet<string>(_lairManager.GetManagers(section).Select(n => n.Certificate.ToString()));
            removeManagerSignatures.ExceptWith(lockedManagerSignatures);

            int capacity = Math.Max(lockedManagerSignatures.Count * 2, 256);
            return new SignatureCollection(removeManagerSignatures.Randomize().Skip(capacity));
        }

        // 閲覧中のChannelを除外
        ChannelCollection _lairManager_RemoveChannelsEvent(object sender)
        {
            HashSet<Channel> lockedChannels = new HashSet<Channel>();

            {
                List<SearchTreeItem> searchTreeItems = new List<SearchTreeItem>();

                foreach (var item in Settings.Instance.SectionControl_SectionTreeItems)
                {
                    searchTreeItems.AddRange(item.SearchTreeItems);
                }

                for (int i = 0; i < searchTreeItems.Count; i++)
                {
                    searchTreeItems.AddRange(searchTreeItems[i].SearchTreeItems);
                    lockedChannels.UnionWith(searchTreeItems[i].ChannelTreeItems.Select(n => n.Channel));
                }
            }

            HashSet<Channel> removeChannels = new HashSet<Channel>(_lairManager.GetChannels());
            removeChannels.ExceptWith(lockedChannels);

            int capacity = Math.Max(lockedChannels.Count * 2, 256);
            return new ChannelCollection(removeChannels.Randomize().Skip(capacity));
        }

        // 閲覧中のSectionのCreatorが対象のChannelを作成していた場合、Creatorが作成したTopicを除外
        SignatureCollection _lairManager_RemoveTopicsEvent(object sender, Channel channel)
        {
            HashSet<string> lockedTopicSignatures = new HashSet<string>();

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems)
            {
                if (item.LeaderSignature == null) continue;

                Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                foreach (var leader in _lairManager.GetLeaders(item.Section))
                {
                    leaderDictionary[leader.Certificate.ToString()] = leader;
                }

                {
                    Leader leader;

                    if (leaderDictionary.TryGetValue(item.LeaderSignature, out leader))
                    {
                        Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                        foreach (var creator in _lairManager.GetCreators(item.Section))
                        {
                            creatorDictionary[creator.Certificate.ToString()] = creator;
                        }

                        foreach (var creatorSignature in leader.CreatorSignatures)
                        {
                            Creator creator;

                            if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                            {
                                HashSet<Channel> channels = new HashSet<Channel>(creator.Channels);

                                if (channels.Contains(channel))
                                {
                                    lockedTopicSignatures.Add(creator.Certificate.ToString());
                                }
                            }
                        }
                    }
                }
            }

            HashSet<string> removeTopicSignatures = new HashSet<string>(_lairManager.GetTopics(channel).Select(n => n.Certificate.ToString()));
            removeTopicSignatures.ExceptWith(lockedTopicSignatures);

            int capacity = Math.Max(lockedTopicSignatures.Count * 2, 256);
            return new SignatureCollection(removeTopicSignatures.Randomize().Skip(capacity));
        }

        // 閲覧中のSectionのCreatorが対象のChannelを作成していた場合、ManagerがTrust認定したMessageを除外
        MessageCollection _lairManager_RemoveMessagesEvent(object sender, Channel channel)
        {
            HashSet<string> trustSignatures = new HashSet<string>();

            foreach (var item in Settings.Instance.SectionControl_SectionTreeItems)
            {
                if (item.LeaderSignature == null) continue;

                Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                foreach (var leader in _lairManager.GetLeaders(item.Section))
                {
                    leaderDictionary[leader.Certificate.ToString()] = leader;
                }

                {
                    Leader leader;

                    if (leaderDictionary.TryGetValue(item.LeaderSignature, out leader))
                    {
                        Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                        foreach (var creator in _lairManager.GetCreators(item.Section))
                        {
                            creatorDictionary[creator.Certificate.ToString()] = creator;
                        }

                        HashSet<Channel> channels = new HashSet<Channel>();

                        foreach (var creatorSignature in leader.CreatorSignatures)
                        {
                            Creator creator;

                            if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                            {
                                channels.UnionWith(creator.Channels);
                            }
                        }

                        if (channels.Contains(channel))
                        {
                            Dictionary<string, Manager> managerDictionary = new Dictionary<string, Manager>();

                            foreach (var manager in _lairManager.GetManagers(item.Section))
                            {
                                managerDictionary[manager.Certificate.ToString()] = manager;
                            }

                            foreach (var managerSignature in leader.ManagerSignatures)
                            {
                                Manager manager;

                                if (managerDictionary.TryGetValue(managerSignature, out manager))
                                {
                                    trustSignatures.UnionWith(manager.TrustSignatures);
                                }
                            }
                        }
                    }
                }
            }

            HashSet<Message> removeMessages = new HashSet<Message>(_lairManager.GetMessages(channel));

            List<Message> trustMessages = new List<Message>();
            List<Message> randomMessage = new List<Message>();

            foreach (var message in removeMessages)
            {
                if (trustSignatures.Contains(message.Certificate.ToString()))
                {
                    trustMessages.Add(message);
                }
                else
                {
                    randomMessage.Add(message);
                }
            }

            trustMessages.Sort(new Comparison<Message>((Message x, Message y) =>
            {
                int c = x.CreationTime.CompareTo(y.CreationTime);
                if (c != 0) return c;
                c = x.Content.CompareTo(y.Content);
                if (c != 0) return c;
                c = Collection.Compare(x.GetHash(HashAlgorithm.Sha512), y.GetHash(HashAlgorithm.Sha512));
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            }));
            trustMessages = trustMessages.Skip(trustMessages.Count - 128).ToList();
            trustMessages.Reverse();

            randomMessage = randomMessage.Randomize().Take(256 - trustMessages.Count).ToList();

            removeMessages.ExceptWith(trustMessages);
            removeMessages.ExceptWith(randomMessage);

            return new MessageCollection(removeMessages);
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
                    if (_treeView.SelectedItem is SearchTreeViewItem)
                    {
                        DataObject data = new DataObject("SearchTreeViewItem", _treeView.SelectedItem);
                        DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                    }
                    else if (_treeView.SelectedItem is ChannelTreeViewItem)
                    {
                        DataObject data = new DataObject("ChannelTreeViewItem", _treeView.SelectedItem);
                        DragDrop.DoDragDrop(_treeView, data, DragDropEffects.Move);
                    }
                }
            }
        }

        private void _treeView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SearchTreeViewItem"))
            {
                var s = (SearchTreeViewItem)e.Data.GetData("SearchTreeViewItem");

                var currentItem = _treeView.GetCurrentItem(e.GetPosition);

                if (currentItem is SectionTreeViewItem)
                {
                    var t = (SectionTreeViewItem)currentItem;
                    if (t.Value.SearchTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                    if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                    var list = _treeView.GetLineage(s).OfType<TreeViewItem>().ToList();

                    t.IsSelected = true;

                    if (list[list.Count - 2] is SectionTreeViewItem)
                    {
                        var target = (SectionTreeViewItem)list[list.Count - 2];

                        var tItems = target.Value.SearchTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                        target.Value.SearchTreeItems.Clear();
                        target.Value.SearchTreeItems.AddRange(tItems);

                        target.Update();
                    }
                    else if (list[list.Count - 2] is SearchTreeViewItem)
                    {
                        var target = (SearchTreeViewItem)list[list.Count - 2];

                        var tItems = target.Value.SearchTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                        target.Value.SearchTreeItems.Clear();
                        target.Value.SearchTreeItems.AddRange(tItems);

                        target.Update();
                    }

                    t.Value.SearchTreeItems.Add(s.Value);
                    t.Update();
                }
                else if (currentItem is SearchTreeViewItem)
                {
                    var t = (SearchTreeViewItem)currentItem;
                    if (t.Equals(s) || t.Value.SearchTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                    if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                    var list = _treeView.GetLineage(s).OfType<TreeViewItem>().ToList();

                    t.IsSelected = true;

                    if (list[list.Count - 2] is SectionTreeViewItem)
                    {
                        var target = (SectionTreeViewItem)list[list.Count - 2];

                        var tItems = target.Value.SearchTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                        target.Value.SearchTreeItems.Clear();
                        target.Value.SearchTreeItems.AddRange(tItems);

                        target.Update();
                    }
                    else if (list[list.Count - 2] is SearchTreeViewItem)
                    {
                        var target = (SearchTreeViewItem)list[list.Count - 2];

                        var tItems = target.Value.SearchTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                        target.Value.SearchTreeItems.Clear();
                        target.Value.SearchTreeItems.AddRange(tItems);

                        target.Update();
                    }

                    t.Value.SearchTreeItems.Add(s.Value);
                    t.Update();
                }
            }
            else if (e.Data.GetDataPresent("ChannelTreeViewItem"))
            {
                var s = (ChannelTreeViewItem)e.Data.GetData("ChannelTreeViewItem");
                var t = _treeView.GetCurrentItem(e.GetPosition) as SearchTreeViewItem;

                if (t == null || t.Equals(s) || t.Value.ChannelTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                if (_treeView.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                var list = _treeView.GetLineage((TreeViewItem)s).OfType<TreeViewItem>().ToList();

                t.IsSelected = true;

                {
                    var target = (SearchTreeViewItem)list[list.Count - 2];

                    var tItems = target.Value.ChannelTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                    target.Value.ChannelTreeItems.Clear();
                    target.Value.ChannelTreeItems.AddRange(tItems);

                    target.Update();
                }

                t.Value.ChannelTreeItems.Add(s.Value);
                t.Update();
            }

            this.Update_Cache();
        }

        private void _treeView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

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
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as TreeViewItem;

            this.Update();
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            {
                var sections = Clipboard.GetSections();

                _treeViewPasteMenuItem.IsEnabled = sections.Count() > 0 ? true : false;
            }
        }

        private void _treeViewNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            NewSectionWindow window = new NewSectionWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                var sectionTreeItem = new SectionTreeItem();
                sectionTreeItem.Section = window.Section;
                sectionTreeItem.LeaderInfo = new LeaderInfo();
                sectionTreeItem.CreatorInfo = new CreatorInfo();
                sectionTreeItem.ManagerInfo = new ManagerInfo();

                _treeView.Items.Add(new SectionTreeViewItem(sectionTreeItem));
            }

            this.Update();
        }

        private void _treeViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (var section in Clipboard.GetSections())
            {
                var sectionTreeItem = new SectionTreeItem();
                sectionTreeItem.Section = section;
                sectionTreeItem.LeaderInfo = new LeaderInfo();
                sectionTreeItem.CreatorInfo = new CreatorInfo();
                sectionTreeItem.ManagerInfo = new ManagerInfo();

                _treeView.Items.Add(new SectionTreeViewItem(sectionTreeItem));
            }

            this.Update();
        }

        private void _treeViewImportLockedMessagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
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

                                    LockedHashSet<Message> hashset;

                                    if (!Settings.Instance.Global_LockedMessages.TryGetValue(message.Channel, out hashset))
                                    {
                                        hashset = new LockedHashSet<Message>();
                                        Settings.Instance.Global_LockedMessages[message.Channel] = hashset;
                                    }

                                    hashset.Add(message);
                                }
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }

                    this.Update();
                }
            }
        }

        private void _sectionTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as SectionTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            {
                var sectionTreeViewItemPasteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_sectionTreeViewItemPasteMenuItem");
                if (sectionTreeViewItemPasteMenuItem == null) return;

                var sectionTreeItems = Clipboard.GetSearchTreeItems();

                sectionTreeViewItemPasteMenuItem.IsEnabled = sectionTreeItems.Count() > 0 ? true : false;
            }

            {
                var sectionTreeViewItemChartMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_sectionTreeViewItemChartMenuItem");
                if (sectionTreeViewItemChartMenuItem == null) return;

                sectionTreeViewItemChartMenuItem.IsEnabled = (selectTreeViewItem.Value.LeaderSignature != null);
            }
        }

        private void _sectionTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchTreeItem = new SearchTreeItem();
            searchTreeItem.SearchItem = new SearchItem();

            var searchItem = searchTreeItem.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.SearchTreeItems.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _sectionTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            SectionTreeItemEditWindow window = new SectionTreeItemEditWindow(selectTreeViewItem.Value, _lairManager, _bufferManager);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            _treeView.Items.Remove(selectTreeViewItem);

            this.Update();
        }

        private void _sectionTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSections(new Section[] { selectTreeViewItem.Value.Section });
        }

        private void _sectionTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(LairConverter.ToSectionString(selectTreeViewItem.Value.Section));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value));
            sb.AppendLine();

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _sectionTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetSearchTreeItems())
            {
                selectTreeViewItem.Value.SearchTreeItems.Add(item);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _sectionTreeViewItemChartMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            ChartWindow window = new ChartWindow(selectTreeViewItem.Value.Section, selectTreeViewItem.Value.LeaderSignature, _lairManager);
            window.Owner = _mainWindow;
            window.ShowDialog();

            this.Update();
        }

        private void _searchTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            var selectTreeViewItem = sender as SearchTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            {
                var searchTreeViewItemPasteMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_searchTreeViewItemPasteMenuItem");
                if (searchTreeViewItemPasteMenuItem == null) return;

                var searchTreeItems = Clipboard.GetSearchTreeItems();
                var channels = Clipboard.GetChannels();

                searchTreeViewItemPasteMenuItem.IsEnabled = (searchTreeItems.Count() + channels.Count()) > 0 ? true : false;
            }

            {
                var searchTreeViewItemChannelListMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_searchTreeViewItemChannelListMenuItem");
                if (searchTreeViewItemChannelListMenuItem == null) return;

                searchTreeViewItemChannelListMenuItem.IsEnabled = (sectionTreeViewItem.Value.LeaderSignature != null);
            }
        }

        private void _searchTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchTreeItem = new SearchTreeItem();
            searchTreeItem.SearchItem = new SearchItem();

            var searchItem = searchTreeItem.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.SearchTreeItems.Add(searchTreeItem);

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _searchTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            var searchItem = selectTreeViewItem.Value.SearchItem;
            SearchItemEditWindow window = new SearchItemEditWindow(ref searchItem);
            window.Owner = _mainWindow;
            window.ShowDialog();

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _searchTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var list = _treeView.GetLineage(selectTreeViewItem).Cast<TreeViewItem>().ToList();

            if (list[list.Count - 2] is SectionTreeViewItem)
            {
                var target = (SectionTreeViewItem)list[list.Count - 2];

                target.IsSelected = true;

                target.Value.SearchTreeItems.Remove(selectTreeViewItem.Value);
                target.Update();
            }
            else if (list[list.Count - 2] is SearchTreeViewItem)
            {
                var target = (SearchTreeViewItem)list[list.Count - 2];

                target.IsSelected = true;

                target.Value.SearchTreeItems.Remove(selectTreeViewItem.Value);
                target.Update();
            }

            this.Update();
        }

        private void _searchTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new SearchTreeItem[] { selectTreeViewItem.Value });

            var list = _treeView.GetLineage(selectTreeViewItem).Cast<TreeViewItem>().ToList();

            if (list[list.Count - 2] is SectionTreeViewItem)
            {
                var target = (SectionTreeViewItem)list[list.Count - 2];

                target.IsSelected = true;

                target.Value.SearchTreeItems.Remove(selectTreeViewItem.Value);
                target.Update();
            }
            else if (list[list.Count - 2] is SearchTreeViewItem)
            {
                var target = (SearchTreeViewItem)list[list.Count - 2];

                target.IsSelected = true;

                target.Value.SearchTreeItems.Remove(selectTreeViewItem.Value);
                target.Update();
            }

            this.Update();
        }

        private void _searchTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSearchTreeItems(new SearchTreeItem[] { selectTreeViewItem.Value });
        }

        private void _searchTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetSearchTreeItems())
            {
                selectTreeViewItem.Value.SearchTreeItems.Add(item);
            }

            foreach (var item in Clipboard.GetChannelTreeItems())
            {
                selectTreeViewItem.Value.ChannelTreeItems.Add(item);
            }

            foreach (var channel in Clipboard.GetChannels())
            {
                if (selectTreeViewItem.Value.ChannelTreeItems.Any(n => n.Channel == channel)) continue;

                var channelTreeItem = new ChannelTreeItem();
                channelTreeItem.Channel = channel;

                selectTreeViewItem.Value.ChannelTreeItems.Add(channelTreeItem);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _searchTreeViewItemChannelListMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as SearchTreeViewItem;
            if (selectTreeViewItem == null) return;

            HashSet<Channel> channels = new HashSet<Channel>();

            if (sectionTreeViewItem.Value.LeaderSignature != null)
            {
                Dictionary<string, Leader> leaderDictionary = new Dictionary<string, Leader>();

                foreach (var leader in _lairManager.GetLeaders(sectionTreeViewItem.Value.Section))
                {
                    leaderDictionary[leader.Certificate.ToString()] = leader;
                }

                {
                    Leader leader;

                    if (leaderDictionary.TryGetValue(sectionTreeViewItem.Value.LeaderSignature, out leader))
                    {
                        Dictionary<string, Creator> creatorDictionary = new Dictionary<string, Creator>();

                        foreach (var creator in _lairManager.GetCreators(sectionTreeViewItem.Value.Section))
                        {
                            creatorDictionary[creator.Certificate.ToString()] = creator;
                        }

                        foreach (var creatorSignature in leader.CreatorSignatures)
                        {
                            Creator creator;

                            if (creatorDictionary.TryGetValue(creatorSignature, out creator))
                            {
                                channels.UnionWith(creator.Channels);
                            }
                        }
                    }
                }
            }

            {
                List<SearchTreeItem> searchTreeItems = new List<SearchTreeItem>(sectionTreeViewItem.Value.SearchTreeItems);

                for (int i = 0; i < searchTreeItems.Count; i++)
                {
                    searchTreeItems.AddRange(searchTreeItems[i].SearchTreeItems);
                    channels.ExceptWith(searchTreeItems[i].ChannelTreeItems.Select(n => n.Channel));
                }
            }

            ChannelListWindow window = new ChannelListWindow(channels, _lairManager);
            window.Owner = _mainWindow;

            window.ChannelJoinEvent += (object sender2, Channel channel) =>
            {
                var channelTreeItem = new ChannelTreeItem();
                channelTreeItem.Channel = channel;

                selectTreeViewItem.Value.ChannelTreeItems.Add(channelTreeItem);

                selectTreeViewItem.Update();
                this.Update_Cache();
            };

            window.ShowDialog();
        }

        private void _channelTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as ChannelTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

            {
                var channelTreeItemTreeViewItemExportLockedMessagesMenuItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(n => n.Name == "_channelTreeItemTreeViewItemExportLockedMessagesMenuItem");
                if (channelTreeItemTreeViewItemExportLockedMessagesMenuItem == null) return;

                channelTreeItemTreeViewItemExportLockedMessagesMenuItem.IsEnabled = Settings.Instance.Global_LockedMessages.ContainsKey(selectTreeViewItem.Value.Channel);
            }
        }

        private void _channelTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SearchTreeViewItem>().LastOrDefault() as SearchTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.ChannelTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _channelTreeItemTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChannelTreeItems(new ChannelTreeItem[] { selectTreeViewItem.Value });

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SearchTreeViewItem>().LastOrDefault() as SearchTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.ChannelTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _channelTreeItemTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChannelTreeItems(new ChannelTreeItem[] { selectTreeViewItem.Value });
        }

        private void _channelTreeItemTreeViewItemExportLockedMessagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            LockedHashSet<Message> hashSet;
            if (!Settings.Instance.Global_LockedMessages.TryGetValue(selectTreeViewItem.Value.Channel, out hashSet)) return;

            var tempList = hashSet.ToList();

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
                    using (Stream directoryStream = LairConverter.ToMessagesStream(tempList))
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

        #region Tool

        private void _trustToggleButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsFilterEnabled = _trustToggleButton.IsChecked.Value;

            _listViewItemCollection.Clear();

            this.Update_Cache();
        }


        private void _topicUploadToggleButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            Topic topTopic = null;

            if (selectTreeViewItem.CreatorSignature != null)
            {
                Dictionary<string, Topic> topicDictionary = new Dictionary<string, Topic>();

                foreach (var topic in _lairManager.GetTopics(selectTreeViewItem.Value.Channel))
                {
                    topicDictionary[topic.Certificate.ToString()] = topic;
                }

                topicDictionary.TryGetValue(selectTreeViewItem.CreatorSignature, out topTopic);
            }

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == sectionTreeViewItem.Value.UploadSignature);

            if ((digitalSignature != null && digitalSignature.ToString() == selectTreeViewItem.CreatorSignature && sectionTreeViewItem.Value.CreatorInfo.Channels.Contains(selectTreeViewItem.Value.Channel)))
            {
                var content = (topTopic != null) ? (topTopic.Content ?? "") : "";

                TopicEditWindow window = new TopicEditWindow(selectTreeViewItem.Value.Channel, content, digitalSignature, _lairManager);
                window.Owner = _mainWindow;

                window.ShowDialog();
            }
            else
            {
                TopicPreviewWindow window = new TopicPreviewWindow(topTopic);
                window.Owner = _mainWindow;

                window.ShowDialog();
            }

            this.Update();

            e.Handled = true;
        }
        
        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            MessageEditWindow window = new MessageEditWindow(selectTreeViewItem.Value.Channel, "", null, digitalSignature, _lairManager);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                this.Update_Cache();
            }
        }

        #endregion

        #region _listView

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

        private void _listView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listView);
            var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

            _gridViewColumn.Width = Math.Max(0, _listView.ActualWidth - 21);

            _listView.Items.Refresh();
        }

        private void _richTextBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

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

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == sectionTreeViewItem.Value.UploadSignature);

            foreach (var item in list)
            {
                if (item.Name == "_richTextBoxFilterWordMenuItem")
                {
                    item.IsEnabled = !richTextBox.Selection.IsEmpty;
                }
                else if (item.Name == "_richTextBoxLockMenuItem")
                {
                    item.IsEnabled = (_listView.SelectedItems.Count != 0);
                }
                else if (item.Name == "_richTextBoxResponsMenuItem")
                {
                    item.IsEnabled = (digitalSignature != null);
                }
            }
        }

        private void _listView_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftCtrl)
                    || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightCtrl))
                {
                    var index = _listView.GetCurrentIndex(e.GetPosition);
                    if (index == -1) return;

                    var selectItem = _listViewItemCollection[index];

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

                    var selectItem = _listViewItemCollection[index];

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

                var selectItem = _listViewItemCollection[index];

                if (!_listView.SelectedItems.Contains(selectItem))
                {
                    _listView.SelectedItems.Clear();
                    _listView.SelectedItems.Add(selectItem);
                }
            }
        }

        private void _richTextBoxResponsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == sectionTreeViewItem.Value.UploadSignature);
            if (digitalSignature == null) return;

            var responsMessages = _listView.SelectedItems.OfType<MessageWrapper>().Select(n => n.Value).ToList();

            MessageEditWindow window = new MessageEditWindow(selectTreeViewItem.Value.Channel, "", responsMessages, digitalSignature, _lairManager);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                this.Update_Cache();
            }
        }

        private void _richTextBoxCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            string text = richTextBox.Selection.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                text = richTextBox.ToString();
            }

            Clipboard.SetText(text);
        }

        private void _richTextBoxTrustMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sectionTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SectionTreeViewItem>().FirstOrDefault() as SectionTreeViewItem;
            if (sectionTreeViewItem == null) return;

            foreach (var item in _listView.SelectedItems.OfType<MessageWrapper>())
            {
                sectionTreeViewItem.Value.ManagerInfo.TrustSignatures.Add(item.Value.Certificate.ToString());
            }
        }

        private void _richTextBoxLockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            LockedHashSet<Message> messages;

            if (!Settings.Instance.Global_LockedMessages.TryGetValue(selectTreeViewItem.Value.Channel, out messages))
            {
                messages = new LockedHashSet<Message>();
                Settings.Instance.Global_LockedMessages[selectTreeViewItem.Value.Channel] = messages;
            }

            foreach (var item in _listView.SelectedItems.OfType<MessageWrapper>())
            {
                messages.Add(item.Value);
            }
        }

        private void _richTextBoxUnlockThisMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            LockedHashSet<Message> messages;

            if (!Settings.Instance.Global_LockedMessages.TryGetValue(selectTreeViewItem.Value.Channel, out messages))
            {
                messages = new LockedHashSet<Message>();
                Settings.Instance.Global_LockedMessages[selectTreeViewItem.Value.Channel] = messages;
            }

            foreach (var item in _listView.SelectedItems.OfType<MessageWrapper>())
            {
                messages.Remove(item.Value);
            }
        }

        private void _richTextBoxFilterWordMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var richTextBox = (((e.Source as MenuItem).Parent as MenuItem).Parent as ContextMenu).PlacementTarget as RichTextBox;
            if (richTextBox == null) return;

            if (richTextBox.Selection.IsEmpty) return;

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SearchTreeViewItem>().LastOrDefault() as SearchTreeViewItem;
            if (searchTreeViewItem == null) return;

            var text = richTextBox.Selection.Text.Trim('\r', '\n');
            if (text.Length == 0) return;

            if (text == text.Replace("\r", "").Replace("\n", ""))
            {
                var item = new SearchContains<string>()
                {
                    Contains = false,
                    Value = text,
                };

                if (searchTreeViewItem.Value.SearchItem.SearchWordCollection.Contains(item)) return;
                searchTreeViewItem.Value.SearchItem.SearchWordCollection.Add(item);
            }
            else
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = false,
                    Value = new SearchRegex() { IsIgnoreCase = true, Value = Regex.Escape(text) },
                };

                if (searchTreeViewItem.Value.SearchItem.SearchRegexCollection.Contains(item)) return;
                searchTreeViewItem.Value.SearchItem.SearchRegexCollection.Add(item);
            }

            this.Update();
        }

        private void _richTextBoxFilterSignatureMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectListViewItems = _listView.SelectedItems;
            if (selectListViewItems == null) return;

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<SearchTreeViewItem>().LastOrDefault() as SearchTreeViewItem;
            if (searchTreeViewItem == null) return;

            foreach (var listItem in selectListViewItems.Cast<MessageWrapper>())
            {
                var signature = listItem.Value.Certificate.ToString();

                var item = new SearchContains<SearchRegex>()
                {
                    Contains = false,
                    Value = new SearchRegex()
                    {
                        IsIgnoreCase = false,
                        Value = Regex.Escape(signature),
                    },
                };

                if (searchTreeViewItem.Value.SearchItem.SearchSignatureCollection.Contains(item)) continue;
                searchTreeViewItem.Value.SearchItem.SearchSignatureCollection.Add(item);
            }

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

        private void _searchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            _treeViewNewMenuItem_Click(null, null);
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SearchTreeViewItem)
            {
                _searchTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SearchTreeViewItem)
            {
                _searchTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionTreeViewItem)
            {

            }
            else if (_treeView.SelectedItem is SearchTreeViewItem)
            {
                _searchTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (Clipboard.GetSections() != null)
            {
                _treeViewPasteMenuItem_Click(null, null);
            }
            else
            {
                if (_treeView.SelectedItem is SectionTreeViewItem)
                {
                    _sectionTreeViewItemPasteMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is SearchTreeViewItem)
                {
                    _searchTreeViewItemPasteMenuItem_Click(null, null);
                }
                else if (_treeView.SelectedItem is ChannelTreeViewItem)
                {

                }
            }
        }

        private void Execute_Search(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
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

        public bool Equals(LeaderInfo other)
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

        public bool Equals(ManagerInfo other)
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

        public bool Equals(CreatorInfo other)
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

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
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
            base.Header = new TextBlock() { Text = MessageConverter.ToSectionString(this.Value.Section) };
            base.IsExpanded = this.Value.IsExpanded;

            List<SearchTreeViewItem> list = new List<SearchTreeViewItem>();

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
                var vx = x.Value;
                var vy = y.Value;

                int c = vx.SearchItem.Name.CompareTo(vy.SearchItem.Name);
                if (c != 0) return c;
                c = vx.SearchTreeItems.Count.CompareTo(vy.SearchTreeItems.Count);
                if (c != 0) return c;
                c = vx.ChannelTreeItems.Count.CompareTo(vy.ChannelTreeItems.Count);
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

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
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
            base.Header = new TextBlock() { Text = this.Value.SearchItem.Name };
            base.IsExpanded = this.Value.IsExpanded;

            List<dynamic> list = new List<dynamic>();

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

            list.Sort(delegate(object x, object y)
            {
                if (x is SearchTreeViewItem)
                {
                    if (y is SearchTreeViewItem)
                    {
                        var vx = ((SearchTreeViewItem)x).Value;
                        var vy = ((SearchTreeViewItem)y).Value;

                        int c = vx.SearchItem.Name.CompareTo(vy.SearchItem.Name);
                        if (c != 0) return c;
                        c = vx.SearchTreeItems.Count.CompareTo(vy.SearchTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.ChannelTreeItems.Count.CompareTo(vy.ChannelTreeItems.Count);
                        if (c != 0) return c;
                    }
                    else if (y is ChannelTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is ChannelTreeViewItem)
                {
                    if (y is ChannelTreeViewItem)
                    {
                        var cx = ((ChannelTreeViewItem)x).Value.Channel;
                        var cy = ((ChannelTreeViewItem)y).Value.Channel;

                        int c = cx.Name.CompareTo(cy.Name);
                        if (c != 0) return c;
                        c = Collection.Compare(cx.Id, cy.Id);
                        if (c != 0) return c;
                    }
                    else if (y is SearchTreeViewItem)
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
        private string _creatorSignature;

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
            base.Header = new TextBlock() { Text = string.Format("{0} ({1}) {2}", _value.Channel.Name, _value.Messages.Count, _creatorSignature) };
        }

        public string CreatorSignature
        {
            get
            {
                return _creatorSignature;
            }
            set
            {
                _creatorSignature = value;

                this.Update();
            }
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
        private string _leaderSignature;

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

        [DataMember(Name = "LeaderSignature")]
        public string LeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _leaderSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _leaderSignature = value;
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

        #region IDeepClone<SectionTreeItem>

        public SectionTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionTreeItem)ds.ReadObject(textDictionaryReader);
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
        private bool _isFilterEnabled = true;
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

        [DataMember(Name = "IsFilterEnabled")]
        public bool IsFilterEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isFilterEnabled;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isFilterEnabled = value;
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
        private LockedList<SearchContains<SearchRegex>> _searchSignatureCollection;
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
        public LockedList<SearchContains<SearchRegex>> SearchSignatureCollection
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_searchSignatureCollection == null)
                        _searchSignatureCollection = new LockedList<SearchContains<SearchRegex>>();

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
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>
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
                return _contains;
            }
            set
            {
                _contains = value;
            }
        }

        [DataMember(Name = "Value")]
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
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
            return string.Format("{0} {1}", this.Contains, this.Value);
        }

        #region IDeepClone<SearchContains<T>>

        public SearchContains<T> DeepClone()
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

        #endregion
    }

    [DataContract(Name = "SearchRegex", Namespace = "http://Lair/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>
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
                return _value;
            }
            set
            {
                _value = value;

                this.RegexUpdate();
            }
        }

        [DataMember(Name = "IsIgnoreCase")]
        public bool IsIgnoreCase
        {
            get
            {
                return _isIgnoreCase;
            }
            set
            {
                _isIgnoreCase = value;

                this.RegexUpdate();
            }
        }

        private void RegexUpdate()
        {
            var o = RegexOptions.Compiled | RegexOptions.Singleline;
            if (_isIgnoreCase) o |= RegexOptions.IgnoreCase;

            if (_value != null) _regex = new Regex(_value, o);
            else _regex = null;
        }

        public bool IsMatch(string value)
        {
            if (_regex == null) return false;

            return _regex.IsMatch(value);
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
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
            return string.Format("{0} {1}", this.IsIgnoreCase, this.Value);
        }

        #region IDeepClone<SearchRegex>

        public SearchRegex DeepClone()
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

        #endregion
    }

    [DataContract(Name = "SearchRange", Namespace = "http://Lair/Windows")]
    class SearchRange<T> : IEquatable<SearchRange<T>>, IDeepCloneable<SearchRange<T>>
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
                return _max;
            }
            set
            {
                _max = value;
                _min = (_min.CompareTo(_max) > 0) ? _max : _min;
            }
        }

        [DataMember(Name = "Min")]
        public T Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                _max = (_max.CompareTo(_min) < 0) ? _min : _max;
            }
        }

        public bool Verify(T value)
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

        public override int GetHashCode()
        {
            return this.Min.GetHashCode();
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
            return string.Format("Max = {0}, Min = {1}", this.Max, this.Min);
        }

        #region IDeepClone<SearchRange<T>>

        public SearchRange<T> DeepClone()
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

        #endregion
    }
}
