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
    [DataContract(Name = "ChatTreeItem", Namespace = "http://Lair/Windows")]
    class ChatTreeItem : ICloneable<ChatTreeItem>, IThisLock
    {
        private Chat _tag;

        private bool _isTrustEnabled = true;

        private bool _isNewTopic;
        private ChatTopic _chatTopic;
        private LockedList<ChatMessage> _unreadChatMessages;
        private LockedList<ChatMessage> _readChatMessages;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatTreeItem()
        {

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
            set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
                }
            }
        }

        [DataMember(Name = "IsNewTopic")]
        public bool IsNewTopic
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isNewTopic;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isNewTopic = value;
                }
            }
        }

        [DataMember(Name = "ChatTopic")]
        public ChatTopic ChatTopic
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chatTopic;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chatTopic = value;
                }
            }
        }

        [DataMember(Name = "UnreadChatMessages")]
        public LockedList<ChatMessage> UnreadChatMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_unreadChatMessages == null)
                        _unreadChatMessages = new LockedList<ChatMessage>();

                    return _unreadChatMessages;
                }
            }
        }

        [DataMember(Name = "ReadChatMessages")]
        public LockedList<ChatMessage> ReadChatMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_readChatMessages == null)
                        _readChatMessages = new LockedList<ChatMessage>();

                    return _readChatMessages;
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
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ChatTreeItem)ds.ReadObject(textDictionaryReader);
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
