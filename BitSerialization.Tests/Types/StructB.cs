using BitSerialization.Reflection;
using System.Runtime.InteropServices;

namespace BitSerialization.Tests.Types
{
    internal enum EnumC : ushort
    {
        A = 1 << 0,
        B = 1 << 1,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct StructB
    {
        public const int Size = 108;

        public ushort A;
        public ushort B;
        public ushort C;
        public ushort D;
        public ushort E;
        public EnumC F;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] G;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] H;
        [BitArray(BitArraySizeType.Const, ConstSize = 32)]
        public byte[] I;
    }
}
