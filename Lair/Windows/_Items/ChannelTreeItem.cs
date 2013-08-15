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
    [DataContract(Name = "ChannelTreeItem", Namespace = "http://Lair/Windows")]
    class ChannelTreeItem : IDeepCloneable<ChannelTreeItem>, IThisLock
    {
        private Channel _channel;
        private bool _isTrustFilterEnabled = true;

        private Topic _topic;

        private LockedHashSet<Message> _messages;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        [DataMember(Name = "Channel")]
        public Channel Channel
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _channel;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _channel = value;
                }
            }
        }

        [DataMember(Name = "IsTrustFilterEnabled")]
        public bool IsTrustFilterEnabled
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isTrustFilterEnabled;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isTrustFilterEnabled = value;
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
                    return _topic;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _topic = value;
                }
            }
        }

        [DataMember(Name = "Messages")]
        public LockedHashSet<Message> Messages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_messages == null)
                        _messages = new LockedHashSet<Message>();

                    return _messages;
                }
            }
        }

        #region IDeepClone<ChannelTreeItem>

        public ChannelTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ChannelTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (ChannelTreeItem)ds.ReadObject(textDictionaryReader);
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
