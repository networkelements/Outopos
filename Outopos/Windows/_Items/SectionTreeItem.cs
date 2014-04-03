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

        private string _comment;
        private Exchange _exchange;
        private LockedList<string> _trustSignatures;
        private LockedList<Wiki> _wikis;
        private LockedList<Chat> _chats;

        private LockedList<SectionProfile> _cacheSectionProfiles;

        private ChatCategorizeTreeItem _chatCategorizeTreeItem;
        private MailCategorizeTreeItem _mailCategorizeTreeItem;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public SectionTreeItem(Section section)
        {
            this.Tag = section;
            this.Exchange = new Exchange(ExchangeAlgorithm.Rsa2048);
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

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _comment;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _comment = value;
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

        [DataMember(Name = "Wikis")]
        public LockedList<Wiki> Wikis
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_wikis == null)
                        _wikis = new LockedList<Wiki>();

                    return _wikis;
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

        [DataMember(Name = "CacheSectionProfiles")]
        public LockedList<SectionProfile> CacheSectionProfiles
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_cacheSectionProfiles == null)
                        _cacheSectionProfiles = new LockedList<SectionProfile>();

                    return _cacheSectionProfiles;
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
                    if (_chatCategorizeTreeItem == null)
                    {
                        _chatCategorizeTreeItem = new ChatCategorizeTreeItem();
                        _chatCategorizeTreeItem.Name = "Category";
                    }

                    return _chatCategorizeTreeItem;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _chatCategorizeTreeItem = value;
                }
            }
        }

        [DataMember(Name = "MailCategorizeTreeItem")]
        public MailCategorizeTreeItem MailCategorizeTreeItem
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_mailCategorizeTreeItem == null)
                    {
                        _mailCategorizeTreeItem = new MailCategorizeTreeItem();
                        _mailCategorizeTreeItem.Name = "Category";
                    }

                    return _mailCategorizeTreeItem;
                }
            }
            private set
            {
                lock (this.ThisLock)
                {
                    _mailCategorizeTreeItem = value;
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
