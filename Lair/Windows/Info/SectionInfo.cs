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
    [DataContract(Name = "SectionInfo", Namespace = "http://Lair/Windows")]
    class SectionInfo : IDeepCloneable<SectionInfo>, IThisLock
    {
        private Section _section = null;
        private string _leaderSignature = null;

        private object _thisLock = new object();
        private static object _thisStaticLock = new object();

        public SectionInfo()
        {

        }

        public override int GetHashCode()
        {
            if (_section == null) return 0;
            else return _section.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is SectionInfo)) return false;

            return this.Equals((SectionInfo)obj);
        }

        public bool Equals(SectionInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.Section != other.Section
                || this.LeaderSignature != other.LeaderSignature)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "Section")]
        public Section Section
        {
            get
            {
                return _section;
            }
            set
            {
                _section = value;
            }
        }

        [DataMember(Name = "LeaderSignature")]
        public string LeaderSignature
        {
            get
            {
                return _leaderSignature;
            }
            set
            {
                _leaderSignature = value;
            }
        }

        #region IDeepClone<SectionInfo>

        public SectionInfo DeepClone()
        {
            lock (this.ThisLock)
            {
                var ds = new DataContractSerializer(typeof(SectionInfo));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (XmlDictionaryWriter textDictionaryWriter = XmlDictionaryWriter.CreateTextWriter(ms, new UTF8Encoding(false), false))
                    {
                        ds.WriteObject(textDictionaryWriter, this);
                    }

                    ms.Position = 0;

                    using (XmlDictionaryReader textDictionaryReader = XmlDictionaryReader.CreateTextReader(ms, XmlDictionaryReaderQuotas.Max))
                    {
                        return (SectionInfo)ds.ReadObject(textDictionaryReader);
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
