using BitSerialization.Common;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace BitSerialization.SourceGen
{
    internal struct IntegerOrEnumTypeInfo
    {
        public string SerializeTypeCast;
        public string DeserializeTypeCast;
        public string SerializeFuncName;
        public string DeserializeFuncName;
        public int TypeSize;
    }
}
