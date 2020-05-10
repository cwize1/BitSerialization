//
// Copyright (c) 2020 Chris Gunn
//

using System;
using System.Reflection;

namespace BitSerialization.Reflection.PreCalculated.Implementation
{
    // Function type for deserializing a field on an object.
    // Note: The FieldInfo value is passed into this function to allow the JIT-compiler to optimize the FieldInfo.SetValue call
    // when the type is known at JIT-compile-time.
    internal delegate ReadOnlySpan<byte> DeserializeFieldHandler(ReadOnlySpan<byte> source, FieldInfo fieldInfo, object obj);

    // Function type for serializing a field on an object.
    // Note: The FieldInfo value is passed into this function to allow the JIT-compiler to optimize the FieldInfo.GetValue call
    // when the type is known at JIT-compile-time.
    internal delegate Span<byte> SerializeFieldHandler(Span<byte> destination, FieldInfo fieldInfo, object obj);

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
