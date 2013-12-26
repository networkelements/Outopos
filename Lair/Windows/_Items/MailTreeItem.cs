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
using Library.Io;

namespace Lair.Windows
{
    [DataContract(Name = "MailTreeItem", Namespace = "http://Lair/Windows")]
    class MailTreeItem : ICloneable<MailTreeItem>, IThisLock
    {
        private string _targetSignature;
        private LockedList<SectionMessage> _sentSectionMessages;
        private LockedList<SectionMessage> _unreadSectionMessages;
        private LockedList<SectionMessage> _readSectionMessages;

        private volatile object _thisLock;
        private static readonly object _initializeLock = new object();

        public MailTreeItem()
        {

        }

        [DataMember(Name = "TargetSignature")]
        public string TargetSignature
        {
            get
            {
                lock (this.ThisLock)
                {
                    return _targetSignature;
                }
            }
            set
            {
                lock (this.ThisLock)
                {
                    _targetSignature = value;
                }
            }
        }

        [DataMember(Name = "SentSectionMessages")]
        public LockedList<SectionMessage> SentSectionMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_sentSectionMessages == null)
                        _sentSectionMessages = new LockedList<SectionMessage>();

                    return _sentSectionMessages;
                }
            }
        }

        [DataMember(Name = "UnreadSectionMessages")]
        public LockedList<SectionMessage> UnreadSectionMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_unreadSectionMessages == null)
                        _unreadSectionMessages = new LockedList<SectionMessage>();

                    return _unreadSectionMessages;
                }
            }
        }

        [DataMember(Name = "ReadSectionMessages")]
        public LockedList<SectionMessage> ReadSectionMessages
        {
            get
            {
                lock (this.ThisLock)
                {
                    if (_readSectionMessages == null)
                        _readSectionMessages = new LockedList<SectionMessage>();

                    return _readSectionMessages;
                }
            }
        }

        #region ICloneable<MailTreeItem>

        public MailTreeItem Clone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(MailTreeItem));

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
                        return (MailTreeItem)ds.ReadObject(textDictionaryReader);
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
