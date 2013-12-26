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
    class SectionCategorizeTreeViewItem : TreeViewItemEx
    {
        private SectionCategorizeTreeItem _value;

        private ObservableCollection<TreeViewItem> _listViewItemCollection = new ObservableCollection<TreeViewItem>();
        private TextBlock _header = new TextBlock();

        public SectionCategorizeTreeViewItem(SectionCategorizeTreeItem value)
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

            foreach (var item in _listViewItemCollection.OfType<SectionCategorizeTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<SectionCategorizeTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new SectionCategorizeTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            foreach (var item in _listViewItemCollection.OfType<SectionTreeViewItem>().ToArray())
            {
                if (!_value.SectionTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.SectionTreeItems)
            {
                if (!_listViewItemCollection.OfType<SectionTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new SectionTreeViewItem(item);
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
                if (x is SectionCategorizeTreeViewItem)
                {
                    if (y is SectionCategorizeTreeViewItem)
                    {
                        var vx = ((SectionCategorizeTreeViewItem)x).Value;
                        var vy = ((SectionCategorizeTreeViewItem)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.SectionTreeItems.Count.CompareTo(vy.SectionTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is SectionTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is SectionTreeViewItem)
                {
                    if (y is SectionTreeViewItem)
                    {
                        var vx = ((SectionTreeViewItem)x).Value;
                        var vy = ((SectionTreeViewItem)y).Value;

                        int c = vx.Tag.Name.CompareTo(vy.Tag.Name);
                        if (c != 0) return c;
                        c = Collection.Compare(vx.Tag.Id, vy.Tag.Id);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is SectionCategorizeTreeViewItem)
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

        public SectionCategorizeTreeItem Value
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
