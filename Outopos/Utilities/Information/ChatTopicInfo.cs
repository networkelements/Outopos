using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "ChatTopicInfo", Namespace = "http://Outopos")]
    class ChatTopicInfo
    {
        [DataMember(Name = "Header")]
        public ChatTopicHeader Header { get; set; }

        [DataMember(Name = "Content")]
        public ChatTopicContent Content { get; set; }

        [DataMember(Name = "IsUnread")]
        public bool IsUnread { get; set; }
    }
}
