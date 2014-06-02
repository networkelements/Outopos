using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Outopos
{
    [DataContract(Name = "HypertextFormatType", Namespace = "http://Library/Net/Outopos")]
    public enum HypertextFormatType : byte
    {
        [EnumMember(Value = "MiniWiki")]
        MiniWiki = 0,
    }

    public interface IHypertext
    {
        HypertextFormatType FormatType { get; }
        string Hypertext { get; }
    }
}
