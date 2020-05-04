using BitSerialization.Common;
using BitSerialization.Tests.Types;
using System;
using Xunit;

namespace BitSerialization.Tests
{
    public class BitSerializerTests
    {
        [Fact]
        public void Basic()
        { 
            StructA value = new StructA()
            {
                A = 3,
                F = EnumA.A,
                G = EnumB.B,
            };

            byte[] output = new byte[StructA.Size];
            BitSerializer.Serialize(value, output);

            StructA value2;
            BitSerializer.Deserialize(output, out value2);
        }

        [Fact]
        public void Basic2()
        {
            StructA value = new StructA()
            {
                A = 3,
                F = EnumA.A,
                G = EnumB.B,
            };

            byte[] output = new byte[StructA.Size];
            BitSerializerStruct<StructA>.Serialize(output, value);

            StructA value2;
            BitSerializerStruct<StructA>.Deserialize(output, out value2);
        }

        [Fact]
        public void Basic3()
        {
            StructA value = new StructA()
            {
                A = 3,
                F = EnumA.A,
                G = EnumB.B,
            };

            byte[] output = new byte[StructA.Size];
            StructASerializer.Serialize(output, value);

            StructA value2;
            StructASerializer.Deserialize(output, out value2);
        }

        [Fact]
        public void ConstArray()
        {
            StructB value = new StructB()
            {
                H = new byte[] { 0x11, 0x12, 0x13, 0x14 },
                I = new byte[] { 0x21, 0x22, 0x23, 0x24 },
                J = new byte[] { 0x31, 0x32, 0x33, 0x34 },
            };

            byte[] output = new byte[StructB.Size];
            BitSerializer.Serialize(value, output);

            StructB value2;
            BitSerializer.Deserialize(output, out value2);
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
