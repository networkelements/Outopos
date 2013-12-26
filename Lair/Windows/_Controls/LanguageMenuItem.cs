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
using System.Windows.Controls;
using Lair.Properties;

namespace Lair.Windows
{
    class LanguageMenuItem : MenuItem
    {
        private string _value;

        public LanguageMenuItem()
        {
            LanguagesManager.UsingLanguageChangedEvent += new UsingLanguageChangedEventHandler(this.LanguagesManager_UsingLanguageChangedEvent);
        }

        void LanguagesManager_UsingLanguageChangedEvent(object sender)
        {
            this.Update();
        }

        public string Value
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

        private void Update()
        {
            base.Header = LanguagesManager.Instance.Translate("Languages_" + _value) ?? _value;
        }
    }
}
