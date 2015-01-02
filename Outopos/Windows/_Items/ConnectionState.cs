using System.Runtime.Serialization;

namespace Outopos.Windows
{
    [DataContract(Name = "ConnectionState", Namespace = "http://Outopos/Windows")]
    enum ConnectionState
    {
        [EnumMember(Value = "Red")]
        Red = 0,

        [EnumMember(Value = "Yello")]
        Yello = 1,

        [EnumMember(Value = "Green")]
        Green = 2,
    }
}
