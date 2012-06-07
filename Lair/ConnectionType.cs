using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Library;

namespace Lair
{
    [DataContract(Name = "ConnectionType", Namespace = "http://Lair")]
    public enum ConnectionType
    {
        [EnumMember(Value = "Tcp")]
        Tcp = 0,

        [EnumMember(Value = "Socks4Proxy")]
        Socks4Proxy = 1,

        [EnumMember(Value = "Socks4aProxy")]
        Socks4aProxy = 2,

        [EnumMember(Value = "Socks5Proxy")]
        Socks5Proxy = 3,

        [EnumMember(Value = "HttpProxy")]
        HttpProxy = 4,
    }
}
