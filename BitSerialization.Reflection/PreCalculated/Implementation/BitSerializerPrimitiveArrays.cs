//
// Copyright (c) 2020 Chris Gunn
//

using BitSerialization.Common;
using System;

namespace BitSerialization.Reflection.Precalculated.Implementation
{
    internal sealed class BitSerializerUInt8Array<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt8Array(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt8(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<byte, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt8(itr, BitSerializerPrimitives.HardCast<T, byte>(value));
        }
    }

    internal sealed class BitSerializerInt8Array<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt8Array(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt8(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<sbyte, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt8(itr, BitSerializerPrimitives.HardCast<T, sbyte>(value));
        }
    }

    internal sealed class BitSerializerUInt16LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt16LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt16LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<ushort, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt16LittleEndian(itr, BitSerializerPrimitives.HardCast<T, ushort>(value));
        }
    }

    internal sealed class BitSerializerUInt16BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt16BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt16BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<ushort, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt16BigEndian(itr, BitSerializerPrimitives.HardCast<T, ushort>(value));
        }
    }

    internal sealed class BitSerializerInt16LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt16LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt16LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<short, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt16LittleEndian(itr, BitSerializerPrimitives.HardCast<T, short>(value));
        }
    }

    internal sealed class BitSerializerInt16BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt16BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt16BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<short, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt16BigEndian(itr, BitSerializerPrimitives.HardCast<T, short>(value));
        }
    }

    internal sealed class BitSerializerUInt32LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt32LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt32LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<uint, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt32LittleEndian(itr, BitSerializerPrimitives.HardCast<T, uint>(value));
        }
    }

    internal sealed class BitSerializerUInt32BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt32BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt32BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<uint, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt32BigEndian(itr, BitSerializerPrimitives.HardCast<T, uint>(value));
        }
    }

    internal sealed class BitSerializerInt32LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt32LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt32LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<int, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt32LittleEndian(itr, BitSerializerPrimitives.HardCast<T, int>(value));
        }
    }

    internal sealed class BitSerializerInt32BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt32BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt32BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<int, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt32BigEndian(itr, BitSerializerPrimitives.HardCast<T, int>(value));
        }
    }

    internal sealed class BitSerializerUInt64LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt64LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt64LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<ulong, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt64LittleEndian(itr, BitSerializerPrimitives.HardCast<T, ulong>(value));
        }
    }

    internal sealed class BitSerializerUInt64BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerUInt64BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeUInt64BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<ulong, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeUInt64BigEndian(itr, BitSerializerPrimitives.HardCast<T, ulong>(value));
        }
    }

    internal sealed class BitSerializerInt64LittleEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt64LittleEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt64LittleEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<long, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt64LittleEndian(itr, BitSerializerPrimitives.HardCast<T, long>(value));
        }
    }

    internal sealed class BitSerializerInt64BigEndianArray<T> :
        BitSerializerArray<T>
        where T : struct
    {
        public BitSerializerInt64BigEndianArray(BitArrayAttribute settings) :
            base(settings)
        {
        }

        protected override ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value)
        {
            var result = BitSerializerPrimitives.DeserializeInt64BigEndian(itr, out var tmp);
            value = BitSerializerPrimitives.HardCast<long, T>(tmp);
            return result;
        }

        protected override Span<byte> SerializeItem(Span<byte> itr, in T value)
        {
            return BitSerializerPrimitives.SerializeInt64BigEndian(itr, BitSerializerPrimitives.HardCast<T, long>(value));
        }
    }
}
