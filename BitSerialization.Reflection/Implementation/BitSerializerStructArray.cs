using System;

namespace BitSerialization.Reflection.Implementation
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
            return BitSerializerStruct<T>.Deserialize(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerStruct<T>.Serialize(itr, in value);
        }
    }
}
