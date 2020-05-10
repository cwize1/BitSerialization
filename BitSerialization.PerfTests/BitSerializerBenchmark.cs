//
// Copyright (c) 2020 Chris Gunn
//

using BenchmarkDotNet.Attributes;
using BitSerialization.Common;
using BitSerialization.Reflection.OnTheFly;
using BitSerialization.Reflection.Precalculated;
using System;
using System.Runtime.InteropServices;

namespace BitSerialization.PerfTests
{
    public partial class BitSerializerBenchmark
    {
        internal enum Enum1 : int
        {
            A,
            B,
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct Struct1
        {
            public byte A;
            public sbyte B;
            public ushort C;
            public short D;
            public uint E;
            public int F;
            public ulong G;
            public long H;
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct Struct2
        {
            public Struct1 A;
            public Enum1 B;
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        internal struct Struct3
        {
            [BitArray(BitArraySizeType.EndFill)]
            public Struct2[]? AnArray;
        }

        private readonly Struct3 _Data;
        private readonly int _DataSerializationSize;

        public BitSerializerBenchmark()
        {
            Random random = new Random();

            _Data = new Struct3()
            {
                AnArray = new Struct2[200],
            };

            for (int i = 0; i != _Data.AnArray.Length; ++i)
            {
                _Data.AnArray[i] = new Struct2()
                {
                    A = new Struct1()
                    {
                        A = (byte)random.Next(byte.MinValue, byte.MaxValue + 1),
                        B = (sbyte)random.Next(sbyte.MinValue, sbyte.MaxValue + 1),
                        C = (ushort)random.Next(ushort.MinValue, ushort.MaxValue + 1),
                        D = (short)random.Next(short.MinValue, short.MaxValue + 1),
                        E = (uint)random.Next(),
                        F = random.Next(),
                        G = (uint)random.Next(),
                        H = random.Next(),
                    },
                    B = (Enum1)random.Next(),
                };
            }

            _DataSerializationSize = Struct3Serializer.CalculateSize(_Data);
        }

        [Benchmark]
        public void OnTheFly()
        {
            byte[] output = new byte[_DataSerializationSize];
            BitSerializer.Serialize(output, _Data);

            Struct3 value;
            BitSerializer.Deserialize(output, out value);
        }

        [Benchmark]
        public void Precalculated()
        {
            byte[] output = new byte[_DataSerializationSize];
            BitSerializer<Struct3>.Serialize(output, _Data);

            Struct3 value;
            BitSerializer<Struct3>.Deserialize(output, out value);
        }

        [Benchmark(Baseline = true)]
        public void CodeGen()
        {
            byte[] output = new byte[_DataSerializationSize];
            Struct3Serializer.Serialize(output, _Data);

            Struct3 value;
            Struct3Serializer.Deserialize(output, out value);
        }
    }
}
