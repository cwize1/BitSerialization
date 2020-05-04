using BitSerialization.Common;
using System.Runtime.InteropServices;

namespace BitSerialization.Tests.Types
{
    internal enum EnumC : ushort
    {
        A = 1 << 0,
        B = 1 << 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    [BitStruct]
    internal struct StructB
    {
        public const int Size = 108;

        public ushort A;
        public ushort B;
        public ushort C;
        public ushort D;
        public ushort E;
        public EnumC F;
        public StructA G;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] H;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] I;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] J;
        public ushort K;
    }
}
