using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Lair;
using Library.Security;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;

namespace Lair.Windows
{
    class SectionTreeViewItem : TreeViewItem
    {
        private SectionTreeItem _value;

        private TextBlock _header = new TextBlock();

        public SectionTreeViewItem(SectionTreeItem sectionTreeItem)
            : base()
        {
            this.Value = sectionTreeItem;

            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            _header.Text = MessageConverter.ToSectionString(this.Value.Section);
        }

        public SectionTreeItem Value
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
