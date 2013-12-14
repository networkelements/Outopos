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

        private bool _isNewTopic;
        private ChatTopicPack _chatTopicPack;
        private LockedList<ChatMessagePack> _unreadChatMessagePacks;
        private LockedList<ChatMessagePack> _readChatMessagePacks;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public ChatTreeItem()
        {

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

        [DataMember(Name = "ChatTopicPack")]
        public ChatTopicPack ChatTopicPack
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chatTopicPack;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chatTopicPack = value;
                }
            }
        }

        [DataMember(Name = "UnreadChatMessagePacks")]
        public LockedList<ChatMessagePack> UnreadChatMessagePacks
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_unreadChatMessagePacks == null)
                        _unreadChatMessagePacks = new LockedList<ChatMessagePack>();

                    return _unreadChatMessagePacks;
                }
            }
        }

        [DataMember(Name = "ReadChatMessagePacks")]
        public LockedList<ChatMessagePack> ReadChatMessagePacks
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_readChatMessagePacks == null)
                        _readChatMessagePacks = new LockedList<ChatMessagePack>();

                    return _readChatMessagePacks;
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
