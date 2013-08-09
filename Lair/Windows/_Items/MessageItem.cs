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

namespace Lair.Windows
{
    class MessageItem : IEnumerable<MessageItem>, IDeepCloneable<MessageItem>, IThisLock
    {
        private Message _message;
        private MessageContent _content;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public override int GetHashCode()
        {
            if (_message == null) return 0;
            else return _message.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MessageItem)) return false;

            return this.Equals((MessageItem)obj);
        }

        public bool Equals(MessageItem other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Message != other.Message
                || this.Content != other.Content)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "Message")]
        public Message Message
        {
            get
            {
                return _message;
            }
            set
            {
                _message = value;
            }
        }

        [DataMember(Name = "Content")]
        public MessageContent Content
        {
            get
            {
                return _content;
            }
            set
            {
                _content = value;
            }
        }

        #region IDeepClone<MessageItem>

        public MessageItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(MessageItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (MessageItem)ds.ReadObject(textDictionaryReader);
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
