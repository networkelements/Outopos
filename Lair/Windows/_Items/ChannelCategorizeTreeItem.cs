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
    [DataContract(Name = "ChannelCategorizeTreeItem", Namespace = "http://Amoeba/Windows")]
    class ChannelCategorizeTreeItem : IDeepCloneable<ChannelCategorizeTreeItem>, IThisLock
    {
        private string _name;
        private LockedList<ChannelTreeItem> _channelTreeItems;
        private LockedList<ChannelCategorizeTreeItem> _children;
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

        [DataMember(Name = "ChannelTreeItems")]
        public LockedList<ChannelTreeItem> ChannelTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_channelTreeItems == null)
                        _channelTreeItems = new LockedList<ChannelTreeItem>();

                    return _channelTreeItems;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<ChannelCategorizeTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<ChannelCategorizeTreeItem>();

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

        #region IDeepClone<ChannelCategorizeTreeItem>

        public ChannelCategorizeTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ChannelCategorizeTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ChannelCategorizeTreeItem)ds.ReadObject(textDictionaryReader);
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
