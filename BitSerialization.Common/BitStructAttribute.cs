using System;

namespace BitSerialization.Common
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class BitStructAttribute :
        Attribute
    {
        public static readonly BitStructAttribute Default = new BitStructAttribute();

        public BitEndianess Endianess { get; set; } = BitEndianess.LittleEndian;
    }
}
