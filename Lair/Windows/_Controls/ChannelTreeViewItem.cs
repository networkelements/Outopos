using System.Windows;
using System.Windows.Controls;
using Lair.Properties;
using Library.Collections;
using Library.Net.Lair;
using System;

namespace Lair.Windows
{
    class ChannelTreeViewItem : TreeViewItem
    {
        private ChannelTreeItem _value;

        private TextBlock _header = new TextBlock();

        public ChannelTreeViewItem(ChannelTreeItem value)
            : base()
        {
            if (value == null) throw new ArgumentNullException("value");

            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Value = value;
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            if (!_value.IsTrustFilterEnabled)
            {
                _header.Text = string.Format("{0} ({1}) {2}", _value.Channel.Name, _value.MessageInformation.Count, "!");
            }
            else
            {
                _header.Text = string.Format("{0} ({1})", _value.Channel.Name, _value.MessageInformation.Count);
            }
        }

        public ChannelTreeItem Value
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
