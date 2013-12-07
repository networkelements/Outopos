using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Io;
using Library.Net.Lair;

namespace Lair.Windows
{
    [DataContract(Name = "ChatMessageInfo", Namespace = "http://Lair/Windows")]
    class ChatMessageInfo : ICloneable<ChatMessageInfo>, IThisLock
    {
        private bool _isNew;
        private Header _header;
        private ChatMessageContent _content;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        [DataMember(Name = "IsNew")]
        public bool IsNew
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isNew;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isNew = value;
                }
            }
        }

        [DataMember(Name = "Header")]
        public Header Header
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _header;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _header = value;
                }
            }
        }


        [DataMember(Name = "Content")]
        public ChatMessageContent Content
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _content;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _content = value;
                }
            }
        }

        #region ICloneable<ChatMessageInfo>

        public ChatMessageInfo Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(TagTreeItem));

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
                        return (ChatMessageInfo)ds.ReadObject(textDictionaryReader);
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
