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
using a = Library.Net.Lair;

namespace Lair.Windows
{
    /// <summary>
    /// Interaction logic for SectionControl.xaml
    /// </summary>
    partial class SectionControl : UserControl
    {
        private MainWindow _mainWindow = (MainWindow)Application.Current.MainWindow;
        private BufferManager _bufferManager;
        private LairManager _lairManager;

        private Thread _searchThread = null;

        private volatile bool _refresh = false;

        private static Random _random = new Random();

        private SectionCategorizeTreeViewItem _treeViewItem;

        private Dictionary<Tag, SectionManager> _sectionManagers = new Dictionary<Tag, SectionManager>();

        public SectionControl(LairManager lairManager, BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
            _lairManager = lairManager;

            _treeViewItem = new SectionCategorizeTreeViewItem(Settings.Instance.SectionControl_SectionCategorizeTreeItem);

            InitializeComponent();

            _treeView.Items.Add(_treeViewItem);

            try
            {
                _treeViewItem.IsSelected = true;
            }
            catch (Exception)
            {

            }

            _mainWindow._tabControl.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
            {
                if (e.OriginalSource != _mainWindow._tabControl) return;

                if (_mainWindow.SelectedTab == MainWindowTabType.Section)
                {
                    if (!_refresh) this.Update_Title();
                }
            };

            _searchThread = new Thread(new ThreadStart(this.Search));
            _searchThread.Priority = ThreadPriority.Highest;
            _searchThread.IsBackground = true;
            _searchThread.Name = "SectionControl_SearchThread";
            _searchThread.Start();

            this.Update();
        }

