using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using Library;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    class TagCategorizeTreeViewItem : TreeViewItemEx
    {
        private TagCategorizeTreeItem _value;

        private ObservableCollection<TreeViewItem> _listViewItemCollection = new ObservableCollection<TreeViewItem>();
        private TextBlock _header = new TextBlock();

        public TagCategorizeTreeViewItem(TagCategorizeTreeItem value)
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

            foreach (var item in _listViewItemCollection.OfType<TagCategorizeTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<TagCategorizeTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new TagCategorizeTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            foreach (var item in _listViewItemCollection.OfType<TagTreeViewItem>().ToArray())
            {
                if (!_value.TagTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.TagTreeItems)
            {
                if (!_listViewItemCollection.OfType<TagTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new TagTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.Cast<TreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                if (x is TagCategorizeTreeViewItem)
                {
                    if (y is TagCategorizeTreeViewItem)
                    {
                        var vx = ((TagCategorizeTreeViewItem)x).Value;
                        var vy = ((TagCategorizeTreeViewItem)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.TagTreeItems.Count.CompareTo(vy.TagTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is TagTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is TagTreeViewItem)
                {
                    if (y is TagTreeViewItem)
                    {
                        var vx = ((TagTreeViewItem)x).Value;
                        var vy = ((TagTreeViewItem)y).Value;

                        int c = vx.Tag.Name.CompareTo(vy.Tag.Name);
                        if (c != 0) return c;
                        c = Collection.Compare(vx.Tag.Id, vy.Tag.Id);
                        if (c != 0) return c;
                        c = vx.LeaderSignature.CompareTo(vy.LeaderSignature);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is TagCategorizeTreeViewItem)
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

        public TagCategorizeTreeItem Value
        {
            get
            {
                return _value;
            }
            private set
            {
                _value = value;

                this.Update();
            }
        }
    }
}
