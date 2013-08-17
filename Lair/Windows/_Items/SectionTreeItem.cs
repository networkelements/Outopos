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
    [DataContract(Name = "SectionTreeItem", Namespace = "http://Lair/Windows")]
    class SectionTreeItem : IDeepCloneable<SectionTreeItem>, IThisLock
    {
        private Section _section;
        private string _leaderSignature;
        private string _uploadSignature;

        private ChannelCategorizeTreeItem _channelCategorizeTreeItem;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public SectionTreeItem()
        {

        }

        [DataMember(Name = "Section")]
        public Section Section
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _section;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _section = value;
                }
            }
        }

        [DataMember(Name = "LeaderSignature")]
        public string LeaderSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _leaderSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _leaderSignature = value;
                }
            }
        }

        [DataMember(Name = "UploadSignature")]
        public string UploadSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _uploadSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _uploadSignature = value;
                }
            }
        }

        [DataMember(Name = "ChannelCategorizeTreeItem")]
        public ChannelCategorizeTreeItem ChannelCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _channelCategorizeTreeItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _channelCategorizeTreeItem = value;
                }
            }
        }

        #region IDeepClone<SectionTreeItem>

        public SectionTreeItem DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionTreeItem));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionTreeItem)ds.ReadObject(textDictionaryReader);
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
