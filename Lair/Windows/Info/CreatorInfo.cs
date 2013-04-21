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
    [DataContract(Name = "CreatorInfo", Namespace = "http://Lair/Windows")]
    class CreatorInfo : IDeepCloneable<CreatorInfo>, IThisLock
    {
        private ChannelCollection _channels = null;
        private string _comment = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public CreatorInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_comment == null) return 0;
            else return _comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is CreatorInfo)) return false;

            return this.Equals((CreatorInfo)obj);
        }

        public bool Equals(CreatorInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            if (this.Channels != null && other.Channels != null)
            {
                if (this.Channels.Count != other.Channels.Count) return false;

                for (int i = 0; i < this.Channels.Count; i++) if (this.Channels[i] != other.Channels[i]) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return _comment;
        }

        [DataMember(Name = "Channels")]
        public ChannelCollection Channels
        {
            get
            {
                if (_channels == null)
                    _channels = new ChannelCollection(Creator.MaxChannelsCount);

                return _channels;
            }
        }

        [DataMember(Name = "Comment")]
        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                if (value != null && value.Length > Creator.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        #region IDeepClone<CreatorInfo>

        public CreatorInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(CreatorInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (CreatorInfo)ds.ReadObject(textDictionaryReader);
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
