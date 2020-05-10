using BitSerialization.Common;
using BitSerialization.Reflection.PreCalculated.Implementation;
using BitSerialization.Reflection.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BitSerialization.Reflection.PreCalculated
{
    public static class BitSerializer<T>
        where T : struct
    {
        private static readonly FieldSerializationData[] _Playbook;

        // Creates a playbook for serializing and deserializing the type T.
        static BitSerializer()
        {
            Type type = typeof(T);

            if (!type.IsLayoutSequential)
            {
                throw new Exception($"Type {type.Name} must have a LayoutKind.Sequential struct layout.");
            }

            BitStructAttribute structAttribute = type.GetCustomAttribute<BitStructAttribute>() ?? BitStructAttribute.Default;

            // Gets the type's fields in the order in which they are declared.
            // Note: Sorting by MetadataToken sorts the fields by declaration order when StructLayout is set to LayoutKind.Sequential.
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy((field) => field.MetadataToken);

            List<FieldSerializationData> playbook = new List<FieldSerializationData>();
            foreach (FieldInfo fieldInfo in fields)
            {
                DeserializeFieldHandler? deserializeFunc = null;
                SerializeFieldHandler? serializeFunc = null;
                bool handled = false;

                if (fieldInfo.FieldType.IsArray)
                {
                    // Get the array's serialization settings.
                    BitArrayAttribute? arrayAttribute = fieldInfo.GetCustomAttribute<BitArrayAttribute>();
                    if (arrayAttribute == null)
                    {
                        throw new Exception($"Field ${fieldInfo.Name} from type ${type.Name} must be annotated with BitArrayAttribute.");
                    }

                    switch (arrayAttribute.SizeType)
                    {
                    case BitArraySizeType.Const:
                    case BitArraySizeType.EndFill:
                        break;

                    default:
                        throw new Exception($"Unknown BitArraySizeType value of {arrayAttribute.SizeType}");
                    }

                    Type elementType = fieldInfo.FieldType.GetElementType()!;

                    if (elementType.IsArray)
                    {
                        // Can't handle array of arrays directly as serialization settings are required for the nested arrays.
                        throw new Exception($"Cannot serialize a pure array of array for field {fieldInfo.Name} of type {type.Name}. Use a wrapper struct instead.");
                    }
                    else if (elementType.IsStruct() ||
                        elementType.IsClass ||
                        elementType.IsPrimitive ||
                        elementType.IsEnum)
                    {
                        Type? arraySerializerType = null;

                        if (elementType.IsStruct() || elementType.IsClass)
                        {
                            // Create an array serializer type for this object type.
                            arraySerializerType = typeof(BitSerializerStructArray<>).MakeGenericType(elementType);
                        }
                        else
                        {
                            // If element type is an enum, get the underlying integer type to use for serialization.
                            Type elementUnderlyingType = elementType.IsEnum ?
                                elementType.GetEnumUnderlyingType() :
                                elementType;

                            Dictionary<Type, BitSerializerPrimitives.TypeData> types = BitSerializerPrimitives.GetTypeData(structAttribute.Endianess);

                            // Get the array serializer class type for the integer type.
                            if (types.TryGetValue(elementUnderlyingType, out var typeData))
                            {
                                arraySerializerType = typeData.ArraySerializerType;

                                if (elementType.IsEnum)
                                {
                                    // Change the array serializer class's generic type parameter to the enum type.
                                    arraySerializerType = arraySerializerType.GetGenericTypeDefinition().MakeGenericType(elementType);
                                }
                            }
                        }

                        if (arraySerializerType != null)
                        {
                            // Create an instance of the array serialization type.
                            object arraySerializer = Activator.CreateInstance(arraySerializerType, arrayAttribute)!;

                            // Get references to the serialization and deserialization member functions.
                            deserializeFunc = (DeserializeFieldHandler)arraySerializerType.GetMethod(nameof(BitSerializerArray<int>.DeserializeField), BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate(typeof(DeserializeFieldHandler), arraySerializer);
                            serializeFunc = (SerializeFieldHandler)arraySerializerType.GetMethod(nameof(BitSerializerArray<int>.SerializeField), BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate(typeof(SerializeFieldHandler), arraySerializer);
                            handled = true;
                        }
                    }
                }
                else if (fieldInfo.FieldType.IsEnum ||
                    fieldInfo.FieldType.IsPrimitive)
                {
                    // If element type is an enum, get the underlying integer type to use for serialization.
                    Type fieldUnderlyingType = fieldInfo.FieldType.IsEnum ?
                        fieldInfo.FieldType.GetEnumUnderlyingType() :
                        fieldInfo.FieldType;

                    Dictionary<Type, BitSerializerPrimitives.TypeData> types = BitSerializerPrimitives.GetTypeData(structAttribute.Endianess);

                    // Get the serialization methods for the integer type.
                    if (types.TryGetValue(fieldUnderlyingType, out var typeData))
                    {
                        deserializeFunc = typeData.DeserializeFunc;
                        serializeFunc = typeData.SerializeFunc;

                        if (fieldInfo.FieldType.IsEnum)
                        {
                            // Change deserialization function's generic type parameter to the enum type.
                            // Note: This isn't required for the serialization function since an enum wrapped in an object box can be directly
                            // cast to the integer type.
                            MethodInfo deserializeFuncInfo = deserializeFunc.GetMethodInfo()!;
                            deserializeFunc = (DeserializeFieldHandler)deserializeFuncInfo.GetGenericMethodDefinition().MakeGenericMethod(fieldInfo.FieldType).CreateDelegate(typeof(DeserializeFieldHandler));
                        }

                        handled = true;
                    }
                }
                else if (fieldInfo.FieldType.IsStruct() ||
                    fieldInfo.FieldType.IsClass)
                {
                    // Get the BitSerializer type for this object.
                    Type bitSerializerStructType = typeof(BitSerializer<>).MakeGenericType(fieldInfo.FieldType);

                    // Get references to the serialization and deserialization static functions.
                    deserializeFunc = (DeserializeFieldHandler)bitSerializerStructType.GetMethod(nameof(DeserializeField), BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate(typeof(DeserializeFieldHandler));
                    serializeFunc = (SerializeFieldHandler)bitSerializerStructType.GetMethod(nameof(SerializeField), BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate(typeof(SerializeFieldHandler));
                    handled = true;
                }

                if (!handled)
                {
                    throw new Exception($"Can't serialize type of {fieldInfo.FieldType.Name} from field {fieldInfo.Name}.");
                }

                playbook.Add(new FieldSerializationData(fieldInfo, deserializeFunc!, serializeFunc!));
            }

            _Playbook = playbook.ToArray();
        }

        public static ReadOnlySpan<byte> Deserialize(ReadOnlySpan<byte> itr, out T value)
        {
            object valueAsObject = new T();

            foreach (FieldSerializationData play in _Playbook)
            {
                itr = play.DeserializeFunc(itr, play.FieldInfo, valueAsObject);
            }

            value = (T)valueAsObject;
            return itr;
        }

        private static ReadOnlySpan<byte> DeserializeField(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
        {
            T value;
            itr = Deserialize(itr, out value);
            fieldInfo.SetValue(obj, value);
            return itr;
        }

        public static Span<byte> Serialize(Span<byte> itr, in T value)
        {
            object valueAsObject = value;

            foreach (FieldSerializationData play in _Playbook)
            {
                itr = play.SerializeFunc(itr, play.FieldInfo, valueAsObject);
            }

            return itr;
        }

        private static Span<byte> SerializeField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            object? valueAsObject = fieldInfo.GetValue(obj);
            return Serialize(itr, in Unsafe.Unbox<T>(valueAsObject));
        }
    }
}
