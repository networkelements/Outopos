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
using Library.Io;

namespace Lair.Windows
{
    [DataContract(Name = "SectionTreeItem", Namespace = "http://Lair/Windows")]
    class SectionTreeItem : ICloneable<SectionTreeItem>, IThisLock
    {
        private Tag _tag;
        private string _leaderSignature;

        private string _uploadSignature;

        private ChatCategorizeTreeItem _chatCategorizeTreeItem;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SectionTreeItem(Tag tag)
        {
            this.Tag = tag;
        }

        [DataMember(Name = "Tag")]
        public Tag Tag
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _tag;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
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

        [DataMember(Name = "ChatCategorizeTreeItem")]
        public ChatCategorizeTreeItem ChatCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _chatCategorizeTreeItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _chatCategorizeTreeItem = value;
                }
            }
        }

        #region ICloneable<SectionTreeItem>

        public SectionTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionTreeItem));

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
