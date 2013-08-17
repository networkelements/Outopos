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
    [DataContract(Name = "MessageInformation", Namespace = "http://Lair/Windows")]
    class MessageInformation : IDeepCloneable<MessageInformation>, IThisLock
    {
        private bool _isNew;
        private Message _message;
        private MessageContent _messageContent;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

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

        [DataMember(Name = "Message")]
        public Message Message
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _message;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _message = value;
                }
            }
        }


        [DataMember(Name = "MessageContent")]
        public MessageContent MessageContent
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _messageContent;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _messageContent = value;
                }
            }
        }

        #region IDeepClone<MessageInformation>

        public MessageInformation DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(MessageInformation));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (MessageInformation)ds.ReadObject(textDictionaryReader);
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
