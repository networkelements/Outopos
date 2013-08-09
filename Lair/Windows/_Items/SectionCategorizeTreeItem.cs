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
using Library.Collections;

namespace Lair.Windows
{
    [DataContract(Name = "SectionCategorizeTreeItem", Namespace = "http://Amoeba/Windows")]
    class SectionCategorizeTreeItem : IDeepCloneable<SectionCategorizeTreeItem>, IThisLock
    {
        private string _name;
        private LockedList<SectionTreeItem> _sectionTreeItems;
        private LockedList<SectionCategorizeTreeItem> _children;
        private bool _isExpanded = true;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Name")]
        public string Name
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _name;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _name = value;
                }
            }
        }

        [DataMember(Name = "SectionTreeItems")]
        public LockedList<SectionTreeItem> SectionTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_sectionTreeItems == null)
                        _sectionTreeItems = new LockedList<SectionTreeItem>();

                    return _sectionTreeItems;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<SectionCategorizeTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<SectionCategorizeTreeItem>();

                    return _children;
                }
            }
        }

        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isExpanded;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isExpanded = value;
                }
            }
        }

        #region IDeepClone<SectionCategorizeTreeItem>

        public SectionCategorizeTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionCategorizeTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionCategorizeTreeItem)ds.ReadObject(textDictionaryReader);
                    }
                }
            }
        }

        #endregion

        #region IThisLock

        public object ThisLock
        {
            get
            {
                lock (_thisStaticLock)
                {
                    if (_thisLock == null)
                        _thisLock = new object();

                    return _thisLock;
                }
            }
        }

        #endregion
    }
}
