using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "ProfileInfo", Namespace = "http://Outopos")]
    class ProfileInfo : IEquatable<ProfileInfo>
    {
        public ProfileInfo(ProfileHeader header, ProfileContent content)
        {
            this.Header = header;
            this.Content = content;
        }

        [DataMember(Name = "Header")]
        public ProfileHeader Header { get; private set; }

        [DataMember(Name = "Content")]
        public ProfileContent Content { get; private set; }

        public static bool operator ==(ProfileInfo x, ProfileInfo y)
        {
            if ((object)x == null)
            {
                if ((object)y == null) return true;

                return y.Equals(x);
            }
            else
            {
                return x.Equals(y);
            }
        }

        public static bool operator !=(ProfileInfo x, ProfileInfo y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return this.Header.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ProfileInfo)) return false;

            return this.Equals((ProfileInfo)obj);
        }

        public bool Equals(ProfileInfo other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;

            if (this.Header != other.Header
                || this.Content != other.Content)
            {
                return false;
            }

            return true;
        }
    }
}
