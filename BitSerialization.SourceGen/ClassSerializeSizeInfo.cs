using System;
using System.Collections.Generic;
using System.Text;

namespace BitSerialization.SourceGen
{
    internal enum ClassSerializeSizeType
    {
        Dynamic,
        Const,
    }

    internal struct ClassSerializeSizeInfo
    {
        public ClassSerializeSizeType Type;
        public int ConstSize;

        public static bool operator==(ClassSerializeSizeInfo a, ClassSerializeSizeInfo b)
        {
            return a.Type == b.Type &&
                a.ConstSize == b.ConstSize;
        }

        public static bool operator !=(ClassSerializeSizeInfo a, ClassSerializeSizeInfo b)
        {
            return !(a == b);
        }
    }
}
