//
// Copyright (c) 2020 Chris Gunn
//

using System;

namespace BitSerialization.Common
{
    // Specifies how the size of an array is handled when serializing and deserializing.
    public enum BitArraySizeType
    {
        // The size of the array is a constant value. When serializing, if the provided array is
        // too small (or null) then the array is backfilled with the default value.
        Const,

        // The array sits at the end of the packet and its size is determined by the
        // packet size.
        EndFill,
    }

    // Specifies how an array is serialized and deserialized.
    [AttributeUsage(AttributeTargets.Field)]
    public class BitArrayAttribute :
        Attribute
    {
        public BitArrayAttribute(BitArraySizeType sizeType)
        {
            this.SizeType = sizeType;
        }

        public BitArraySizeType SizeType { private set; get; }

        // If SizeType is BitArraySizeType.Const then this property specifies the length of the array.
        public int ConstSize { get; set; }
    }
}
