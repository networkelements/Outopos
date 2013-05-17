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
    [DataContract(Name = "LeaderInfo", Namespace = "http://Lair/Windows")]
    class LeaderInfo : IDeepCloneable<LeaderInfo>, IThisLock
    {
        private SignatureCollection _creatorSignatures = null;
        private SignatureCollection _managerSignatures = null;
        private string _comment = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public LeaderInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_comment == null) return 0;
            else return _comment.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is LeaderInfo)) return false;

            return this.Equals((LeaderInfo)obj);
        }

        public bool Equals(LeaderInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Comment != other.Comment)
            {
                return false;
            }

            if (this.CreatorSignatures != null && other.CreatorSignatures != null)
            {
                if (!Collection.Equals(this.CreatorSignatures, other.CreatorSignatures)) return false;
            }

            if (this.ManagerSignatures != null && other.ManagerSignatures != null)
            {
                if (!Collection.Equals(this.ManagerSignatures, other.ManagerSignatures)) return false;
            }

            return true;
        }

        public override string ToString()
        {
            return _comment;
        }

        [DataMember(Name = "CreatorSignatures")]
        public SignatureCollection CreatorSignatures
        {
            get
            {
                if (_creatorSignatures == null)
                    _creatorSignatures = new SignatureCollection(Leader.MaxCreatorSignaturesCount);

                return _creatorSignatures;
            }
        }

        [DataMember(Name = "ManagerSignatures")]
        public SignatureCollection ManagerSignatures
        {
            get
            {
                if (_managerSignatures == null)
                    _managerSignatures = new SignatureCollection(Leader.MaxManagerSignaturesCount);

                return _managerSignatures;
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
                if (value != null && value.Length > Leader.MaxCommentLength)
                {
                    throw new ArgumentException();
                }
                else
                {
                    _comment = value;
                }
            }
        }

        #region IDeepClone<LeaderInfo>

        public LeaderInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(LeaderInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (LeaderInfo)ds.ReadObject(textDictionaryReader);
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