        private void Search()
        {
            try
            {
                for (; ; )
                {
                    Thread.Sleep(100);
                    if (!_refresh) continue;

                    TreeViewItem tempTreeViewItem = null;

                    this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                    {
                        tempTreeViewItem = (TreeViewItem)_treeView.SelectedItem;
                    }));

                    if (tempTreeViewItem is SectionCategorizeTreeViewItem)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            this.Update_Title();
                        }));
                    }
                    else if (tempTreeViewItem is SectionTreeViewItem)
                    {
                        var tagTreeViewItem = (SectionTreeViewItem)tempTreeViewItem;

                        this.Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(() =>
                        {
                            if (tempTreeViewItem != _treeView.SelectedItem) return;
                            _refresh = false;

                            SectionManager sectionManager;

                            if (!_sectionManagers.TryGetValue(tagTreeViewItem.Value.Tag, out sectionManager))
                            {
                                sectionManager = _lairManager.GetSectionManager(tagTreeViewItem.Value.Tag, tagTreeViewItem.Value.LeaderSignature);
                                _sectionManagers[tagTreeViewItem.Value.Tag] = sectionManager;
                            }

                            if (tagTreeViewItem.ChatControl == null)
                            {
                                var chatControl = new ChatControl(tagTreeViewItem.Value.ChatCategorizeTreeItem, _lairManager, sectionManager, _bufferManager);

                                tagTreeViewItem.ChatControl = chatControl;
                                _channelGrid.Children.Add(chatControl);
                            }

                            foreach (var item in _channelGrid.Children.OfType<ChatControl>())
                            {
                                if (item == tagTreeViewItem.ChatControl)
                                {
                                    item.Visibility = System.Windows.Visibility.Visible;
                                }
                                else
                                {
                                    item.Visibility = System.Windows.Visibility.Hidden;
                                }
                            }
                        }));
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private void Update()
        {
            Settings.Instance.SectionControl_SectionCategorizeTreeItem = _treeViewItem.Value;

            _mainWindow.Title = string.Format("Lair {0}", App.LairVersion);
            _refresh = true;
        }

        private void Update_Title()
        {
            if (_refresh) return;

            if (_mainWindow.SelectedTab == MainWindowTabType.Section)
            {
                if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
                {
                    var selectTreeViewItem = (SectionCategorizeTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectTreeViewItem.Value.Name);
                }
                else if (_treeView.SelectedItem is SectionTreeViewItem)
                {
                    var selectTreeViewItem = (SectionTreeViewItem)_treeView.SelectedItem;

                    _mainWindow.Title = string.Format("Lair {0} - {1}", App.LairVersion, selectTreeViewItem.Value.Tag.Name);
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

                if (sourceItem is SectionCategorizeTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is SectionCategorizeTreeViewItem)
                    {
                        var s = (SectionCategorizeTreeViewItem)sourceItem;
                        var d = (SectionCategorizeTreeViewItem)destinationItem;

                        if (d.Value.Children.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is SectionCategorizeTreeViewItem)
                        {
                            var p = (SectionCategorizeTreeViewItem)parentItem;

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
                else if (sourceItem is SectionTreeViewItem)
                {
                    var destinationItem = (TreeViewItem)_treeView.GetCurrentItem(e.GetPosition);

                    if (destinationItem is SectionCategorizeTreeViewItem)
                    {
                        var s = (SectionTreeViewItem)sourceItem;
                        var d = (SectionCategorizeTreeViewItem)destinationItem;

                        if (d.Value.SectionTreeItems.Any(n => object.ReferenceEquals(n, s.Value))) return;
                        if (_treeView.GetAncestors(d).Any(n => object.ReferenceEquals(n, s))) return;

                        var parentItem = (TreeViewItem)s.Parent;

                        if (parentItem is SectionCategorizeTreeViewItem)
                        {
                            var p = (SectionCategorizeTreeViewItem)parentItem;

                            var tItems = p.Value.SectionTreeItems.Where(n => !object.ReferenceEquals(n, s.Value)).ToArray();
                            p.Value.SectionTreeItems.Clear();
                            p.Value.SectionTreeItems.AddRange(tItems);

                            p.Update();
                        }

                        d.IsSelected = true;
                        d.Value.SectionTreeItems.Add(s.Value);
                        d.Update();
                    }
                }

                this.Update();
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

        private void _sectionCategorizeTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void _sectionCategorizeTreeViewItemNewSectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow();
            window.Owner = _mainWindow;
            window.Title = LanguagesManager.Instance.SectionControl_NewSection_NameWindowTitle;

            if (window.ShowDialog() == true)
            {
                byte[] id = new byte[64];
                (new System.Security.Cryptography.RNGCryptoServiceProvider()).GetBytes(id);

                var tag = new Tag(id, window.Text);
                var sectionTreeItem = new SectionTreeItem(tag);

                sectionTreeItem.ChatCategorizeTreeItem = new ChatCategorizeTreeItem();

                SectionTreeItemEditWindow sectionTreeItemEditWindow = new SectionTreeItemEditWindow(sectionTreeItem, _lairManager, _bufferManager);
                sectionTreeItemEditWindow.Owner = _mainWindow;

                if (sectionTreeItemEditWindow.ShowDialog() == true)
                {
                    selectTreeViewItem.Value.SectionTreeItems.Add(sectionTreeItem);

                    selectTreeViewItem.Update();
                }
            }

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemNewCategoryMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            NameWindow window = new NameWindow();
            window.Owner = _mainWindow;

            if (window.ShowDialog() == true)
            {
                selectTreeViewItem.Value.Children.Add(new SectionCategorizeTreeItem() { Name = window.Text });

                selectTreeViewItem.Update();
            }

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var parent = (SectionCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null || selectTreeViewItem == _treeViewItem) return;

            Clipboard.SetSectionCategorizeTreeItems(new SectionCategorizeTreeItem[] { selectTreeViewItem.Value });

            var parent = (SectionCategorizeTreeViewItem)selectTreeViewItem.Parent;

            parent.IsSelected = true;
            parent.Value.Children.Remove(selectTreeViewItem.Value);
            parent.Update();

            this.Update();
        }

        private void _sectionCategorizeTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionCategorizeTreeItems(new SectionCategorizeTreeItem[] { selectTreeViewItem.Value });
        }

        private void _sectionCategorizeTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sectionTreeViewItems = new List<SectionTreeViewItem>();

            {
                var categorizeSectionTreeViewItems = new List<SectionCategorizeTreeViewItem>();
                categorizeSectionTreeViewItems.Add(_treeViewItem);

                for (int i = 0; i < categorizeSectionTreeViewItems.Count; i++)
                {
                    categorizeSectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionCategorizeTreeViewItem>());
                    sectionTreeViewItems.AddRange(categorizeSectionTreeViewItems[i].Items.OfType<SectionTreeViewItem>());
                }
            }

            var sb = new StringBuilder();

            foreach (var item in sectionTreeViewItems)
            {
                var link = new Link(item.Value.Tag, "Section", null);

                sb.AppendLine(LairConverter.ToLinkString(link));
                sb.AppendLine(MessageConverter.ToInfoMessage(link));
                sb.AppendLine();
            }

            Clipboard.SetText(sb.ToString().TrimEnd('\r', '\n'));
        }

        private void _sectionCategorizeTreeViewItemPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionCategorizeTreeViewItem;
            if (selectTreeViewItem == null) return;

            foreach (var item in Clipboard.GetSectionCategorizeTreeItems())
            {
                selectTreeViewItem.Value.Children.Add(item);
            }

            foreach (var item in Clipboard.GetSectionTreeItems())
            {
                selectTreeViewItem.Value.SectionTreeItems.Add(item);
            }

            selectTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeViewItemContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectTreeViewItem = sender as SectionTreeViewItem;
            if (selectTreeViewItem == null || _treeView.SelectedItem != selectTreeViewItem) return;

            var contextMenu = selectTreeViewItem.ContextMenu as ContextMenu;
            if (contextMenu == null) return;

        }

        private void _sectionTreeViewItemDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            if (MessageBox.Show(_mainWindow, LanguagesManager.Instance.MainWindow_Delete_Message, "Section", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK) return;

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<SectionCategorizeTreeViewItem>().LastOrDefault() as SectionCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.SectionTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeViewItemCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionTreeItems(new SectionTreeItem[] { selectTreeViewItem.Value });

            var searchTreeViewItem = _treeView.GetAncestors((TreeViewItem)_treeView.SelectedItem).OfType<SectionCategorizeTreeViewItem>().LastOrDefault() as SectionCategorizeTreeViewItem;
            if (searchTreeViewItem == null) return;

            searchTreeViewItem.IsSelected = true;

            searchTreeViewItem.Value.SectionTreeItems.Remove(selectTreeViewItem.Value);
            searchTreeViewItem.Update();

            this.Update();
        }

        private void _sectionTreeViewItemCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            Clipboard.SetSectionTreeItems(new SectionTreeItem[] { selectTreeViewItem.Value });
        }

        private void _sectionTreeViewItemCopyInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectTreeViewItem = _treeView.SelectedItem as SectionTreeViewItem;
            if (selectTreeViewItem == null) return;

            var sb = new StringBuilder();

            var link = new Link(selectTreeViewItem.Value.Tag, "Section", null);
            sb.AppendLine(LairConverter.ToLinkString(link));
            sb.AppendLine(MessageConverter.ToInfoMessage(link));

            Clipboard.SetText(sb.ToString());
        }

        #endregion

        private void Execute_New(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemNewCategoryMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {

            }
        }

        private void Execute_Delete(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemDeleteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeViewItemDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemCopyMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeViewItemCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemCutMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {
                _sectionTreeViewItemCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (_treeView.SelectedItem is SectionCategorizeTreeViewItem)
            {
                _sectionCategorizeTreeViewItemPasteMenuItem_Click(null, null);
            }
            else if (_treeView.SelectedItem is SectionTreeViewItem)
            {

            }
        }
    }
}
