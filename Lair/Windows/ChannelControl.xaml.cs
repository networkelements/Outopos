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
using System.Xml;
using Lair;
using Lair.Properties;
using Library;
using Library.Net.Lair;
using Library.Security;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

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
                            if (_treeView.SelectedItem is CategoryTreeViewItem)
                            {
                                var item  = (CategoryTreeViewItem)_treeView.SelectedItem;
                                
                                _listBoxItemCollection.Clear();

                                if (App.SelectTab == "Channel")
                                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, item.Value.Name);
                            }
                            else if(_treeView.SelectedItem is BoardTreeViewItem)
                            {
                                selectTreeViewItem = (BoardTreeViewItem)_treeView.SelectedItem;
                            }
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

                        if (filter != null)
                        {
                            foreach (var message in messages)
                            {
                                if (filter.Keys.Any(n => message.VerifyKey(n)))
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

                            _listBox.Items.Refresh();

                            if (App.SelectTab == "Channel")
                                _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, MessageConverter.ToChannelString(selectTreeViewItem.Value.Channel));
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
        }

        private static void Filter(ref HashSet<Message> messages, Category category)
        {
            DateTime now = DateTime.UtcNow;

            messages.IntersectWith(messages.ToArray().Where(n =>
            {
                if (string.IsNullOrWhiteSpace(n.Content)) return false;
                if ((n.CreationTime - now) > new TimeSpan(0, 30, 0)) return false;

                return true;
            }));

            messages.IntersectWith(messages.ToArray().Where(searchItem =>
            {
                bool flag;

                if (category.SearchWordCollection.Any(n => n.Contains == true))
                {
                    flag = category.SearchWordCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchItem.Content.Contains(searchContains.Value);

                        return false;
                    });
                    if (!flag) return false;
                }

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

                if (category.SearchRegexCollection.Any(n => n.Contains == true))
                {
                    flag = category.SearchRegexCollection.Any(searchContains =>
                    {
                        if (searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Content);

                        return false;
                    });
                    if (!flag) return false;
                }

                return true;
            }));

            messages.ExceptWith(messages.ToArray().Where(searchItem =>
            {
                bool flag;

                if (category.SearchWordCollection.Any(n => n.Contains == false))
                {
                    flag = category.SearchWordCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchItem.Content.Contains(searchContains.Value);

                        return false;
                    });
                    if (flag) return true;
                }

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

                if (category.SearchRegexCollection.Any(n => n.Contains == false))
                {
                    flag = category.SearchRegexCollection.Any(searchContains =>
                    {
                        if (!searchContains.Contains) return searchContains.Value.IsMatch(searchItem.Content);

                        return false;
                    });
                    if (flag) return true;
                }

                return false;
            }));
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
                else if (s.Value is Board) t.Value.Boards.Add(s.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();
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
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                _treeViewNewChannelMenuItem.IsEnabled = true;
                _treeViewAddCategoryMenuItem.IsEnabled = true;
                _treeViewEditMenuItem.IsEnabled = true;
                _treeViewDeleteMenuItem.IsEnabled = true;
                _treeViewCutContextMenuItem.IsEnabled = true;
                _treeViewCopyContextMenuItem.IsEnabled = true;

                {
                    var categories = Clipboard.GetCategories();
                    var channels = Clipboard.GetChannels();

                    _treeViewPasteContextMenuItem.IsEnabled = (categories.Count() + channels.Count()) > 0 ? true : false;
                }
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                _treeViewNewChannelMenuItem.IsEnabled = false;
                _treeViewAddCategoryMenuItem.IsEnabled = false;
                _treeViewEditMenuItem.IsEnabled = false;
                _treeViewDeleteMenuItem.IsEnabled = true;
                _treeViewCutContextMenuItem.IsEnabled = true;
                _treeViewCopyContextMenuItem.IsEnabled = true;
                _treeViewPasteContextMenuItem.IsEnabled = false;
            }
        }

        private void _treeViewNewChannelMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            Channel channel = new Channel();

            NewChannelWindow window = new NewChannelWindow(out channel);
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Boards.Add(new Board() { Channel = channel });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _treeViewAddCategoryMenuItem_Click(object sender, RoutedEventArgs e)
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
                if (selectTreeViewItem == null) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Categories.Remove(selectTreeViewItem.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Remove(selectTreeViewItem.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
        }

        private void _treeViewCutContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                var list = _treeViewItem.GetLineage(selectTreeViewItem).OfType<TreeViewItem>().ToList();

                Clipboard.SetCategories(new Category[] { selectTreeViewItem.Value });
                Clipboard.SetChannels(new Channel[0]);

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
                Clipboard.SetCategories(new Category[0]);

                ((CategoryTreeViewItem)list[list.Count - 2]).Value.Boards.Remove(selectTreeViewItem.Value);

                ((CategoryTreeViewItem)list[list.Count - 2]).Update();

                this.Update();
            }
        }

        private void _treeViewCopyContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (_treeView.SelectedItem is CategoryTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
                if (selectTreeViewItem == null) return;

                Clipboard.SetCategories(new Category[] { selectTreeViewItem.Value });
                Clipboard.SetChannels(new Channel[0]);
            }
            else if (_treeView.SelectedItem is BoardTreeViewItem)
            {
                var selectTreeViewItem = _treeView.SelectedItem as BoardTreeViewItem;
                if (selectTreeViewItem == null) return;

                Clipboard.SetChannels(new Channel[] { selectTreeViewItem.Value.Channel });
                Clipboard.SetCategories(new Category[0]);
            }
        }

        private void _treeViewPasteContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as CategoryTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.Categories.AddRange(Clipboard.GetCategories());
            Clipboard.SetCategories(new Category[0]);

            foreach (var channel in Clipboard.GetChannels())
            {
                selectTreeViewItem.Value.Boards.Add(new Board() { Channel = channel });
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        #endregion

        #region _listBox

        private void _listBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;

            var peer = ItemsControlAutomationPeer.CreatePeerForElement(_listBox);
            var scrollProvider = peer.GetPattern(PatternInterface.Scroll) as IScrollProvider;

            try
            {
                if (e.Delta > 0) scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallDecrement);
                if (e.Delta < 0) scrollProvider.Scroll(System.Windows.Automation.ScrollAmount.NoAmount, System.Windows.Automation.ScrollAmount.SmallIncrement);
            }
            catch (Exception)
            {

            }
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
        private static BitmapImage _image;
        private Category _value;
        private int _hit;
        private ObservableCollection<object> _listViewItemCollection = new ObservableCollection<object>();

        static CategoryTreeViewItem()
        {
            try
            {
                _image = CategoryTreeViewItem.GetImage(Path.Combine(App.DirectoryPaths["Icons"], "Category.png"));
            }
            catch (Exception)
            {

            }
        }

        private static BitmapImage GetImage(string path)
        {
            var icon = new BitmapImage();
            icon.BeginInit();
            icon.StreamSource = new FileStream(path, FileMode.Open);
            icon.EndInit();
            return icon;
        }

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

        public void Update()
        {
            Grid grid = new Grid();
            grid.Children.Add(new Image() { Source = _image, Height = 16, Width = 16, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });
            grid.Children.Add(new TextBlock() { Margin = new Thickness(22, 0, 0, 0), Text = string.Format("{0} ({1})", _value.Name, _hit) });

            this.Header = grid;

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
                        c = xsignature.CompareTo(ysignature);
                        if (c != 0) return c;

                        return bx.GetHashCode().CompareTo(by.GetHashCode());
                    }
                    else if (y is CategoryTreeViewItem)
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
                    else if (y is BoardTreeViewItem)
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
        private static BitmapImage _image;
        private Board _value;
        private int _hit;

        static BoardTreeViewItem()
        {
            try
            {
                _image = BoardTreeViewItem.GetImage(Path.Combine(App.DirectoryPaths["Icons"], "Board.png"));
            }
            catch (Exception)
            {

            }
        }

        private static BitmapImage GetImage(string path)
        {
            var icon = new BitmapImage();
            icon.BeginInit();
            icon.StreamSource = new FileStream(path, FileMode.Open);
            icon.EndInit();
            return icon;
        }

        public BoardTreeViewItem(Board board)
            : base()
        {
            this.Value = board;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        public void Update()
        {
            Grid grid = new Grid();
            grid.Children.Add(new Image() { Source = _image, Height = 16, Width = 16, HorizontalAlignment = System.Windows.HorizontalAlignment.Left });
            grid.Children.Add(new TextBlock() { Margin = new Thickness(22, 0, 0, 0), Text = string.Format("{0} ({1})", _value.Channel.Name, _hit) });

            this.Header = grid;
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
        private List<SearchContains<string>> _searchWordCollection;
        private List<SearchContains<SearchRegex>> _searchRegexCollection;
        private List<SearchContains<string>> _searchSignatureCollection;

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

        [DataMember(Name = "SearchWordCollection")]
        public List<SearchContains<string>> SearchWordCollection
        {
            get
            {
                if (_searchWordCollection == null)
                    _searchWordCollection = new List<SearchContains<string>>();

                return _searchWordCollection;
            }
        }

        [DataMember(Name = "SearchNameRegexCollection")]
        public List<SearchContains<SearchRegex>> SearchRegexCollection
        {
            get
            {
                if (_searchRegexCollection == null)
                    _searchRegexCollection = new List<SearchContains<SearchRegex>>();

                return _searchRegexCollection;
            }
        }

        [DataMember(Name = "SearchSignatureCollection")]
        public List<SearchContains<string>> SearchSignatureCollection
        {
            get
            {
                if (_searchSignatureCollection == null)
                    _searchSignatureCollection = new List<SearchContains<string>>();

                return _searchSignatureCollection;
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

    [DataContract(Name = "SearchContains", Namespace = "http://Amoeba/Windows")]
    class SearchContains<T> : IEquatable<SearchContains<T>>, IDeepCloneable<SearchContains<T>>
    {
        [DataMember(Name = "Contains")]
        public bool Contains { get; set; }

        [DataMember(Name = "Value")]
        public T Value { get; set; }

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

    [DataContract(Name = "SearchRegex", Namespace = "http://Amoeba/Windows")]
    class SearchRegex : IEquatable<SearchRegex>, IDeepCloneable<SearchRegex>
    {
        private string _value;
        private bool _isIgnoreCase;

        private Regex _regex;

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

            try
            {
                if (_value != null) _regex = new Regex(_value, o);
                else _regex = null;
            }
            catch (Exception)
            {
                _regex = null;
            }
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
}
