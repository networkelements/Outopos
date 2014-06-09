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
    [DataContract(Name = "ChatTreeItem", Namespace = "http://Outopos/Windows")]
    class ChatTreeItem : ICloneable<ChatTreeItem>, IThisLock
    {
        private Chat _tag;

        private bool _isTrustEnabled = true;

        private ChatTopicInfo _chatTopicInfo;
        private LockedList<ChatMessageInfo> _chatMessageInfos;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatTreeItem(Chat tag)
        {
            this.Tag = tag;
        }

        [DataMember(Name = "Tag")]
        public Chat Tag
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _tag;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
                }
            }
        }

        [DataMember(Name = "IsTrustEnabled")]
        public bool IsTrustEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isTrustEnabled;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isTrustEnabled = value;
                }
            }
        }

        [DataMember(Name = "ChatTopicInfo")]
        public ChatTopicInfo ChatTopicInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chatTopicInfo;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chatTopicInfo = value;
                }
            }
        }

        [DataMember(Name = "ChatMessageInfos")]
        public LockedList<ChatMessageInfo> ChatMessageInfos
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chatMessageInfos == null)
                        _chatMessageInfos = new LockedList<ChatMessageInfo>();

                    return _chatMessageInfos;
                }
            }
        }

        #region ICloneable<ChatTreeItem>

        public ChatTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ChatTreeItem));

                using (BufferStream stream = new BufferStream(BufferManager.Instance))
                {
                    using (WrapperStream wrapperStream = new WrapperStream(stream, true))
                    using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(xmlDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ChatTreeItem)ds.ReadObject(xmlDictionaryReader);
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
