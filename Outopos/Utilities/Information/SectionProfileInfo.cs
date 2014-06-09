using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Library.Net.Outopos;

namespace Outopos
{
    [DataContract(Name = "SectionProfileInfo", Namespace = "http://Outopos")]
    class SectionProfileInfo
    {
        [DataMember(Name = "Header")]
        public SectionProfileHeader Header { get; set; }

        [DataMember(Name = "Content")]
        public SectionProfileContent Content { get; set; }
    }
}
