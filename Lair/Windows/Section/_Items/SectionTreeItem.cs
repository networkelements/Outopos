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
    [DataContract(Name = "SectionTreeItem", Namespace = "http://Lair/Windows")]
    class SectionTreeItem : ICloneable<SectionTreeItem>, IThisLock
    {
        private Section _tag;
        private string _leaderSignature;
        private string _uploadSignature;

        private Exchange _exchange;
        private LockedList<string> _trustSignatures;
        private LockedList<Archive> _archives;
        private LockedList<Chat> _chats;

        private LockedList<SectionProfilePack> _sectionProfilePacks;

        private ChatCategorizeTreeItem _chatCategorizeTreeItem;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SectionTreeItem(Section section)
        {
            this.Tag = section;
            this.Exchange = new Exchange(ExchangeAlgorithm.Rsa2048);
            this.ChatCategorizeTreeItem = new ChatCategorizeTreeItem();
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

        [DataMember(Name = "Exchange")]
        public Exchange Exchange
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _exchange;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _exchange = value;
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

        [DataMember(Name = "TrustSignatures")]
        public LockedList<string> TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new LockedList<string>();

                    return _trustSignatures;
                }
            }
        }

        [DataMember(Name = "Chats")]
        public LockedList<Chat> Chats
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chats == null)
                        _chats = new LockedList<Chat>();

                    return _chats;
                }
            }
        }

        [DataMember(Name = "Archives")]
        public LockedList<Archive> Archives
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_archives == null)
                        _archives = new LockedList<Archive>();

                    return _archives;
                }
            }
        }

        [DataMember(Name = "SectionProfilePacks")]
        public LockedList<SectionProfilePack> SectionProfilePacks
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_sectionProfilePacks == null)
                        _sectionProfilePacks = new LockedList<SectionProfilePack>();

                    return _sectionProfilePacks;
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
