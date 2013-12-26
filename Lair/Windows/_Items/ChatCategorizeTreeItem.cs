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
using Library.Io;

namespace Lair.Windows
{
    [DataContract(Name = "ChatCategorizeTreeItem", Namespace = "http://Lair/Windows")]
    class ChatCategorizeTreeItem : ICloneable<ChatCategorizeTreeItem>, IThisLock
    {
        private string _name;
        private LockedList<ChatTreeItem> _chatTreeItems;
        private LockedList<ChatCategorizeTreeItem> _children;
        private bool _isExpanded = true;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatCategorizeTreeItem()
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

        [DataMember(Name = "ChatTreeItems")]
        public LockedList<ChatTreeItem> ChatTreeItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chatTreeItems == null)
                        _chatTreeItems = new LockedList<ChatTreeItem>();

                    return _chatTreeItems;
                }
            }
        }

        [DataMember(Name = "Children")]
        public LockedList<ChatCategorizeTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<ChatCategorizeTreeItem>();

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

        #region ICloneable<ChatCategorizeTreeItem>

        public ChatCategorizeTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ChatCategorizeTreeItem));

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
                        return (ChatCategorizeTreeItem)ds.ReadObject(textDictionaryReader);
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
