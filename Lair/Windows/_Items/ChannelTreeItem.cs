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
        private bool _isTopicUpdated;

        private LockedHashSet<MessageItem> _messageItems;

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

        [DataMember(Name = "IsTopicUpdated")]
        public bool IsTopicUpdated
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _isTopicUpdated;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _isTopicUpdated = value;
                }
            }
        }

        [DataMember(Name = "MessageItems")]
        public LockedHashSet<MessageItem> MessageItems
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_messageItems == null)
                        _messageItems = new LockedHashSet<MessageItem>();

                    return _messageItems;
                }
            }
        }

        #region IDeepClone<ChannelItem>

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
