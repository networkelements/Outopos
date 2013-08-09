using System.Windows;
using System.Windows.Controls;
using Lair.Properties;
using Library.Collections;
using Library.Net.Lair;

namespace Lair.Windows
{
    class ChannelTreeViewItem : TreeViewItem
    {
        private ChannelTreeItem _value;

        private TextBlock _header = new TextBlock();

        public ChannelTreeViewItem(ChannelTreeItem channelTreeItem)
            : base()
        {
            this.Value = channelTreeItem;

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
            string suffix;

            if (!_value.IsTrustFilterEnabled)
            {
                suffix = "!";
            }

            LockedHashSet<MessageItem> lockedMessageItems;

            if (Settings.Instance.Global_LockedMessageItems.TryGetValue(_value.Channel, out lockedMessageItems))
            {
                _header.Text = string.Format("{0} ({1}-{2}) {3}", _value.Channel.Name, _value.MessageItems.Count, lockedMessageItems.Count, suffix);
            }
            else
            {
                _header.Text = string.Format("{0} ({1}) {2}", _value.Channel.Name, _value.MessageItems.Count, suffix);
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
