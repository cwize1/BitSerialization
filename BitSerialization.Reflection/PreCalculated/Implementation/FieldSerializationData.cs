//
// Copyright (c) 2020 Chris Gunn
//

using System.Reflection;

namespace BitSerialization.Reflection.PreCalculated.Implementation
{
    internal struct FieldSerializationData
    {
        public FieldInfo FieldInfo;
        public DeserializeFieldHandler DeserializeFunc;
        public SerializeFieldHandler SerializeFunc;

        public FieldSerializationData(FieldInfo fieldInfo, DeserializeFieldHandler deserializeFunc, SerializeFieldHandler serializeFunc)
        {
            this.FieldInfo = fieldInfo;
            this.DeserializeFunc = deserializeFunc;
            this.SerializeFunc = serializeFunc;
        }
    }
}
