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
        private Chat _chat;
        private bool _isTrustEnabled = true;
        private ChatTopicInformation _chatTopicInformation;
        private List<ChatMessageInformaiton> _chatMessageInformation;

        private readonly object _thisLock = new object();
        private static readonly object _initializeLock = new object();

        public ChatTreeItem()
        {

        }

        [DataMember(Name = "Chat")]
        public Chat Chat
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chat;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chat = value;
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

        [DataMember(Name = "ChatTopicInformation")]
        public ChatTopicInformation ChatTopicInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chatTopicInformation;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chatTopicInformation = value;
                }
            }
        }

        [DataMember(Name = "ChatMessageInformation")]
        public List<ChatMessageInformaiton> ChatMessageInformation
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chatMessageInformation == null)
                        _chatMessageInformation = new List<ChatMessageInformaiton>();

                    return _chatMessageInformation;
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
