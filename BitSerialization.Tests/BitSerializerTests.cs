//
// Copyright (c) 2020 Chris Gunn
//

using BitSerialization.Common;
using BitSerialization.Reflection.OnTheFly;
using BitSerialization.Reflection.PreCalculated;
using KellermanSoftware.CompareNetObjects;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace BitSerialization.Tests
{
    public partial class BitSerializerTests
    {
        delegate int CalculateSizeFunc<T>(T value);
        delegate Span<byte> SerializeFunc<T>(Span<byte> output, T value);
        delegate ReadOnlySpan<byte> DeserializeFunc<T>(ReadOnlySpan<byte> input, out T value);

        internal enum EnumOfInt : int
        {
            A = -1,
            B = 0,
            C = 1,
        }

        internal enum EnumOfByte : byte
        {
            A,
            B,
            C,
        }

        private void CheckSerializers<T>(
            T value,
            int expectedSerializeSize,
            int? reportedSerializeSize,
            CalculateSizeFunc<T> calculateSizeFunc,
            SerializeFunc<T> serializeFunc,
            DeserializeFunc<T> deserializeFunc
            ) where
                T : struct
        {
            CheckSerializers(value, value, expectedSerializeSize, reportedSerializeSize, calculateSizeFunc, serializeFunc, deserializeFunc);
        }

        private void CheckSerializers<T>(
            T value,
            T expectedDeserializeValue,
            int expectedSerializeSize,
            int? reportedSerializeSize,
            CalculateSizeFunc<T> calculateSizeFunc,
            SerializeFunc<T> serializeFunc,
            DeserializeFunc<T> deserializeFunc
            ) where
                T : struct
        {
            if (reportedSerializeSize != null)
            {
                Assert.Equal(expectedSerializeSize, reportedSerializeSize);
            }

            Assert.Equal(expectedSerializeSize, calculateSizeFunc(value));

            byte[] output1 = new byte[expectedSerializeSize];
            BitSerializer.Serialize(value, output1);

            byte[] output2 = new byte[expectedSerializeSize];
            BitSerializer<T>.Serialize(output2, value);

            byte[] output3 = new byte[expectedSerializeSize];
            serializeFunc(output3, value);

            Assert.Equal(output1, output2);
            Assert.Equal(output1, output3);

            T value1;
            BitSerializer.Deserialize(output1, out value1);

            T value2;
            BitSerializer<T>.Deserialize(output2, out value2);

            T value3;
            deserializeFunc(output3, out value3);

            expectedDeserializeValue.ShouldCompare(value1);
            expectedDeserializeValue.ShouldCompare(value2);
            expectedDeserializeValue.ShouldCompare(value3);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct BasicStruct
        {
            public const int Size = 30;

            public byte A;
            public sbyte B;
            public ushort C;
            public short D;
            public uint E;
            public int F;
            public ulong G;
            public long H;
        }

        [Fact]
        public void Basic()
        {
            BasicStruct value = new BasicStruct()
            {
                A = 1,
                B = -2,
                C = 3,
                D = -4,
                E= 5,
                F = -6,
                G = 7,
                H = -8,
            };

            CheckSerializers(
                value,
                BasicStruct.Size,
                BasicStructSerializer.Size,
                BasicStructSerializer.CalculateSize,
                BasicStructSerializer.Serialize,
                BasicStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct BasicWithEnumsStruct
        {
            public const int Size = 5;

            public EnumOfByte A;
            public EnumOfInt B;
        }

        [Fact]
        public void BasicWithEnums()
        {
            BasicWithEnumsStruct value = new BasicWithEnumsStruct()
            {
                A = EnumOfByte.A,
                B = EnumOfInt.C,
            };

            CheckSerializers(
                value,
                BasicWithEnumsStruct.Size,
                BasicWithEnumsStructSerializer.Size,
                BasicWithEnumsStructSerializer.CalculateSize,
                BasicWithEnumsStructSerializer.Serialize,
                BasicWithEnumsStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct PrimitiveConstArraysStruct
        {
            public const int Size = 60;

            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public byte[]? A;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public sbyte[]? B;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public ushort[]? C;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public short[]? D;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public uint[]? E;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public int[]? F;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public ulong[]? G;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public long[]? H;
        }

        [Fact]
        public void PrimitiveConstArrays()
        {
            PrimitiveConstArraysStruct value = new PrimitiveConstArraysStruct()
            {
                A = new byte[] { 1, 2 },
                B = new sbyte[] { -3, -4 },
                C = new ushort[] { 5, 6 },
                D = new short[] { -7, -8 },
                E = new uint[] { 9, 10 },
                F = new int[] { -11, -12 },
                G = new ulong[] { 13, 14 },
                H = new long[] { -15, -16 },
            };

            CheckSerializers(
                value,
                PrimitiveConstArraysStruct.Size,
                PrimitiveConstArraysStructSerializer.Size,
                PrimitiveConstArraysStructSerializer.CalculateSize,
                PrimitiveConstArraysStructSerializer.Serialize,
                PrimitiveConstArraysStructSerializer.Deserialize);
        }

        [Fact]
        public void PrimitiveConstArraysWithBackfill()
        {
            PrimitiveConstArraysStruct value = new PrimitiveConstArraysStruct()
            {
                A = new byte[] { 1 },
                B = new sbyte[] { -2 },
                C = new ushort[] { },
                D = new short[] { },
                E = new uint[] { 2 },
                F = new int[] { -3 },
                G = null,
                H = null,
            };

            PrimitiveConstArraysStruct expectedDeserializeValue = new PrimitiveConstArraysStruct()
            {
                A = new byte[] { 1, 0 },
                B = new sbyte[] { -2, 0 },
                C = new ushort[] { 0, 0 },
                D = new short[] { 0, 0 },
                E = new uint[] { 2, 0 },
                F = new int[] { -3, 0 },
                G = new ulong[] { 0, 0 },
                H = new long[] { 0, 0 },
            };

            CheckSerializers(
                value,
                expectedDeserializeValue,
                PrimitiveConstArraysStruct.Size,
                PrimitiveConstArraysStructSerializer.Size,
                PrimitiveConstArraysStructSerializer.CalculateSize,
                PrimitiveConstArraysStructSerializer.Serialize,
                PrimitiveConstArraysStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct WithEnumsConstArraysStruct
        {
            public const int Size = 16;

            [BitArray(BitArraySizeType.Const, ConstSize = 4)]
            public EnumOfByte[]? A;
            [BitArray(BitArraySizeType.Const, ConstSize = 3)]
            public EnumOfInt[]? B;
        }

        [Fact]
        public void WithEnumsConstArrays()
        {
            WithEnumsConstArraysStruct value = new WithEnumsConstArraysStruct()
            {
                A = new EnumOfByte[] { EnumOfByte.A, EnumOfByte.B, EnumOfByte.C, EnumOfByte.A },
                B = new EnumOfInt[] { EnumOfInt.A, EnumOfInt.B, EnumOfInt.C },
            };

            CheckSerializers(
                value,
                WithEnumsConstArraysStruct.Size,
                WithEnumsConstArraysStructSerializer.Size,
                WithEnumsConstArraysStructSerializer.CalculateSize,
                WithEnumsConstArraysStructSerializer.Serialize,
                WithEnumsConstArraysStructSerializer.Deserialize);
        }

        [Fact]
        public void WithEnumsConstArraysWithBackfill()
        {
            WithEnumsConstArraysStruct value = new WithEnumsConstArraysStruct()
            {
                A = new EnumOfByte[] { EnumOfByte.A, EnumOfByte.B },
                B = new EnumOfInt[] { EnumOfInt.A, EnumOfInt.B },
            };

            WithEnumsConstArraysStruct expectedDeserializeValue = new WithEnumsConstArraysStruct()
            {
                A = new EnumOfByte[] { EnumOfByte.A, EnumOfByte.B, 0, 0 },
                B = new EnumOfInt[] { EnumOfInt.A, EnumOfInt.B, 0 },
            };

            CheckSerializers(
                value,
                expectedDeserializeValue,
                WithEnumsConstArraysStruct.Size,
                WithEnumsConstArraysStructSerializer.Size,
                WithEnumsConstArraysStructSerializer.CalculateSize,
                WithEnumsConstArraysStructSerializer.Serialize,
                WithEnumsConstArraysStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct PrimitivesEndFillArrayStruct
        {
            [BitArray(BitArraySizeType.EndFill)]
            public long[]? A;
        }

        [Fact]
        public void PrimitivesEndFillArrayEmptyValues()
        {
            PrimitivesEndFillArrayStruct valueNull = new PrimitivesEndFillArrayStruct()
            {
                A = null,
            };

            PrimitivesEndFillArrayStruct valueEmpty = new PrimitivesEndFillArrayStruct()
            {
                A = new long[] { },
            };

            CheckSerializers(
                valueNull,
                valueEmpty,
                0,
                null,
                PrimitivesEndFillArrayStructSerializer.CalculateSize,
                PrimitivesEndFillArrayStructSerializer.Serialize,
                PrimitivesEndFillArrayStructSerializer.Deserialize);

            CheckSerializers(
                valueEmpty,
                valueEmpty,
                0,
                null,
                PrimitivesEndFillArrayStructSerializer.CalculateSize,
                PrimitivesEndFillArrayStructSerializer.Serialize,
                PrimitivesEndFillArrayStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct EnumEndFillArrayStruct
        {
            [BitArray(BitArraySizeType.EndFill)]
            public EnumOfByte[]? A;
        }

        [Fact]
        public void EnumEndFillArray()
        {
            EnumEndFillArrayStruct value = new EnumEndFillArrayStruct()
            {
                A = new EnumOfByte[] { EnumOfByte.A, EnumOfByte.B, EnumOfByte.C },
            };

            CheckSerializers(
                value,
                3,
                null,
                EnumEndFillArrayStructSerializer.CalculateSize,
                EnumEndFillArrayStructSerializer.Serialize,
                EnumEndFillArrayStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct WithStructVariableStruct
        {
            public const int Size = BasicStruct.Size;

            public BasicStruct A;
        }

        [Fact]
        public void WithStructVariable()
        {
            WithStructVariableStruct value = new WithStructVariableStruct()
            {
                A = new BasicStruct()
                {
                    A = 1,
                    B = 2,
                },
            };

            CheckSerializers(
                value,
                WithStructVariableStruct.Size,
                WithStructVariableStructSerializer.Size,
                WithStructVariableStructSerializer.CalculateSize,
                WithStructVariableStructSerializer.Serialize,
                WithStructVariableStructSerializer.Deserialize);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct ArrayOfStructStruct
        {
            public const int Size = BasicStruct.Size * 3;

            [BitArray(BitArraySizeType.Const, ConstSize = 3)]
            public BasicStruct[]? A;
        }

        [Fact]
        public void ArrayOfStruct()
        {
            ArrayOfStructStruct value = new ArrayOfStructStruct()
            {
                A = new BasicStruct[]
                {
                    new BasicStruct()
                    {
                        A = 1,
                        B = 2,
                    },
                        new BasicStruct()
                    {
                        C = 3,
                        D = 4,
                    },
                    new BasicStruct()
                    {
                        E = 5,
                        F = 6,
                    }
                },
            };

            CheckSerializers(
                value,
                ArrayOfStructStruct.Size,
                ArrayOfStructStructSerializer.Size,
                ArrayOfStructStructSerializer.CalculateSize,
                ArrayOfStructStructSerializer.Serialize,
                ArrayOfStructStructSerializer.Deserialize);
        }
    }
}
