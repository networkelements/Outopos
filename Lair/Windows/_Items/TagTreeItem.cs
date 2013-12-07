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
    [DataContract(Name = "TagTreeItem", Namespace = "http://Lair/Windows")]
    class TagTreeItem : ICloneable<TagTreeItem>, IThisLock
    {
        private Tag _tag;
        private string _uploadSignature;

        private SectionTreeItem _sectionTreeItem;
        private ChatCategorizeTreeItem _chatCategorizeTreeItem;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public TagTreeItem()
        {

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
            set
            {
                lock (this.ThisLock)
                {
                    _tag = value;
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

        [DataMember(Name = "SectionTreeItem")]
        public SectionTreeItem SectionTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _sectionTreeItem;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _sectionTreeItem = value;
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

        #region ICloneable<TagTreeItem>

        public TagTreeItem Clone()
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
                        return (TagTreeItem)ds.ReadObject(textDictionaryReader);
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
