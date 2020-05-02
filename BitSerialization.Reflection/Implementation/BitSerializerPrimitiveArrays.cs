using System;

namespace BitSerialization.Reflection.Implementation
{
    internal sealed class BitSerializerUInt8Array :
        BitSerializerArray<byte>
    {
        public BitSerializerUInt8Array(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out byte value)
        {
            return BitSerializerPrimitives.DeserializeUInt8(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in byte value)
        {
            return BitSerializerPrimitives.SerializeUInt8(itr, value);
        }
    }

    internal sealed class BitSerializerInt8Array :
        BitSerializerArray<sbyte>
    {
        public BitSerializerInt8Array(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out sbyte value)
        {
            return BitSerializerPrimitives.DeserializeInt8(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in sbyte value)
        {
            return BitSerializerPrimitives.SerializeInt8(itr, value);
        }
    }

    internal sealed class BitSerializerUInt16LittleEndianArray :
        BitSerializerArray<ushort>
    {
        public BitSerializerUInt16LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out ushort value)
        {
            return BitSerializerPrimitives.DeserializeUInt16LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in ushort value)
        {
            return BitSerializerPrimitives.SerializeUInt16LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerUInt16BigEndianArray :
        BitSerializerArray<ushort>
    {
        public BitSerializerUInt16BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out ushort value)
        {
            return BitSerializerPrimitives.DeserializeUInt16BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in ushort value)
        {
            return BitSerializerPrimitives.SerializeUInt16BigEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt16LittleEndianArray :
        BitSerializerArray<short>
    {
        public BitSerializerInt16LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out short value)
        {
            return BitSerializerPrimitives.DeserializeInt16LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in short value)
        {
            return BitSerializerPrimitives.SerializeInt16LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt16BigEndianArray :
        BitSerializerArray<short>
    {
        public BitSerializerInt16BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out short value)
        {
            return BitSerializerPrimitives.DeserializeInt16BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in short value)
        {
            return BitSerializerPrimitives.SerializeInt16BigEndian(itr, value);
        }
    }

    internal sealed class BitSerializerUInt32LittleEndianArray :
        BitSerializerArray<uint>
    {
        public BitSerializerUInt32LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out uint value)
        {
            return BitSerializerPrimitives.DeserializeUInt32LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in uint value)
        {
            return BitSerializerPrimitives.SerializeUInt32LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerUInt32BigEndianArray :
        BitSerializerArray<uint>
    {
        public BitSerializerUInt32BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out uint value)
        {
            return BitSerializerPrimitives.DeserializeUInt32BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in uint value)
        {
            return BitSerializerPrimitives.SerializeUInt32BigEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt32LittleEndianArray :
        BitSerializerArray<int>
    {
        public BitSerializerInt32LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out int value)
        {
            return BitSerializerPrimitives.DeserializeInt32LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in int value)
        {
            return BitSerializerPrimitives.SerializeInt32LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt32BigEndianArray :
        BitSerializerArray<int>
    {
        public BitSerializerInt32BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out int value)
        {
            return BitSerializerPrimitives.DeserializeInt32BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in int value)
        {
            return BitSerializerPrimitives.SerializeInt32BigEndian(itr, value);
        }
    }

    internal sealed class BitSerializerUInt64LittleEndianArray :
        BitSerializerArray<ulong>
    {
        public BitSerializerUInt64LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out ulong value)
        {
            return BitSerializerPrimitives.DeserializeUInt64LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in ulong value)
        {
            return BitSerializerPrimitives.SerializeUInt64LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerUInt64BigEndianArray :
        BitSerializerArray<ulong>
    {
        public BitSerializerUInt64BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out ulong value)
        {
            return BitSerializerPrimitives.DeserializeUInt64BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in ulong value)
        {
            return BitSerializerPrimitives.SerializeUInt64BigEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt64LittleEndianArray :
        BitSerializerArray<long>
    {
        public BitSerializerInt64LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out long value)
        {
            return BitSerializerPrimitives.DeserializeInt64LittleEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in long value)
        {
            return BitSerializerPrimitives.SerializeInt64LittleEndian(itr, value);
        }
    }

    internal sealed class BitSerializerInt64BigEndianArray :
        BitSerializerArray<long>
    {
        public BitSerializerInt64BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out long value)
        {
            return BitSerializerPrimitives.DeserializeInt64BigEndian(itr, out value);
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in long value)
        {
            return BitSerializerPrimitives.SerializeInt64BigEndian(itr, value);
        }
    }
}
