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
    [DataContract(Name = "SectionTreeItem", Namespace = "http://Outopos/Windows")]
    class SectionTreeItem : ICloneable<SectionTreeItem>, IThisLock
    {
        private Section _tag;
        private string _leaderSignature;
        private string _uploadSignature;

        private LockedList<SectionProfileInfo> _trustSectionProfileInfos;

        private PersonalInformation _personalInformation;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SectionTreeItem(Section tag)
        {
            this.Tag = tag;
            this.PersonalInformation = new PersonalInformation();
        }

        [DataMember(Name = "Tag")]
        public Section Tag
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

        [DataMember(Name = "TrustSectionProfileInfos")]
        public LockedList<SectionProfileInfo> TrustSectionProfileInfos
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSectionProfileInfos == null)
                        _trustSectionProfileInfos = new LockedList<SectionProfileInfo>();

                    return _trustSectionProfileInfos;
                }
            }
        }

        [DataMember(Name = "PersonalInformation")]
        public PersonalInformation PersonalInformation
        {
            get
            {
                return _personalInformation;
            }
            private set
            {
                _personalInformation = value;
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
                    using (XmlDictionaryWriter xmlDictionaryWriter = XmlDictionaryWriter.CreateBinaryWriter(wrapperStream))
                    {
                        ds.WriteObject(xmlDictionaryWriter, this);
                    }

                    stream.Position = 0;

                    using (XmlDictionaryReader xmlDictionaryReader = XmlDictionaryReader.CreateBinaryReader(stream, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionTreeItem)ds.ReadObject(xmlDictionaryReader);
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

    [DataContract(Name = "PersonalInformation", Namespace = "http://Outopos/Windows")]
    class PersonalInformation
    {
        private Exchange _exchange;
        private SignatureCollection _trustSignatures;
        private WikiCollection _wikis;
        private ChatCollection _chats;

        [DataMember(Name = "Exchange")]
        public Exchange Exchange
        {
            get
            {
                return _exchange;
            }
            set
            {
                _exchange = value;
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public SignatureCollection TrustSignatures
        {
            get
            {
                if (_trustSignatures == null)
                    _trustSignatures = new SignatureCollection();

                return _trustSignatures;
            }
        }

        [DataMember(Name = "Wikis")]
        public WikiCollection Wikis
        {
            get
            {
                if (_wikis == null)
                    _wikis = new WikiCollection();

                return _wikis;
            }
        }

        [DataMember(Name = "Chats")]
        public ChatCollection Chats
        {
            get
            {
                if (_chats == null)
                    _chats = new ChatCollection();

                return _chats;
            }
        }
    }
}
