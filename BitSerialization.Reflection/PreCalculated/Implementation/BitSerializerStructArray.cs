//
// Copyright (c) 2020 Chris Gunn
//

using BitSerialization.Common;
using System;

namespace BitSerialization.Reflection.Precalculated.Implementation
{
    internal sealed class BitSerializerStructArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerStructArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            return BitSerializer<T>.Deserialize(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializer<T>.Serialize(itr, in value);
        }
    }
}
