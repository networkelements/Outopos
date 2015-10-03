using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Library;
using Library.Net.Outopos;
using Library.Security;
using System.Windows.Input;

namespace Outopos.Windows
{
    class SignatureTreeViewItem : TreeViewItemEx
    {
        private SignatureTreeItem _value;

        private ObservableCollectionEx<SignatureTreeViewItem> _listViewItemCollection = new ObservableCollectionEx<SignatureTreeViewItem>();
        private TextBlock _header = new TextBlock();

        public SignatureTreeViewItem(SignatureTreeItem value)
            : base()
        {
            if (value == null) throw new ArgumentNullException("value");

            base.ItemsSource = _listViewItemCollection;
            base.Header = _header;
            base.IsExpanded = true;

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

        public void Update()
        {
            _header.Text = _value.Profile.Certificate.ToString();

            foreach (var item in _listViewItemCollection.OfType<SignatureTreeViewItem>().ToArray())
            {
                if (!_value.Children.Any(n => object.ReferenceEquals(n, item.Value)))
                {
                    _listViewItemCollection.Remove(item);
                }
            }

            foreach (var item in _value.Children)
            {
                if (!_listViewItemCollection.OfType<SignatureTreeViewItem>().Any(n => object.ReferenceEquals(n.Value, item)))
                {
                    var treeViewItem = new SignatureTreeViewItem(item);
                    treeViewItem.Parent = this;

                    _listViewItemCollection.Add(treeViewItem);
                }
            }

            this.Sort();
        }

        public void Sort()
        {
            var list = _listViewItemCollection.OfType<SignatureTreeViewItem>().ToList();

            list.Sort((x, y) =>
            {
                int c = x.Value.Profile.Certificate.ToString().CompareTo(y.Value.Profile.Certificate.ToString());
                if (c != 0) return c;

                return x.GetHashCode().CompareTo(y.GetHashCode());
            });

            for (int i = 0; i < list.Count; i++)
            {
                var o = _listViewItemCollection.IndexOf(list[i]);

                if (i != o) _listViewItemCollection.Move(o, i);
            }

            foreach (var item in this.Items.OfType<SignatureTreeViewItem>())
            {
                item.Sort();
            }
        }

        public SignatureTreeItem Value
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
