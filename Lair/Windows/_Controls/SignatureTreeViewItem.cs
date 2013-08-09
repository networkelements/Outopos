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
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    class SignatureTreeViewItem : TreeViewItem
    {
        private string _signature;

        private TextBlock _header = new TextBlock();

        public SignatureTreeViewItem(string signature)
            : base()
        {
            _signature = signature;

            base.Header = _header;

            base.RequestBringIntoView += (object sender, RequestBringIntoViewEventArgs e) =>
            {
                e.Handled = true;
            };

            this.Update();
        }

        protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            this.IsSelected = true;

            e.Handled = true;
        }

        public void Update()
        {
            _header.Text = _signature;
        }

        public string Signature
        {
            get
            {
                return _signature;
            }
        }
    }
}
