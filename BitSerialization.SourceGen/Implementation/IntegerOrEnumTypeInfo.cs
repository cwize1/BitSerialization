//
// Copyright (c) 2020 Chris Gunn
//

namespace BitSerialization.SourceGen.Implementation
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
