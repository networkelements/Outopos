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
    class MailCategorizeTreeViewItem : TreeViewItemEx
    {
        private MailCategorizeTreeItem _value;

        private ObservableCollection<TreeViewItem> _listViewItemCollection = new ObservableCollection<TreeViewItem>();
        private TextBlock _header = new TextBlock();

        public MailCategorizeTreeViewItem(MailCategorizeTreeItem value)
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

            foreach (var item in _listViewItemCollection.OfType<MailCategorizeTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<MailCategorizeTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new MailCategorizeTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            foreach (var item in _listViewItemCollection.OfType<MailTreeViewItem>().ToArray())
            {
                if (!_value.MailTreeItems.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.MailTreeItems)
            {
                if (!_listViewItemCollection.OfType<MailTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new MailTreeViewItem(item);
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
                if (x is MailCategorizeTreeViewItem)
                {
                    if (y is MailCategorizeTreeViewItem)
                    {
                        var vx = ((MailCategorizeTreeViewItem)x).Value;
                        var vy = ((MailCategorizeTreeViewItem)y).Value;

                        int c = vx.Name.CompareTo(vy.Name);
                        if (c != 0) return c;
                        c = vx.MailTreeItems.Count.CompareTo(vy.MailTreeItems.Count);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is MailTreeViewItem)
                    {
                        return 1;
                    }
                }
                else if (x is MailTreeViewItem)
                {
                    if (y is MailTreeViewItem)
                    {
                        var vx = ((MailTreeViewItem)x).Value;
                        var vy = ((MailTreeViewItem)y).Value;

                        int c = vx.TargetSignature.CompareTo(vy.TargetSignature);
                        if (c != 0) return c;
                        c = Collection.Compare(vx.TargetSignature, vy.TargetSignature);
                        if (c != 0) return c;
                        c = vx.GetHashCode().CompareTo(vy.GetHashCode());
                        if (c != 0) return c;
                    }
                    else if (y is MailCategorizeTreeViewItem)
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

        public MailCategorizeTreeItem Value
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
