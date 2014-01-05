using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Lair.Properties;
using Library;
using Library.Collections;
using Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// ConnectionControl.xaml の相互作用ロジック
    /// </summary>
    partial class ConnectionControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private LairManager _amoebaManager;

        private ObservableCollectionEx<LairInfomationListViewItem> _infomationListViewItemCollection = new ObservableCollectionEx<LairInfomationListViewItem>();
        private ObservableCollectionEx<ConnectionListViewItem> _listViewItemCollection = new ObservableCollectionEx<ConnectionListViewItem>();

        private Thread _showLairInfomationThread;
        private Thread _showConnectionInfomationwThread;

        public ConnectionControl(LairManager amoebaManager)
        {
            _amoebaManager = amoebaManager;

            InitializeComponent();

            _listView.ItemsSource = _listViewItemCollection;

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_SentByteCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_ReceivedByteCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_CreateConnectionCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_AcceptConnectionCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_SurroundingNodeCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_RelayBlockCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_LockSpace" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_FreeSpace" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_UsingSpace" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_NodeCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_HeaderCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_BlockCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushNodeCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushBlockLinkCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushBlockRequestCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushBlockCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushHeaderRequestCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PushHeaderCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem());

            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullNodeCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullBlockLinkCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullBlockRequestCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullBlockCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullHeaderRequestCount" });
            _infomationListViewItemCollection.Add(new LairInfomationListViewItem() { Id = "ConnectionControl_PullHeaderCount" });

            _infomationListView.ItemsSource = _infomationListViewItemCollection;

            _showLairInfomationThread = new Thread(this.ShowLairInfomation);
            _showLairInfomationThread.Priority = ThreadPriority.Highest;
            _showLairInfomationThread.IsBackground = true;
            _showLairInfomationThread.Name = "ConnectionControl_ShowLairInfomationThread";
            _showLairInfomationThread.Start();

            _showConnectionInfomationwThread = new Thread(this.ShowConnectionInfomation);
            _showConnectionInfomationwThread.Priority = ThreadPriority.Highest;
            _showConnectionInfomationwThread.IsBackground = true;
            _showConnectionInfomationwThread.Name = "ConnectionControl_ShowConnectionInfomationThread";
            _showConnectionInfomationwThread.Start();
        }

        private void ShowLairInfomation()
        {
            try
            {
                for (; ; )
                {
                    var information = _amoebaManager.Information;
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    dic["ConnectionControl_SentByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.SentByteCount);
                    dic["ConnectionControl_ReceivedByteCount"] = NetworkConverter.ToSizeString(_amoebaManager.ReceivedByteCount);

                    dic["ConnectionControl_CreateConnectionCount"] = ((int)information["CreateConnectionCount"]).ToString();
                    dic["ConnectionControl_AcceptConnectionCount"] = ((int)information["AcceptConnectionCount"]).ToString();

                    dic["ConnectionControl_SurroundingNodeCount"] = ((int)information["SurroundingNodeCount"]).ToString();
                    dic["ConnectionControl_RelayBlockCount"] = ((int)information["RelayBlockCount"]).ToString();

                    dic["ConnectionControl_LockSpace"] = NetworkConverter.ToSizeString(((long)information["LockSpace"])).ToString();
                    dic["ConnectionControl_FreeSpace"] = NetworkConverter.ToSizeString(((long)information["FreeSpace"])).ToString();
                    dic["ConnectionControl_UsingSpace"] = NetworkConverter.ToSizeString(((long)information["UsingSpace"])).ToString();

                    dic["ConnectionControl_NodeCount"] = ((int)information["OtherNodeCount"]).ToString();
                    dic["ConnectionControl_HeaderCount"] = ((int)information["HeaderCount"]).ToString();
                    dic["ConnectionControl_BlockCount"] = ((int)information["BlockCount"]).ToString();

                    dic["ConnectionControl_PushNodeCount"] = ((int)information["PushNodeCount"]).ToString();
                    dic["ConnectionControl_PushBlockLinkCount"] = ((int)information["PushBlockLinkCount"]).ToString();
                    dic["ConnectionControl_PushBlockRequestCount"] = ((int)information["PushBlockRequestCount"]).ToString();
                    dic["ConnectionControl_PushBlockCount"] = ((int)information["PushBlockCount"]).ToString();
                    dic["ConnectionControl_PushHeaderRequestCount"] = ((int)information["PushHeaderRequestCount"]).ToString();
                    dic["ConnectionControl_PushHeaderCount"] = ((int)information["PushHeaderCount"]).ToString();

                    dic["ConnectionControl_PullNodeCount"] = ((int)information["PullNodeCount"]).ToString();
                    dic["ConnectionControl_PullBlockLinkCount"] = ((int)information["PullBlockLinkCount"]).ToString();
                    dic["ConnectionControl_PullBlockRequestCount"] = ((int)information["PullBlockRequestCount"]).ToString();
                    dic["ConnectionControl_PullBlockCount"] = ((int)information["PullBlockCount"]).ToString();
                    dic["ConnectionControl_PullHeaderRequestCount"] = ((int)information["PullHeaderRequestCount"]).ToString();
                    dic["ConnectionControl_PullHeaderCount"] = ((int)information["PullHeaderCount"]).ToString();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in dic)
                        {
                            _infomationListViewItemCollection.First(n => n.Id == item.Key).Value = item.Value;
                        }
                    }));

                    Thread.Sleep(1000 * 10);
                }
            }
            catch (Exception)
            {

            }
        }

        private void ShowConnectionInfomation()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (_mainWindow.SelectedTab != MainWindowTabType.Connection) continue;

                    var connectionInformation = _amoebaManager.ConnectionInformation.ToArray();
                    Dictionary<int, Information> dic = new Dictionary<int, Information>();

                    foreach (var item in connectionInformation.ToArray())
                    {
                        dic[(int)item["Id"]] = item;
                    }

                    Dictionary<int, ConnectionListViewItem> dic2 = new Dictionary<int, ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            dic2[item.Id] = item;
                        }
                    }));

                    List<ConnectionListViewItem> removeList = new List<ConnectionListViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        foreach (var item in _listViewItemCollection.ToArray())
                        {
                            if (!dic.ContainsKey(item.Id))
                            {
                                removeList.Add(item);
                            }
                        }
                    }));

                    List<ConnectionListViewItem> newList = new List<ConnectionListViewItem>();
                    Dictionary<ConnectionListViewItem, Information> updateDic = new Dictionary<ConnectionListViewItem, Information>();
                    bool clearFlag = false;
                    var selectItems = new List<ConnectionListViewItem>();

                    if (removeList.Count > 100)
                    {
                        clearFlag = true;
                        removeList.Clear();
                        updateDic.Clear();

                        foreach (var information in connectionInformation)
                        {
                            newList.Add(new ConnectionListViewItem(information));
                        }

                        HashSet<int> hid = new HashSet<int>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            hid.UnionWith(_listView.SelectedItems.OfType<ConnectionListViewItem>().Select(n => n.Id));
                        }));

                        foreach (var item in newList)
                        {
                            if (hid.Contains(item.Id))
                            {
                                selectItems.Add(item);
                            }
                        }
                    }
                    else
                    {
                        foreach (var information in connectionInformation)
                        {
                            ConnectionListViewItem item = null;

                            if (dic2.ContainsKey((int)information["Id"]))
                                item = dic2[(int)information["Id"]];

                            if (item != null)
                            {
                                if (!Collection.Equals(item.Information, information))
                                {
                                    updateDic[item] = information;
                                }
                            }
                            else
                            {
                                newList.Add(new ConnectionListViewItem(information));
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        bool sortFlag = false;

                        if (newList.Count != 0) sortFlag = true;
                        if (removeList.Count != 0) sortFlag = true;
                        if (updateDic.Count != 0) sortFlag = true;

                        if (clearFlag) _listViewItemCollection.Clear();

                        foreach (var item in newList)
                        {
                            _listViewItemCollection.Add(item);
                        }

                        foreach (var item in removeList)
                        {
                            _listViewItemCollection.Remove(item);
                        }

                        foreach (var item in updateDic)
                        {
                            item.Key.Information = item.Value;
                        }

                        if (clearFlag)
                        {
                            _listView.SelectedItems.Clear();
                            _listView.SetSelectedItems(selectItems);
                        }

                        if (sortFlag && _listViewItemCollection.Count < 3000) this.Sort();
                    }));

                    Thread.Sleep(1000 * 3);
                }
            }
            catch (Exception)
            {

            }
        }

        private void _listView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _listView.SelectedItems;

            _listViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _listViewPasteMenuItem.IsEnabled = Clipboard.ContainsNodes();
        }

        private void _listViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectItems = _listView.SelectedItems;
            if (selectItems == null) return;

            var nodes = new List<Node>();

            foreach (var information in selectItems.OfType<ConnectionListViewItem>().Select(n => n.Information))
            {
                if (information.Contains("Node"))
                {
                    nodes.Add((Node)information["Node"]);
                }
            }

            Clipboard.SetNodes(nodes);
        }

        private void _listViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _amoebaManager.SetOtherNodes(Clipboard.GetNodes());
        }

        #region Sort

        private void Sort()
        {
            this.GridViewColumnHeaderClickedHandler(null, null);
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                var item = e.OriginalSource as GridViewColumnHeader;
                if (item == null || item.Role == GridViewColumnHeaderRole.Padding) return;

                string headerClicked = item.Column.Header as string;
                if (headerClicked == null) return;

                ListSortDirection direction;

                if (headerClicked != Settings.Instance.ConnectionControl_LastHeaderClicked)
                {
                    direction = ListSortDirection.Ascending;
                }
                else
                {
                    if (Settings.Instance.ConnectionControl_ListSortDirection == ListSortDirection.Ascending)
                    {
                        direction = ListSortDirection.Descending;
                    }
                    else
                    {
                        direction = ListSortDirection.Ascending;
                    }
                }

                Sort(headerClicked, direction);

                Settings.Instance.ConnectionControl_LastHeaderClicked = headerClicked;
                Settings.Instance.ConnectionControl_ListSortDirection = direction;
            }
            else
            {
                _listView.Items.SortDescriptions.Clear();

                if (Settings.Instance.ConnectionControl_LastHeaderClicked != null)
                {
                    var list = Sort(_listViewItemCollection, Settings.Instance.ConnectionControl_LastHeaderClicked, Settings.Instance.ConnectionControl_ListSortDirection).ToList();

                    for (int i = 0; i < list.Count; i++)
                    {
                        var o = _listViewItemCollection.IndexOf(list[i]);

                        if (i != o) _listViewItemCollection.Move(o, i);
                    }
                }
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            _listView.Items.SortDescriptions.Clear();

            if (sortBy == LanguagesManager.Instance.ConnectionControl_Uri)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Uri", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_Priority)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("Priority", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_ReceivedByteCount)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("ReceivedByteCount", direction));
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_SentByteCount)
            {
                _listView.Items.SortDescriptions.Add(new SortDescription("SentByteCount", direction));
            }
        }

        private IEnumerable<ConnectionListViewItem> Sort(IEnumerable<ConnectionListViewItem> collection, string sortBy, ListSortDirection direction)
        {
            List<ConnectionListViewItem> list = new List<ConnectionListViewItem>(collection);

            if (sortBy == LanguagesManager.Instance.ConnectionControl_Uri)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Uri.CompareTo(y.Uri);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_Priority)
            {
                list.Sort((x, y) =>
                {
                    int c = x.Priority.CompareTo(y.Priority);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_ReceivedByteCount)
            {
                list.Sort((x, y) =>
                {
                    int c = x.ReceivedByteCount.CompareTo(y.ReceivedByteCount);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }
            else if (sortBy == LanguagesManager.Instance.ConnectionControl_SentByteCount)
            {
                list.Sort((x, y) =>
                {
                    int c = x.SentByteCount.CompareTo(y.SentByteCount);
                    if (c != 0) return c;
                    c = x.Id.CompareTo(y.Id);
                    if (c != 0) return c;

                    return 0;
                });
            }

            if (direction == ListSortDirection.Descending)
            {
                list.Reverse();
            }

            return list;
        }

        #endregion

        private class LairInfomationListViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            public LairInfomationListViewItem()
            {
                LanguagesManager.UsingLanguageChangedEvent += this.LanguagesManager_UsingLanguageChangedEvent;
            }

            void LanguagesManager_UsingLanguageChangedEvent(object sender)
            {
                this.NotifyPropertyChanged("Name");
            }

            private string _id;
            private string _value;

            public string Id
            {
                get
                {
                    return _id;
                }
                set
                {
                    if (value != _id)
                    {
                        _id = value;

                        this.NotifyPropertyChanged("Id");
                        this.NotifyPropertyChanged("Name");
                    }
                }
            }

            public string Name
            {
                get
                {
                    if (_id != null)
                        return LanguagesManager.Instance.Translate(_id);

                    return null;
                }
            }

            public string Value
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

        private class ConnectionListViewItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private void NotifyPropertyChanged(string info)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
            }

            private int _id;
            private Information _information;
            private string _uri;
            private int _priority;
            private long _receivedByteCount;
            private long _sentByteCount;

            public ConnectionListViewItem(Information information)
            {
                this.Information = information;

                _id = (int)this.Information["Id"];
            }

            public int Id
            {
                get
                {
                    return _id;
                }
            }

            public Information Information
            {
                get
                {
                    return _information;
                }
                set
                {
                    _information = value;

                    if (_information.Contains("Uri")) this.Uri = (string)_information["Uri"];
                    else this.Uri = null;

                    if (_information.Contains("Priority")) this.Priority = (int)_information["Priority"];
                    else this.Priority = 0;

                    if (_information.Contains("ReceivedByteCount")) this.ReceivedByteCount = (long)_information["ReceivedByteCount"];
                    else this.ReceivedByteCount = 0;

                    if (_information.Contains("SentByteCount")) this.SentByteCount = (long)_information["SentByteCount"];
                    else this.SentByteCount = 0;
                }
            }

            public string Uri
            {
                get
                {
                    return _uri;
                }
                set
                {
                    if (value != _uri)
                    {
                        _uri = value;

                        this.NotifyPropertyChanged("Uri");
                    }
                }
            }

            public int Priority
            {
                get
                {
                    return _priority;
                }
                set
                {
                    if (value != _priority)
                    {
                        _priority = value;

                        this.NotifyPropertyChanged("Priority");
                    }
                }
            }

            public long ReceivedByteCount
            {
                get
                {
                    return _receivedByteCount;
                }
                set
                {
                    if (value != _receivedByteCount)
                    {
                        _receivedByteCount = value;

                        this.NotifyPropertyChanged("ReceivedByteCount");
                    }
                }
            }

            public long SentByteCount
            {
                get
                {
                    return _sentByteCount;
                }
                set
                {
                    if (value != _sentByteCount)
                    {
                        _sentByteCount = value;

                        this.NotifyPropertyChanged("SentByteCount");
                    }
                }
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewCopyMenuItem_Click(null, null);
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            _listViewPasteMenuItem_Click(null, null);
        }
    }
}
