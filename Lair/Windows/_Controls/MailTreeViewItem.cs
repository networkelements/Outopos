using System;
using System.Windows;
using System.Windows.Controls;
using Lair.Properties;
using Library.Collections;
using Library.Net.Lair;
using Library;

namespace Lair.Windows
{
    class MailTreeViewItem : TreeViewItemEx
    {
        private MailTreeItem _value;

        private TextBlock _header = new TextBlock();

        public MailTreeViewItem(MailTreeItem value)
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
            _header.Text = string.Format("{0} ({1})", this.Value.TargetSignature, this.Value.SentSectionMessages.Count + this.Value.ReadSectionMessages.Count + this.Value.UnreadSectionMessages.Count);
        }

        public MailTreeItem Value
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
