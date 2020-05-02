using System;

namespace BitSerialization.Reflection
{
    public enum BitArraySizeType
    {
        // The size of the array is a constant value.
        Const,
        // The array sits at the end of the packet and its size is determined by the
        // packet size.
        EndFill,
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BitArrayAttribute :
        Attribute
    {
        public BitArrayAttribute(BitArraySizeType sizeType)
        {
            this.SizeType = sizeType;
        }

        public BitArraySizeType SizeType { private set; get; }

        public int ConstSize { get; set; }
    }
}
