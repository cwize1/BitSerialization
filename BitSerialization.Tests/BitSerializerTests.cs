using BitSerialization.Common;
using BitSerialization.Tests.Types;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace BitSerialization.Tests
{
    public class BitSerializerTests
    {
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

            byte[] output1 = new byte[BasicStruct.Size];
            BitSerializer.Serialize(value, output1);

            byte[] output2 = new byte[BasicStruct.Size];
            BitSerializerStruct<BasicStruct>.Serialize(output2, value);

            byte[] output3 = new byte[BasicStruct.Size];
            BitSerializerTests_BasicStructSerializer.Serialize(output3, value);

            Assert.Equal(output1, output2);
            Assert.Equal(output1, output3);

            BasicStruct value1;
            BitSerializer.Deserialize(output1, out value1);

            BasicStruct value2;
            BitSerializerStruct<BasicStruct>.Deserialize(output2, out value2);

            BasicStruct value3;
            BitSerializerTests_BasicStructSerializer.Deserialize(output3, out value3);

            Assert.Equal(value, value1);
            Assert.Equal(value, value2);
            Assert.Equal(value, value3);
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct PrimitiveConstArraysStruct
        {
            public const int Size = 60;

            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public byte[] A;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public sbyte[] B;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public ushort[] C;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public short[] D;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public uint[] E;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public int[] F;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public ulong[] G;
            [BitArray(BitArraySizeType.Const, ConstSize = 2)]
            public long[] H;
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

            byte[] output1 = new byte[PrimitiveConstArraysStruct.Size];
            BitSerializer.Serialize(value, output1);

            byte[] output2 = new byte[PrimitiveConstArraysStruct.Size];
            BitSerializerStruct<PrimitiveConstArraysStruct>.Serialize(output2, value);

            byte[] output3 = new byte[PrimitiveConstArraysStruct.Size];
            BitSerializerTests_PrimitiveConstArraysStructSerializer.Serialize(output3, value);

            Assert.Equal(output1, output2);
            Assert.Equal(output1, output3);

            PrimitiveConstArraysStruct value1;
            BitSerializer.Deserialize(output1, out value1);

            PrimitiveConstArraysStruct value2;
            BitSerializerStruct<PrimitiveConstArraysStruct>.Deserialize(output2, out value2);

            PrimitiveConstArraysStruct value3;
            BitSerializerTests_PrimitiveConstArraysStructSerializer.Deserialize(output3, out value3);
        }

        [Fact]
        public void ConstArray2()
        {
            StructB value = new StructB()
            {
                H = new byte[] { 0x11, 0x12, 0x13, 0x14 },
                I = new byte[] { 0x21, 0x22, 0x23, 0x24 },
                J = new byte[] { 0x31, 0x32, 0x33, 0x34 },
            };

            byte[] output = new byte[StructB.Size];
            BitSerializerStruct<StructB>.Serialize(output, value);

            StructB value2;
            BitSerializerStruct<StructB>.Deserialize(output, out value2);
        }

        [Fact]
        public void EndFillArray()
        {
            StructC value = new StructC()
            {
                A = new byte[] { 0x01, 0x02 },
                B = new byte[] { 0x11, 0x12 },
                C = new StructC.StructCSub[]
                {
                    new StructC.StructCSub()
                    {
                        Value = new byte[] { 0x21, 0x22 },
                    },
                    new StructC.StructCSub()
                    {
                        Value = new byte[] { 0x31, 0x32 },
                    },
                }
            };

            byte[] output = new byte[StructC.Size(2)];
            BitSerializer.Serialize(value, output);

            StructC value2;
            BitSerializer.Deserialize(output, out value2);
        }

        [Fact]
        public void EndFillArray2()
        {
            StructC value = new StructC()
            {
                A = new byte[] { 0x01, 0x02 },
                B = new byte[] { 0x11, 0x12 },
                C = new StructC.StructCSub[]
                {
                    new StructC.StructCSub()
                    {
                        Value = new byte[] { 0x21, 0x22 },
                    },
                    new StructC.StructCSub()
                    {
                        Value = new byte[] { 0x31, 0x32 },
                    },
                }
            };

            byte[] output = new byte[StructC.Size(2)];
            BitSerializerStruct<StructC>.Serialize(output, value);

            StructC value2;
            BitSerializerStruct<StructC>.Deserialize(output, out value2);
        }
    }
}
