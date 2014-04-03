using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Outopos;
using Library.Security;
using System.Windows.Controls;

namespace Outopos.Windows
{
    class DigitalSignatureComboBoxItem : ComboBoxItem
    {
        private DigitalSignature _value;

        public DigitalSignatureComboBoxItem()
        {

        }

        public DigitalSignatureComboBoxItem(DigitalSignature digitalSignature)
        {
            this.Value = digitalSignature;
        }

        public void Update()
        {
            this.Content = this.Value.ToString();
        }

        public DigitalSignature Value
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
