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
using Library.Collections;

namespace Lair.Windows
{
    [DataContract(Name = "SignatureTreeItem", Namespace = "http://Lair/Windows")]
    class SignatureTreeItem : ICloneable<SignatureTreeItem>, IThisLock
    {
        private SectionProfilePack _sectionProfilePack;
        private LockedList<SignatureTreeItem> _children;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SignatureTreeItem(SectionProfilePack sectionProfilePack)
        {
            this.SectionProfilePack = sectionProfilePack;
        }

        [DataMember(Name = "SectionProfilePack")]
        public SectionProfilePack SectionProfilePack
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _sectionProfilePack;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _sectionProfilePack = value;
                }
            }
        }

        [DataMember(Name = "SignatureTreeItem")]
        public LockedList<SignatureTreeItem> Children
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_children == null)
                        _children = new LockedList<SignatureTreeItem>();

                    return _children;
                }
            }
        }

        #region ICloneable<SignatureTreeItem>

        public SignatureTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SignatureTreeItem));

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
                        return (SignatureTreeItem)ds.ReadObject(textDictionaryReader);
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
