using BitSerialization.Common;
using System.Runtime.InteropServices;

namespace BitSerialization.Tests.Types
{
    [StructLayout(LayoutKind.Sequential)]
    [BitStruct]
    internal struct StructC
    {
        public static int Size(int extensionCount)
        {
            return 40 + StructCSub.Size * extensionCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        [BitStruct]
        public struct StructCSub
        {
            public const int Size = 30;

            [BitArray(BitArraySizeType.Const, ConstSize = 30)]
            public byte[] Value;
        }

        [BitArray(BitArraySizeType.Const, ConstSize = 30)]
        public byte[] A;
        [BitArray(BitArraySizeType.Const, ConstSize = 10)]
        public byte[] B;
        [BitArray(BitArraySizeType.EndFill)]
        public StructCSub[] C;
    }
}
