using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "ChatTopicInfo", Namespace = "http://Outopos")]
    class ChatTopicInfo : IEquatable<ChatTopicInfo>
    {
        public ChatTopicInfo(ChatTopicHeader header, ChatTopicContent content)
        {
            this.Header = header;
            this.Content = content;
        }

        [DataMember(Name = "Header")]
        public ChatTopicHeader Header { get; private set; }

        [DataMember(Name = "Content")]
        public ChatTopicContent Content { get; private set; }

        public static bool operator ==(ChatTopicInfo x, ChatTopicInfo y)
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

        public static bool operator !=(ChatTopicInfo x, ChatTopicInfo y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return this.Header.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ChatTopicInfo)) return false;

            return this.Equals((ChatTopicInfo)obj);
        }

        public bool Equals(ChatTopicInfo other)
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
