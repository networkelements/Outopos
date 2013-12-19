using System;
using System.Windows;
using System.Windows.Controls;
using Lair.Properties;
using Library.Collections;
using Library.Net.Lair;

namespace Lair.Windows
{
    class ChatTreeViewItem : TreeViewItemEx
    {
        private ChatTreeItem _value;

        private TextBlock _header = new TextBlock();

        public ChatTreeViewItem(ChatTreeItem value)
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
            if (!_value.IsTrustEnabled)
            {
                _header.Text = string.Format("{0} ({1}) {2}", _value.Tag.Name, this.Value.ReadChatMessages.Count + this.Value.UnreadChatMessages.Count, "!");
            }
            else
            {
                _header.Text = string.Format("{0} ({1})", _value.Tag.Name, this.Value.ReadChatMessages.Count + this.Value.UnreadChatMessages.Count);
            }
        }

        public ChatTreeItem Value
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
