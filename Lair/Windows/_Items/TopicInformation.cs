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
    [DataContract(Name = "TopicInformation", Namespace = "http://Lair/Windows")]
    class TopicInformation : IDeepCloneable<TopicInformation>, IThisLock
    {
        private bool _isNew;
        private Topic _message;
        private TopicContent _messageContent;

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

        [DataMember(Name = "Topic")]
        public Topic Topic
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


        [DataMember(Name = "TopicContent")]
        public TopicContent TopicContent
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

        #region IDeepClone<TopicInformation>

        public TopicInformation DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(TopicInformation));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (TopicInformation)ds.ReadObject(textDictionaryReader);
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
