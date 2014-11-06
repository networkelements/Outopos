using System;
using System.Windows;
using System.Windows.Controls;
using Outopos.Properties;
using Library.Collections;
using Library.Net.Outopos;
using Library;
using System.Linq;
using System.Text;

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
            var sb = new StringBuilder();

            {
                sb.Append(this.Value.Tag.Name);
            }

            sb.Append(' ');

            {
                sb.Append(string.Format("({0})", this.Value.ChatMessages.Count(n => n.Value.HasFlag(ChatMessageState.IsUnread))));
                if (!_value.IsTrustEnabled) sb.Append('!');
            }

            sb.Append(" - ");

            {
                sb.Append(NetworkConverter.ToBase64UrlString(this.Value.Tag.Id));
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
