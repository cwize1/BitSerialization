using BitSerialization.Common;
using System.Runtime.InteropServices;

namespace BitSerialization.Tests.Types
{
    internal enum EnumA : ushort
    {
        A = 0b001,
        B = 0b010,
        C = 0b100,
    }

    internal enum EnumB : ushort
    {
        A = 0b001,
        B = 0b010,
    }

    [StructLayout(LayoutKind.Sequential)]
    [BitStruct]
    internal struct StructA
    {
        public const int Size = 20;

        public byte A;
        public byte B;
        public ushort C;
        public uint D;
        public uint E;
        public EnumA F;
        public EnumB G;
        public ushort H;
        public ushort I;
    }
}
