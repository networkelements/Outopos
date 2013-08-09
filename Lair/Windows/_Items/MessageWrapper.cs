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
    [Flags]
    [DataContract(Name = "MessageState", Namespace = "http://Lair/Windows")]
    enum MessageState
    {
        [EnumMember(Value = "New")]
        New = 0x1
    }

    class MessageWrapper : IEquatable<MessageWrapper>
    {
        private MessageState _state;
        private MessageItem _messageItem;

        public override int GetHashCode()
        {
            if (_messageItem == null) return 0;
            else return _messageItem.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if ((object)obj == null || !(obj is MessageWrapper)) return false;

            return this.Equals((MessageWrapper)obj);
        }

        public bool Equals(MessageWrapper other)
        {
            if ((object)other == null) return false;
            if (object.ReferenceEquals(this, other)) return true;
            if (this.GetHashCode() != other.GetHashCode()) return false;

            if (this.State != other.State
                || this.MessageItem != other.MessageItem)
            {
                return false;
            }

            return true;
        }

        [DataMember(Name = "State")]
        public MessageState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
            }
        }

        [DataMember(Name = "MessageItem")]
        public MessageItem MessageItem
        {
            get
            {
                return _messageItem;
            }
            set
            {
                _messageItem = value;
            }
        }
    }
}
