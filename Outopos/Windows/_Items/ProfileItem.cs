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
    [DataContract(Name = "ProfileItem", Namespace = "http://Outopos/Windows")]
    class ProfileItem : ICloneable<ProfileItem>, IThisLock
    {
        private string _uploadSignature;
        private Exchange _exchange;
        private LockedList<Exchange> _oldExchanges;
        private SignatureCollection _trustSignatures;
        private SignatureCollection _deleteSignatures;
        private WikiCollection _wikis;
        private ChatCollection _chats;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

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

        [DataMember(Name = "OldExchanges")]
        public LockedList<Exchange> OldExchanges
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_oldExchanges == null)
                        _oldExchanges = new LockedList<Exchange>();

                    return _oldExchanges;
                }
            }
        }

        [DataMember(Name = "TrustSignatures")]
        public SignatureCollection TrustSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_trustSignatures == null)
                        _trustSignatures = new SignatureCollection();

                    return _trustSignatures;
                }
            }
        }

        [DataMember(Name = "DeleteSignatures")]
        public SignatureCollection DeleteSignatures
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_deleteSignatures == null)
                        _deleteSignatures = new SignatureCollection();

                    return _deleteSignatures;
                }
            }
        }

        [DataMember(Name = "Wikis")]
        public WikiCollection Wikis
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_wikis == null)
                        _wikis = new WikiCollection();

                    return _wikis;
                }
            }
        }

        [DataMember(Name = "Chats")]
        public ChatCollection Chats
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_chats == null)
                        _chats = new ChatCollection();

                    return _chats;
                }
            }
        }

        #region ICloneable<ProfileItem>

        public ProfileItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(ProfileItem));

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
                        return (ProfileItem)ds.ReadObject(xmlDictionaryReader);
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
