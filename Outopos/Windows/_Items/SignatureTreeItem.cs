using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using Library;
using Library.Net.Outopos;
using Library.Security;
using Library.Io;
using Library.Collections;

namespace Outopos.Windows
{
    [DataContract(Name = "SignatureTreeItem", Namespace = "http://Outopos/Windows")]
    class SignatureTreeItem : ICloneable<SignatureTreeItem>, IThisLock
    {
        private ProfileInfo _sectionProfileInfo;
        private LockedList<SignatureTreeItem> _children;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SignatureTreeItem(ProfileInfo sectionProfileInfo)
        {
            this.ProfileInfo = sectionProfileInfo;
        }

        [DataMember(Name = "ProfileInfo")]
        public ProfileInfo ProfileInfo
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _sectionProfileInfo;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _sectionProfileInfo = value;
                }
            }
        }

        [DataMember(Name = "Children")]
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
                    using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(xmlDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SignatureTreeItem)ds.ReadObject(xmlDictionaryReader);
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
