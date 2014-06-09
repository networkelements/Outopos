using System;
using System.Windows;
using System.Windows.Controls;
using Outopos.Properties;
using Library.Collections;
using Library.Net.Outopos;
using Library;

namespace Outopos.Windows
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
                _header.Text = string.Format("{0} ({1}){2} - {3}", this.Value.Tag.Name, this.Value.ChatMessageInfos.Count, "!", NetworkConverter.ToBase64UrlString(this.Value.Tag.Id));
            }
            else
            {
                _header.Text = string.Format("{0} ({1}) - {2}", this.Value.Tag.Name, this.Value.ChatMessageInfos.Count, NetworkConverter.ToBase64UrlString(this.Value.Tag.Id));
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
