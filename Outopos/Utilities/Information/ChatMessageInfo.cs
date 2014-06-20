using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [Flags]
    [DataContract(Name = "ChatMessageState", Namespace = "http://Outopos")]
    enum ChatMessageState
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "IsUnread")]
        IsUnread = 0x01,

        [EnumMember(Value = "IsLocked")]
        IsLocked = 0x02,
    }

    [DataContract(Name = "ChatMessageInfo", Namespace = "http://Outopos")]
    class ChatMessageInfo : IEquatable<ChatMessageInfo>
    {
        public ChatMessageInfo(ChatMessageHeader header, ChatMessageContent content)
        {
            this.Header = header;
            this.Content = content;
        }

        [DataMember(Name = "Header")]
        public ChatMessageHeader Header { get; private set; }

        [DataMember(Name = "Content")]
        public ChatMessageContent Content { get; private set; }

        public static bool operator ==(ChatMessageInfo x, ChatMessageInfo y)
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

        public static bool operator !=(ChatMessageInfo x, ChatMessageInfo y)
        {
            return !(x == y);
        }

        public override int GetHashCode()
        {
            return this.Header.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is ChatMessageInfo)) return false;

            return this.Equals((ChatMessageInfo)obj);
        }

        public bool Equals(ChatMessageInfo other)
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
