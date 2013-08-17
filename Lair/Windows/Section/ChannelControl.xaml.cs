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
    /// Interaction logic for ChannelControl.xaml
    /// </summary>
    partial class ChannelControl : UserControl
    {
        private MainWindow _mainWindow;
        private LairManager _lairManager;
        private BufferManager _bufferManager;

        private Thread _searchThread = null;
        private Thread _cacheThread = null;

        private volatile bool _refresh = false;
        private volatile bool _update = false;
        private AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        private static Random _random = new Random();

        private ChannelCategorizeTreeViewItem _treeViewItem;
        private string _uploadSignature;

        private LockedHashSet<string> _trustSignatures = new LockedHashSet<string>();

        public ChannelControl(MainWindow mainWindow, LairManager lairManager, BufferManager bufferManager,
            ref ChannelCategorizeTreeItem channelCategorizeTreeItem, string uploadSignature)
        {
            _mainWindow = mainWindow;
            _lairManager = lairManager;
            _bufferManager = bufferManager;

            _treeViewItem = new ChannelCategorizeTreeViewItem(channelCategorizeTreeItem);
            _uploadSignature = uploadSignature;

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "ChannelControl_SearchThread";
            _searchThread.Start();

            _cacheThread = new Thread(new ThreadStart(this.Cache));
            _cacheThread.Priority = ThreadPriority.Highest;
            _cacheThread.IsBackground = true;
            _cacheThread.Name = "ChannelControl_CacheThread";
            _cacheThread.Start();

            _searchRowDefinition.Height = new GridLength(0);

            _trustToggleButton.IsEnabled = false;
            _topicUploadButton.IsEnabled = false;
            _messageUploadButton.IsEnabled = false;

            this.Update();

            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);
        }

        public LockedHashSet<string> TrustSignatures
        {
            get
            {
                return _trustSignatures;
            }
        }

        private void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {

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

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (App.SelectTab != TabItemType.Section) return;

                        if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
                        {
                            _refresh = false;

                            _trustToggleButton.IsEnabled = false;
                            _trustToggleButton.ClearValue(ToggleButton.ForegroundProperty);

                            _topicUploadButton.IsEnabled = false;
                            _messageUploadButton.IsEnabled = false;
                        }
                        else if (_treeView.SelectedItem is ChannelTreeViewItem)
                        {
                            selectTreeViewItem = (ChannelTreeViewItem)_treeView.SelectedItem;

                            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _uploadSignature);

                            {
                                _trustToggleButton.IsEnabled = true;

                                if (selectTreeViewItem.Value.IsTrustFilterEnabled)
                                {
                                    _trustToggleButton.Foreground = new SolidColorBrush(App.Colors.Trust_On);
                                }
                                else
                                {
                                    _trustToggleButton.Foreground = new SolidColorBrush(App.Colors.Trust_Off);
                                }
                            }

                            _topicUploadButton.IsEnabled = (digitalSignature != null);
                            _messageUploadButton.IsEnabled = (digitalSignature != null);
                        }
                    }));

                    if (selectTreeViewItem == null) continue;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        if (selectTreeViewItem != _treeView.SelectedItem) return;
                        _refresh = false;

                        this.Update_TreeView_Color();
                    }));
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
                    var channelTreeViewItems = new List<ChannelTreeViewItem>();

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        var categorizeChannelTreeViewItems = new List<ChannelCategorizeTreeViewItem>();
                        categorizeChannelTreeViewItems.Add(_treeViewItem);

                        for (int i = 0; i < categorizeChannelTreeViewItems.Count; i++)
                        {
                            categorizeChannelTreeViewItems.AddRange(categorizeChannelTreeViewItems[i].Items.OfType<ChannelCategorizeTreeViewItem>());
                            channelTreeViewItems.AddRange(categorizeChannelTreeViewItems[i].Items.OfType<ChannelTreeViewItem>());
                        }
                    }));

                    foreach (var treeViewItem in channelTreeViewItems)
                    {
                        bool isUpdate = false;

                        // Topic
                        {
                            Dictionary<string, Topic> topicDictionary = new Dictionary<string, Topic>();

                            foreach (var item in _lairManager.GetTopics(treeViewItem.Value.Channel))
                            {
                                topicDictionary[item.Certificate.ToString()] = item;
                            }

                            List<Topic> topics = new List<Topic>();

                            foreach (var signature in _trustSignatures)
                            {
                                Topic tempTopic;

                                if (topicDictionary.TryGetValue(signature, out tempTopic))
                                {
                                    topics.Add(tempTopic);
                                }
                            }

                            topics.Sort((x, y) =>
                            {
                                return y.CreationTime.CompareTo(x.CreationTime);
                            });

                            lock (treeViewItem.Value.ThisLock)
                            {
                                foreach (var topic in topics)
                                {
                                    try
                                    {
                                        if (treeViewItem.Value.TopicInformation.Topic != topic)
                                        {
                                            var information = new TopicInformation();
                                            information.IsNew = true;
                                            information.Topic = topic;
                                            information.TopicContent = _lairManager.GetContent(topic);

                                            treeViewItem.Value.TopicInformation = information;
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

                        // Message
                        {
                            var oldList = new HashSet<Message>();

                            lock (treeViewItem.Value.ThisLock)
                            {
                                lock (treeViewItem.Value.ThisLock)
                                {
                                    oldList.UnionWith(treeViewItem.Value.MessageInformation.Select(n => n.Message));
                                }
                            }

                            HashSet<Message> newList = new HashSet<Message>(_lairManager.GetMessages(treeViewItem.Value.Channel));
                            newList.UnionWith(oldList);

                            {
                                var tempList = new List<Message>();

                                foreach (var message in newList)
                                {
                                    if (!_trustSignatures.Contains(message.Certificate.ToString())) continue;

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
                                foreach (var item in treeViewItem.Value.MessageInformation.ToArray())
                                {
                                    if (!newList.Contains(item.Message))
                                    {
                                        treeViewItem.Value.MessageInformation.Remove(item);
                                        isUpdate |= true;
                                    }
                                }

                                foreach (var item in newList)
                                {
                                    try
                                    {
                                        if (!oldList.Contains(item))
                                        {
                                            var information = new MessageInformation();
                                            information.IsNew = true;
                                            information.Message = item;
                                            information.MessageContent = _lairManager.GetContent(item);

                                            treeViewItem.Value.MessageInformation.Add(information);
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

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (isUpdate)
                            {
                                treeViewItem.Update();
                            }
                        }));
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
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
            _update = true;
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

                foreach (var item in items.OfType<ChannelTreeViewItem>()
                    .Where(n => n.Value.TopicInformation.IsNew || n.Value.MessageInformation.Any(m => m.IsNew)))
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

                if (sourceItem is ChannelCategorizeTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is ChannelCategorizeTreeViewItem)
                    {
                        var s = (ChannelCategorizeTreeViewItem)sourceItem;
                        var d = (ChannelCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetLineage(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)_treeView.GetParent(s);

                        if (parentItem is ChannelCategorizeTreeViewItem)
                        {
                            var p = (ChannelCategorizeTreeViewItem)parentItem;

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
                else if (sourceItem is ChannelTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is ChannelCategorizeTreeViewItem)
                    {
                        var s = (ChannelTreeViewItem)sourceItem;
                        var d = (ChannelCategorizeTreeViewItem)destinationItem;

                        if (d.Value.ChannelTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetLineage(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)_treeView.GetParent(s);

                        if (parentItem is ChannelCategorizeTreeViewItem)
                        {
                            var p = (ChannelCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.ChannelTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.ChannelTreeItems.Clear();
                            p.Value.ChannelTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.ChannelTreeItems.Add(s.Value);
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

        private void _channelCategorizeTreeViewContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemNewMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemEditMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Channel", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (ChannelCategorizeTreeViewItem)_treeView.GetParent(selectTreeViewItem);

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _channelCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetChannelCategorizeTreeItems(new ChannelCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parent = (ChannelCategorizeTreeViewItem)_treeView.GetParent(selectTreeViewItem);

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _channelCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetChannelCategorizeTreeItems(new ChannelCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _channelCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var channelTreeViewItems = new List<ChannelTreeViewItem>();

            {
                var categorizeChannelTreeViewItems = new List<ChannelCategorizeTreeViewItem>();
                categorizeChannelTreeViewItems.Add(_treeViewItem);

                for (int i = 0; i < categorizeChannelTreeViewItems.Count; i++)
                {
                    categorizeChannelTreeViewItems.AddRange(categorizeChannelTreeViewItems[i].Items.OfType<ChannelCategorizeTreeViewItem>());
                    channelTreeViewItems.AddRange(categorizeChannelTreeViewItems[i].Items.OfType<ChannelTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in channelTreeViewItems)
            {
                sb.AppendLine(LairConverter.ToChannelString(item.Value.Channel));
                sb.AppendLine(MessageConverter.ToInfoMessage(item.Value.Channel));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _channelCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetChannelCategorizeTreeItems())
            {
                selectTreeViewItem.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetChannelTreeItems())
            {
                selectTreeViewItem.Value.ChannelTreeItems.Add(item);
            }

            selectTreeViewItem.Update();

            this.Update_Cache();
        }

        private void _channelCategorizeTreeViewItemTrustOnMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemTrustOffMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemMarkAllMessagesReadMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelCategorizeTreeViewItemChannelListMenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void _channelTreeItemTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as ChannelTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

        }

        private void _channelTreeItemTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<ChannelCategorizeTreeViewItem>().LastOrDefault() as ChannelCategorizeTreeViewItem;
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

            var searchTreeViewItem = _treeView.GetLineage((TreeViewItem)_treeView.SelectedItem).OfType<ChannelCategorizeTreeViewItem>().LastOrDefault() as ChannelCategorizeTreeViewItem;
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

        private void _channelTreeItemTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            sb.AppendLine(LairConverter.ToChannelString(selectTreeViewItem.Value.Channel));
            sb.AppendLine(MessageConverter.ToInfoMessage(selectTreeViewItem.Value.Channel));

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        #region _richTextBox

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
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            selectTreeViewItem.Value.IsTrustFilterEnabled = !selectTreeViewItem.Value.IsTrustFilterEnabled;

            this.Update_Cache();

            e.Handled = true;
        }

        private void _topicUploadButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

        }

        private void _messageUploadButton_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as ChannelTreeViewItem;
            if (selectTreeViewItem == null) return;

            var digitalSignature = Settings.Instance.Global_DigitalSignatureCollection.FirstOrDefault(n => n.ToString() == _uploadSignature);
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
            if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
            {
                _channelCategorizeTreeViewItemNewMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
            {
                _channelCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
            {
                _channelCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
            {
                _channelCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                _channelTreeItemTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is ChannelCategorizeTreeViewItem)
            {
                _channelCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is ChannelTreeViewItem)
            {
                
            }
        }

        private void Execute_Search(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            _searchRowDefinition.Height = new GridLength(24);
            _searchTextBox.Focus();
        }
    }
}
