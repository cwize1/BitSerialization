//
// Copyright (c) 2020 Chris Gunn
//

using BitSerialization.Common;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BitSerialization.Reflection.PreCalculated.Implementation
{
    internal static class BitSerializerPrimitives
    {
        public struct TypeData
        {
            public DeserializeFieldHandler DeserializeFunc;
            public SerializeFieldHandler SerializeFunc;
            public Type ArraySerializerType;

            public TypeData(
                DeserializeFieldHandler deserializeFunc,
                SerializeFieldHandler serializeFunc,
                Type ArraySerializerType)
            {
                this.DeserializeFunc = deserializeFunc;
                this.SerializeFunc = serializeFunc;
                this.ArraySerializerType = ArraySerializerType;
            }
        }

        public static readonly Dictionary<Type, TypeData> LittleEndianTypes = new Dictionary<Type, TypeData>()
        {
            { typeof(byte), new TypeData(DeserializeUInt8Field<byte>, SerializeUInt8Field, typeof(BitSerializerUInt8Array<byte>)) },
            { typeof(sbyte), new TypeData(DeserializeInt8Field<sbyte>, SerializeInt8Field, typeof(BitSerializerInt8Array<sbyte>)) },
            { typeof(ushort), new TypeData(DeserializeUInt16LittleEndianField<ushort>, SerializeUInt16LittleEndianField, typeof(BitSerializerUInt16LittleEndianArray<ushort>)) },
            { typeof(short), new TypeData(DeserializeInt16LittleEndianField<short>, SerializeInt16LittleEndianField, typeof(BitSerializerInt16LittleEndianArray<short>)) },
            { typeof(uint), new TypeData(DeserializeUInt32LittleEndianField<uint>, SerializeUInt32LittleEndianField, typeof(BitSerializerUInt32LittleEndianArray<uint>)) },
            { typeof(int), new TypeData(DeserializeInt32LittleEndianField<int>, SerializeInt32LittleEndianField, typeof(BitSerializerInt32LittleEndianArray<int>)) },
            { typeof(ulong), new TypeData(DeserializeUInt64LittleEndianField<ulong>, SerializeUInt64LittleEndianField, typeof(BitSerializerUInt64LittleEndianArray<ulong>)) },
            { typeof(long), new TypeData(DeserializeInt64LittleEndianField<long>, SerializeInt64LittleEndianField, typeof(BitSerializerInt64LittleEndianArray<long>)) },
        };

        public static readonly Dictionary<Type, TypeData> BigEndianTypes = new Dictionary<Type, TypeData>()
        {
            { typeof(byte), new TypeData(DeserializeUInt8Field<byte>, SerializeUInt8Field, typeof(BitSerializerUInt8Array<byte>)) },
            { typeof(sbyte), new TypeData(DeserializeInt8Field<sbyte>, SerializeInt8Field, typeof(BitSerializerInt8Array<sbyte>)) },
            { typeof(ushort), new TypeData(DeserializeUInt16BigEndianField<ushort>, SerializeUInt16BigEndianField, typeof(BitSerializerUInt16BigEndianArray<ushort>)) },
            { typeof(short), new TypeData(DeserializeInt16BigEndianField<short>, SerializeInt16BigEndianField, typeof(BitSerializerInt16BigEndianArray<short>)) },
            { typeof(uint), new TypeData(DeserializeUInt32BigEndianField<uint>, SerializeUInt32BigEndianField, typeof(BitSerializerUInt32BigEndianArray<uint>)) },
            { typeof(int), new TypeData(DeserializeInt32BigEndianField<int>, SerializeInt32BigEndianField, typeof(BitSerializerInt32BigEndianArray<int>)) },
            { typeof(ulong), new TypeData(DeserializeUInt64BigEndianField<ulong>, SerializeUInt64BigEndianField, typeof(BitSerializerUInt64BigEndianArray<ulong>)) },
            { typeof(long), new TypeData(DeserializeInt64BigEndianField<long>, SerializeInt64BigEndianField, typeof(BitSerializerInt64BigEndianArray<long>)) },
        };

        public static Dictionary<Type, TypeData> GetTypeData(BitEndianess endianess)
        {
            return endianess == BitEndianess.BigEndian ?
                BigEndianTypes :
                LittleEndianTypes;
        }

        // Used to cast from an enum to its integer value and vice versa.
        public static ToType HardCast<FromType, ToType>(FromType value)
        {
            return Unsafe.As<FromType, ToType>(ref value);
        }

        public static ReadOnlySpan<byte> DeserializeUInt8(ReadOnlySpan<byte> source, out byte value)
        {
            if (source.Length < sizeof(byte))
            {
                throw new ArgumentOutOfRangeException(nameof(source), $"Not enough bytes to read byte value.");
            }

            value = source[0];
            return source.Slice(sizeof(byte));
        }

        public static ReadOnlySpan<byte> DeserializeUInt8Field<T>(ReadOnlySpan<byte> source, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            ReadOnlySpan<byte> itr = DeserializeUInt8(source, out var value);
            fieldInfo.SetValue(obj, HardCast<byte, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt8(ReadOnlySpan<byte> source, out sbyte value)
        {
            if (source.Length < sizeof(sbyte))
            {
                throw new ArgumentOutOfRangeException(nameof(source), $"Not enough bytes to read sbyte value.");
            }

            value = (sbyte)source[0];
            return source.Slice(sizeof(sbyte));
        }

        public static ReadOnlySpan<byte> DeserializeInt8Field<T>(ReadOnlySpan<byte> source, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            ReadOnlySpan<byte> itr = DeserializeInt8(source, out var value);
            fieldInfo.SetValue(obj, HardCast<sbyte, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt16LittleEndian(ReadOnlySpan<byte> itr, out ushort value)
        {
            value = BinaryPrimitives.ReadUInt16LittleEndian(itr);
            return itr.Slice(sizeof(ushort));
        }

        public static ReadOnlySpan<byte> DeserializeUInt16LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt16LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<ushort, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt16BigEndian(ReadOnlySpan<byte> itr, out ushort value)
        {
            value = BinaryPrimitives.ReadUInt16BigEndian(itr);
            return itr.Slice(sizeof(ushort));
        }

        public static ReadOnlySpan<byte> DeserializeUInt16BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt16BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<ushort, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt16LittleEndian(ReadOnlySpan<byte> itr, out short value)
        {
            value = BinaryPrimitives.ReadInt16LittleEndian(itr);
            return itr.Slice(sizeof(short));
        }

        public static ReadOnlySpan<byte> DeserializeInt16LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt16LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<short, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt16BigEndian(ReadOnlySpan<byte> itr, out short value)
        {
            value = BinaryPrimitives.ReadInt16BigEndian(itr);
            return itr.Slice(sizeof(short));
        }

        public static ReadOnlySpan<byte> DeserializeInt16BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt16BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<short, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt32LittleEndian(ReadOnlySpan<byte> itr, out uint value)
        {
            value = BinaryPrimitives.ReadUInt32LittleEndian(itr);
            return itr.Slice(sizeof(uint));
        }

        public static ReadOnlySpan<byte> DeserializeUInt32LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt32LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<uint, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt32BigEndian(ReadOnlySpan<byte> itr, out uint value)
        {
            value = BinaryPrimitives.ReadUInt32BigEndian(itr);
            return itr.Slice(sizeof(uint));
        }

        public static ReadOnlySpan<byte> DeserializeUInt32BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt32BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<uint, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt32LittleEndian(ReadOnlySpan<byte> itr, out int value)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(itr);
            return itr.Slice(sizeof(int));
        }

        public static ReadOnlySpan<byte> DeserializeInt32LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt32LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<int, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt32BigEndian(ReadOnlySpan<byte> itr, out int value)
        {
            value = BinaryPrimitives.ReadInt32BigEndian(itr);
            return itr.Slice(sizeof(int));
        }

        public static ReadOnlySpan<byte> DeserializeInt32BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt32BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<int, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt64LittleEndian(ReadOnlySpan<byte> itr, out ulong value)
        {
            value = BinaryPrimitives.ReadUInt64LittleEndian(itr);
            return itr.Slice(sizeof(ulong));
        }

        public static ReadOnlySpan<byte> DeserializeUInt64LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt64LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<ulong, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeUInt64BigEndian(ReadOnlySpan<byte> itr, out ulong value)
        {
            value = BinaryPrimitives.ReadUInt64BigEndian(itr);
            return itr.Slice(sizeof(ulong));
        }

        public static ReadOnlySpan<byte> DeserializeUInt64BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeUInt64BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<ulong, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt64LittleEndian(ReadOnlySpan<byte> itr, out long value)
        {
            value = BinaryPrimitives.ReadInt64LittleEndian(itr);
            return itr.Slice(sizeof(long));
        }

        public static ReadOnlySpan<byte> DeserializeInt64LittleEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt64LittleEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<long, T>(value));
            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeInt64BigEndian(ReadOnlySpan<byte> itr, out long value)
        {
            value = BinaryPrimitives.ReadInt64BigEndian(itr);
            return itr.Slice(sizeof(long));
        }

        public static ReadOnlySpan<byte> DeserializeInt64BigEndianField<T>(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
            where T : struct
        {
            itr = DeserializeInt64BigEndian(itr, out var value);
            fieldInfo.SetValue(obj, HardCast<long, T>(value));
            return itr;
        }

        public static Span<byte> SerializeUInt8(Span<byte> itr, byte value)
        {
            if (itr.Length < sizeof(byte))
            {
                throw new ArgumentOutOfRangeException(nameof(itr), $"Not enough bytes to write byte value.");
            }

            itr[0] = value;
            return itr.Slice(sizeof(byte));
        }

        public static Span<byte> SerializeUInt8Field(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt8(itr, (byte)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt8(Span<byte> itr, sbyte value)
        {
            if (itr.Length < sizeof(sbyte))
            {
                throw new ArgumentOutOfRangeException(nameof(itr), $"Not enough bytes to write sbyte value.");
            }

            itr[0] = (byte)value;
            return itr.Slice(sizeof(sbyte));
        }

        public static Span<byte> SerializeInt8Field(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt8(itr, (sbyte)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt16LittleEndian(Span<byte> itr, ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(itr, value);
            return itr.Slice(sizeof(ushort));
        }

        public static Span<byte> SerializeUInt16LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt16LittleEndian(itr, (ushort)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt16BigEndian(Span<byte> itr, ushort value)
        {
            BinaryPrimitives.WriteUInt16BigEndian(itr, value);
            return itr.Slice(sizeof(ushort));
        }

        public static Span<byte> SerializeUInt16BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt16BigEndian(itr, (ushort)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt16LittleEndian(Span<byte> itr, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(itr, value);
            return itr.Slice(sizeof(short));
        }

        public static Span<byte> SerializeInt16LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt16LittleEndian(itr, (short)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt16BigEndian(Span<byte> itr, short value)
        {
            BinaryPrimitives.WriteInt16BigEndian(itr, value);
            return itr.Slice(sizeof(short));
        }

        public static Span<byte> SerializeInt16BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt16BigEndian(itr, (short)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt32LittleEndian(Span<byte> itr, uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(itr, value);
            return itr.Slice(sizeof(uint));
        }

        public static Span<byte> SerializeUInt32LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt32LittleEndian(itr, (uint)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt32BigEndian(Span<byte> itr, uint value)
        {
            BinaryPrimitives.WriteUInt32BigEndian(itr, value);
            return itr.Slice(sizeof(uint));
        }

        public static Span<byte> SerializeUInt32BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt32BigEndian(itr, (uint)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt32LittleEndian(Span<byte> itr, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(itr, value);
            return itr.Slice(sizeof(int));
        }

        public static Span<byte> SerializeInt32LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt32LittleEndian(itr, (int)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt32BigEndian(Span<byte> itr, int value)
        {
            BinaryPrimitives.WriteInt32BigEndian(itr, value);
            return itr.Slice(sizeof(int));
        }

        public static Span<byte> SerializeInt32BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt32BigEndian(itr, (int)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt64LittleEndian(Span<byte> itr, ulong value)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(itr, value);
            return itr.Slice(sizeof(ulong));
        }

        public static Span<byte> SerializeUInt64LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt64LittleEndian(itr, (ulong)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeUInt64BigEndian(Span<byte> itr, ulong value)
        {
            BinaryPrimitives.WriteUInt64BigEndian(itr, value);
            return itr.Slice(sizeof(ulong));
        }

        public static Span<byte> SerializeUInt64BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeUInt64BigEndian(itr, (ulong)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt64LittleEndian(Span<byte> itr, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(itr, value);
            return itr.Slice(sizeof(long));
        }

        public static Span<byte> SerializeInt64LittleEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt64LittleEndian(itr, (long)fieldInfo.GetValue(obj)!);
        }

        public static Span<byte> SerializeInt64BigEndian(Span<byte> itr, long value)
        {
            BinaryPrimitives.WriteInt64BigEndian(itr, value);
            return itr.Slice(sizeof(long));
        }

        public static Span<byte> SerializeInt64BigEndianField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            return SerializeInt64BigEndian(itr, (long)fieldInfo.GetValue(obj)!);
        }
    }
}
