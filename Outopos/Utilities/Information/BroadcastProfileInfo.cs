using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "BroadcastProfileInfo", Namespace = "http://Outopos")]
    class BroadcastProfileInfo
    {
        [DataMember(Name = "Header")]
        public BroadcastProfileHeader Header { get; set; }

        [DataMember(Name = "Content")]
        public BroadcastProfileContent Content { get; set; }
    }
}
