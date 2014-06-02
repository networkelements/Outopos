using System.Runtime.Serialization;

namespace Outopos.Windows
{
    [DataContract(Name = "UpdateOption", Namespace = "http://Outopos/Windows")]
    enum UpdateOption
    {
        [EnumMember(Value = "None")]
        None = 0,

        [EnumMember(Value = "AutoCheck")]
        AutoCheck = 1,

        [EnumMember(Value = "AutoUpdate")]
        AutoUpdate = 2,
    }
}
