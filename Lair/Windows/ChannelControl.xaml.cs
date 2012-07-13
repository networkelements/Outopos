using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
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
using System.Xml;
using Lair;
using Lair.Properties;
using Library;
using Library.Net.Lair;
using Library.Security;
using System.Windows.Threading;

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
        private volatile bool _refresh = false;

        private ObservableCollection<Message> _listBoxItemCollection = new ObservableCollection<Message>();

        public ChannelControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager)
        {
            _mainWindow = mainWindow;
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            InitializeComponent();

            _treeViewItem.Value = Settings.Instance.ChannelControl_Category;
            _listBox.ItemsSource = _listBoxItemCollection;

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

                if (App.SelectTab == "Search" && !_refresh)
                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectTreeViewItem.Value.Channel.Name);
            };

            _searchThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    for (; ; )
                    {
                        Thread.Sleep(100);
                        if (!_refresh) continue;

                        BoardTreeViewItem selectTreeViewItem = null;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                        }), null);

                        if (selectTreeViewItem == null) continue;

                        HashSet<Message> newList = new HashSet<Message>();
                        HashSet<Message> oldList = new HashSet<Message>();

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            oldList.UnionWith(_listBoxItemCollection.ToArray());
                        }), null);

                        IList<Message> messages;
                        IList<Filter> filters;

                        _lairManager.GetChannelInfomation(selectTreeViewItem.Value.Channel, out messages, out filters);

                        Filter filter = filters.FirstOrDefault(n => selectTreeViewItem.Value.Signature == MessageConverter.ToSignatureString(n.Certificate));

                        foreach (var message in messages)
                        {
                            if (filter.Keys.Any(n => message.VerifyKey(n)))
                            {
                                newList.Add(message);
                            }
                        }

                        var removeList = new List<Message>();
                        var addList = new List<Message>();

                        foreach (var item in oldList)
                        {
                            if (!newList.Contains(item)) removeList.Add(item);
                        }

                        foreach (var item in newList)
                        {
                            if (!oldList.Contains(item)) addList.Add(item);
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action<object>(delegate(object state2)
                        {
                            if (selectTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            _listBox.SelectedItems.Clear();

                            bool sortFlag = false;

                            if (removeList.Count > 100)
                            {
                                sortFlag = true;

                                _listBoxItemCollection.Clear();

                                foreach (var item in newList)
                                {
                                    _listBoxItemCollection.Add(item);
                                }
                            }
                            else
                            {
                                if (addList.Count != 0) sortFlag = true;
                                if (removeList.Count != 0) sortFlag = true;

                                foreach (var item in addList)
                                {
                                    _listBoxItemCollection.Add(item);
                                }

                                foreach (var item in removeList)
                                {
                                    _listBoxItemCollection.Remove(item);
                                }
                            }

                            if (sortFlag) this.Sort();

                            if (App.SelectTab == "Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectTreeViewItem.Value.Channel.Name);
                        }), null);
                    }
                }
                catch (Exception)
                {

                }
            }));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SearchThread";
            _searchThread.Start();

            _listBoxItemCollection.Add(new Message() { Content = "123132123\r\nhttp://www.google.co.jp/ 1111111\r\n" });
            _listBoxItemCollection.Add(new Message()
            {
                Content = "Seed@AAAAABQAQW1vZWJhIDAuMS4xMiDOsS56aXAAAAAIAQAAAAAALUGJAAAAFAIyMDEyLTA3LTEyVDA3OjI4OjQ0WgAAAAADAAAABAQAAAACAAAARQUAAABAAA8F_KZ4Hp3rQSIYbmpE1CXyRP6b1hOH53JtrRiiaagsbwyH7_l5zwlu_YXN25v9b_-em0foUmUOQY4t6WwRwbMAAAAHBkFyY2hpdmUAAAAEB0x6bWEAAAALCFJpam5kYWVsMjU2AAAAQAkytF4JKYcjaG71AXqAJrdVnsV2DR60bf5corKKaKmoUQ1nR9KNavEfZzjVrXNrRown29BILUokHorDL3Osevy4Wn1uwA=="
                    + "\r\n" + "Seed@AAAAABQAQW1vZWJhIDAuMS4xMiDOsS56aXAAAAAIAQAAAAAALUGJAAAAFAIyMDEyLTA3LTEyVDA3OjI4OjQ0WgAAAAADAAAABAQAAAACAAAARQUAAABAAA8F_KZ4Hp3rQSIYbmpE1CXyRP6b1hOH53JtrRiiaagsbwyH7_l5zwlu_YXN25v9b_-em0foUmUOQY4t6WwRwbMAAAAHBkFyY2hpdmUAAAAEB0x6bWEAAAALCFJpam5kYWVsMjU2AAAAQAkytF4JKYcjaG71AXqAJrdVnsV2DR60bf5corKKaKmoUQ1nR9KNavEfZzjVrXNrRown29BILUokHorDL3Osevy4Wn1uwA=="
            });
        }

        private void Update()
        {
            Settings.Instance.ChannelControl_Category = _treeViewItem.Value;

            _treeView_SelectedItemChanged(this, null);
            _treeViewItem.Sort();
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
                if (t == null || s == t
                    || t.Value.Categories.Any(n => object.ReferenceEquals(n, s.Value))
                    || t.Value.Boards.Any(n => object.ReferenceEquals(n, s.Value))) return;

                if (_treeViewItem.GetLineage(t).Any(n => object.ReferenceEquals(n, s))) return;

                t.IsSelected = true;

                var list = _treeViewItem.GetLineage((TreeViewItem)s).OfType<CategoryTreeViewItem>().ToList();
                if (s.Value is Category) list[list.Count - 2].Value.Categories.Remove(s.Value);
                else if (s.Value is Board) list[list.Count - 2].Value.Boards.Remove(s.Value);
                list[list.Count - 2].Update();

                if (s.Value is Category) t.Value.Categories.Add(s.Value);
                else if (s.Value is Board) t.Value.Boards.Add(s.Value);
                t.Update();

                this.Update();
            }
        }

        private void _treeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = _treeView.GetCurrentItem(e.GetPosition) as TreeViewItem;
            if (item == null) return;

            item.IsSelected = true;
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

            _treeView_SelectedItemChanged(null, null);
        }

        private void _treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectSearchTreeViewItem = _treeView.SelectedItem as TreeViewItem;
            if (selectSearchTreeViewItem == null) return;

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
        }

        private void _treeView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _treeViewAddCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _treeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        #endregion

        #region _listBox

        private void _listBox_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _listBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _listBox.Items.Refresh();
        }

        #endregion

        private void _newMessageButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
            if (selectTreeViewItem == null) return;

            Message message = new Message();
            message.Channel = selectTreeViewItem.Value.Channel;

            MessageEditWindow window = new MessageEditWindow(ref message, _lairManager);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                _lairManager.Upload(message);
            }
        }

        #region Sort

        private void Sort()
        {
            List<Message> list = new List<Message>(_listBoxItemCollection);

            list.Sort(delegate(Message x, Message y)
            {
                int c = x.CreationTime.CompareTo(y.CreationTime);
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
    }

    class CategoryTreeViewItem : TreeViewItem
    {
        private Category _value;
        private int _hit;
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();

        public CategoryTreeViewItem()
            : base()
        {
            this.Value = new Category() { Name = "" };

            base.IsExpanded = true;
            base.ItemsSource = _listViewItemCollection;
        }
    
        public CategoryTreeViewItem(Category category)
            : base()
        {
            this.Value = category;

            base.IsExpanded = true;
            base.ItemsSource = _listViewItemCollection;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.Name, _hit);

            List<dynamic> list = new List<dynamic>();

            foreach (var item in this.Value.Boards)
            {
                list.Add(new BoardTreeViewItem(item));
            }

            foreach (var item in this.Value.Categories)
            {
                list.Add(new CategoryTreeViewItem(item));
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

                        int c = bx.Channel.Name.CompareTo(by.Channel.Name);
                        if (c != 0) return c;
                        c = bx.Signature.CompareTo(by.Signature);
                        if (c != 0) return c;

                        return bx.GetHashCode().CompareTo(by.GetHashCode());
                    }
                    else if (y is Category)
                    {
                        return -1;
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
                    else if (y is Board)
                    {
                        return 1;
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

    class BoardTreeViewItem : TreeViewItem
    {
        private Board _value;
        private int _hit;

        public BoardTreeViewItem(Board board)
            : base()
        {
            _value = board;
        }

        public void Update()
        {
            base.Header = string.Format("{0} ({1})", _value.Channel.Name, _hit);
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

    [DataContract(Name = "Category", Namespace = "http://Lair/Windows")]
    class Category : IDeepCloneable<Category>
    {
        private string _name;
        private List<Board> _boards;
        private List<Category> _categories;

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        [DataMember(Name = "Boards")]
        public List<Board> Boards
        {
            get
            {
                if (_boards == null)
                    _boards = new List<Board>();

                return _boards;
            }
        }

        [DataMember(Name = "Categories")]
        public List<Category> Categories
        {
            get
            {
                if (_categories == null)
                    _categories = new List<Category>();

                return _categories;
            }
        }

        #region IDeepClone<Category>

        public Category DeepClone()
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

        #endregion
    }

    [DataContract(Name = "Board", Namespace = "http://Lair/Windows")]
    class Board : IDeepCloneable<Board>
    {
        private Channel _channel;
        private string _signature;

        [DataMember(Name = "Channel")]
        public Channel Channel
        {
            get
            {
                return _channel;
            }
            set
            {
                _channel = value;
            }
        }

        [DataMember(Name = "Signature")]
        public string Signature
        {
            get
            {
                return _signature;
            }
            set
            {
                _signature = value;
            }
        }

        #region IDeepClone<Thread>

        public Board DeepClone()
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

        #endregion
    }
}
