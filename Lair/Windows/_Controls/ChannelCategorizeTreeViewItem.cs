using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Library;
using Library.Net.Lair;
using Library.Security;
using System.Windows.Input;

namespace Lair.Windows
{
    class ChannelCategorizeTreeViewItem : TreeViewItem
    {
        private ChannelCategorizeTreeItem _value;

        private ObservableCollection<TreeViewItem> _listViewItemCollection = new ObservableCollection<TreeViewItem>();
        private TextBlock _header = new TextBlock();

        public ChannelCategorizeTreeViewItem(ChannelCategorizeTreeItem value)
            : base()
        {
            if (value == null) throw new ArgumentNullException("value");

            base.ItemsSource = _listViewItemCollection;
            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Value = value;
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
            _header.Text = this.Value.Name;
            base.IsExpanded = this.Value.IsExpanded;

            foreach (var item in _listViewItemCollection.OfType<ChannelCategorizeTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<ChannelCategorizeTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _listViewItemCollection.Add(new ChannelCategorizeTreeViewItem(item));
                }
            }

            foreach (var item in _listViewItemCollection.OfType<ChannelTreeViewItem>().ToArray())
            {
                if (!_value.ChannelTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.ChannelTreeItems)
            {
                if (!_listViewItemCollection.OfType<ChannelTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    _listViewItemCollection.Add(new ChannelTreeViewItem(item));
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.Cast<TreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                if (x is ChannelCategorizeTreeViewItem)
                {
                    if (y is ChannelCategorizeTreeViewItem)
                    {
                        var vx = ((ChannelCategorizeTreeViewItem)x).Value;
                        var vy = ((ChannelCategorizeTreeViewItem)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.ChannelTreeItems.Count.CompareTo(vy.ChannelTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
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
                        var vx = ((ChannelTreeViewItem)x).Value;
                        var vy = ((ChannelTreeViewItem)y).Value;

                        int c = vx.Channel.Name.CompareTo(vy.Channel.Name);
                        if (c != 0) return c;
                        c = Collection.Compare(vx.Channel.Id, vy.Channel.Id);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is ChannelCategorizeTreeViewItem)
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

        public ChannelCategorizeTreeItem Value
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
}
