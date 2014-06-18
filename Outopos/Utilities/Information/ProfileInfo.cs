using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "ProfileInfo", Namespace = "http://Outopos")]
    class ProfileInfo
    {
        [DataMember(Name = "Header")]
        public ProfileHeader Header { get; set; }

        [DataMember(Name = "Content")]
        public ProfileContent Content { get; set; }
    }
}
