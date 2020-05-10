using BenchmarkDotNet.Attributes;
using BitSerialization.Common;
using BitSerialization.Reflection.OnTheFly;
using BitSerialization.Reflection.PreCalculated;
using System;
using System.Runtime.InteropServices;

namespace BitSerialization.PerfTests
{
    public partial class BitSerializerBenchmark
    {
        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct BasicStruct
        {
            public byte A;
            public sbyte B;
            public ushort C;
            public short D;
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct ArrayOfBasicStruct
        {
            [BitArray(BitArraySizeType.EndFill)]
            public BasicStruct[]? AnArray;
        }

        private readonly ArrayOfBasicStruct _SimpleArray;
        private readonly int _SimpleArraySize;

        public BitSerializerBenchmark()
        {
            Random random = new Random();

            _SimpleArray = new ArrayOfBasicStruct()
            {
                AnArray = new BasicStruct[200],
            };

            for (int i = 0; i != _SimpleArray.AnArray.Length; ++i)
            {
                _SimpleArray.AnArray[i] = new BasicStruct()
                {
                    A = (byte)random.Next(byte.MinValue, byte.MaxValue + 1),
                    B = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue + 1),
                    C = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1),
                    D = (short)random.Next(short.MinValue, short.MaxValue + 1),
                };
            }

            _SimpleArraySize = ArrayOfBasicStructSerializer.CalculateSize(_SimpleArray);
        }

        [Benchmark]
        public void OnTheFly()
        {
            byte[] output = new byte[_SimpleArraySize];
            BitSerializer.Serialize(_SimpleArray, output);

            ArrayOfBasicStruct value;
            BitSerializer.Deserialize(output, out value);
        }

        [Benchmark]
        public void PreCalculated()
        {
            byte[] output = new byte[_SimpleArraySize];
            BitSerializer<ArrayOfBasicStruct>.Serialize(output, _SimpleArray);

            ArrayOfBasicStruct value;
            BitSerializer<ArrayOfBasicStruct>.Deserialize(output, out value);
        }

        [Benchmark(Baseline = true)]
        public void CodeGen()
        {
            byte[] output = new byte[_SimpleArraySize];
            ArrayOfBasicStructSerializer.Serialize(output, _SimpleArray);

            ArrayOfBasicStruct value;
            ArrayOfBasicStructSerializer.Deserialize(output, out value);
        }
    }
}
