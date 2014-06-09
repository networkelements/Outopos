using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "ChatMessageInfo", Namespace = "http://Outopos")]
    class ChatMessageInfo
    {
        [DataMember(Name = "Header")]
        public ChatMessageHeader Header { get; set; }

        [DataMember(Name = "Content")]
        public ChatMessageContent Content { get; set; }

        [DataMember(Name = "IsUnread")]
        public bool IsUnread { get; set; }

        [DataMember(Name = "IsLocked")]
        public bool IsLocked { get; set; }
    }
}
