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
using Library.Collections;
using Library.Io;

namespace Outopos.Windows
{
    [DataContract(Name = "MailCategorizeTreeItem", Namespace = "http://Outopos/Windows")]
    class MailCategorizeTreeItem : ICloneable<MailCategorizeTreeItem>, IThisLock
    {
        private string _name;
        private LockedList<MailTreeItem> _chatTreeItems;
        private LockedList<MailCategorizeTreeItem> _children;
        private bool _isExpanded = true;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public MailCategorizeTreeItem()
        {

        }

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

        [DataMember(Name = "MailTreeItems")]
        public LockedList<MailTreeItem> MailTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chatTreeItems == null)
                        _chatTreeItems = new LockedList<MailTreeItem>();

                    return _chatTreeItems;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<MailCategorizeTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<MailCategorizeTreeItem>();

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

        #region ICloneable<MailCategorizeTreeItem>

        public MailCategorizeTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(MailCategorizeTreeItem));

                using (BufferStream stream = new BufferStream(BufferManager.Instance))
                {
                    using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (MailCategorizeTreeItem)ds.ReadObject(textDictionaryReader);
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
                if (_thisLock == null)
                {
                    lock (_initializeLock)
                    {
                        if (_thisLock == null)
                        {
                            _thisLock = new object();
                        }
                    }
                }

                return _thisLock;
            }
        }

        #endregion
    }
}
