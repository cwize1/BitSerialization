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
                        throw new Exception($"Cannot serialize a pure array of array for field {fieldInfo.Name} of type {type.Name}. Use a wrapper struct instead.");
                    }
                    else if (elementType.IsStruct() ||
                        elementType.IsPrimitive ||
                        elementType.IsEnum)
                    {
                        Type? arraySerializerType = null;

                        if (elementType.IsStruct())
                        {
                            arraySerializerType = typeof(BitSerializerStructArray<>).MakeGenericType(elementType);
                        }
                        else
                        {
                            Type elementUnderlyingType = elementType.IsEnum ?
                                elementType.GetEnumUnderlyingType() :
                                elementType;

                            Dictionary<Type, BitSerializerPrimitives.TypeData> types = structAttribute.Endianess == BitEndianess.BigEndian ?
                                BitSerializerPrimitives.BigEndianTypes :
                                BitSerializerPrimitives.LittleEndianTypes;

                            if (types.TryGetValue(elementUnderlyingType, out var typeData))
                            {
                                arraySerializerType = typeData.ArraySerializerType;

                                if (elementType.IsEnum)
                                {
                                    arraySerializerType = arraySerializerType.GetGenericTypeDefinition().MakeGenericType(elementType);
                                }
                            }
                        }

                        if (arraySerializerType != null)
                        {
                            object arraySerializer = Activator.CreateInstance(arraySerializerType, arrayAttribute)!;
                            deserializeFunc = (DeserializeFieldHandler)arraySerializerType.GetMethod(nameof(BitSerializerArray<int>.DeserializeField), BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate(typeof(DeserializeFieldHandler), arraySerializer);
                            serializeFunc = (SerializeFieldHandler)arraySerializerType.GetMethod(nameof(BitSerializerArray<int>.SerializeField), BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate(typeof(SerializeFieldHandler), arraySerializer);
                            handled = true;
                        }
                    }
                }
                else if (fieldInfo.FieldType.IsEnum ||
                    fieldInfo.FieldType.IsPrimitive)
                {
                    Type fieldUnderlyingType = fieldInfo.FieldType.IsEnum ?
                        fieldInfo.FieldType.GetEnumUnderlyingType() :
                        fieldInfo.FieldType;

                    Dictionary<Type, BitSerializerPrimitives.TypeData> types = structAttribute.Endianess == BitEndianess.BigEndian ?
                        BitSerializerPrimitives.BigEndianTypes :
                        BitSerializerPrimitives.LittleEndianTypes;

                    if (types.TryGetValue(fieldUnderlyingType, out var typeData))
                    {
                        deserializeFunc = typeData.DeserializeFunc;
                        serializeFunc = typeData.SerializeFunc;

                        if (fieldInfo.FieldType.IsEnum)
                        {
                            MethodInfo deserializeFuncInfo = deserializeFunc.GetMethodInfo()!;
                            deserializeFunc = (DeserializeFieldHandler)deserializeFuncInfo.GetGenericMethodDefinition().MakeGenericMethod(fieldInfo.FieldType).CreateDelegate(typeof(DeserializeFieldHandler));
                        }

                        handled = true;
                    }
                }
                else if (fieldInfo.FieldType.IsStruct() ||
                    fieldInfo.FieldType.IsClass)
                {
                    Type bitSerializerStructType = typeof(BitSerializer<>).MakeGenericType(fieldInfo.FieldType);

                    deserializeFunc = (DeserializeFieldHandler)bitSerializerStructType.GetMethod(nameof(DeserializeField)).CreateDelegate(typeof(DeserializeFieldHandler));
                    serializeFunc = (SerializeFieldHandler)bitSerializerStructType.GetMethod(nameof(SerializeField)).CreateDelegate(typeof(SerializeFieldHandler));
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
            value = new T();
            TypedReference resultRef = __makeref(value);

            foreach (FieldSerializationData play in _Playbook)
            {
                itr = play.DeserializeFunc(itr, play.FieldInfo, resultRef);
            }

            return itr;
        }

        public static ReadOnlySpan<byte> DeserializeField(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, TypedReference obj)
        {
            T value;
            itr = Deserialize(itr, out value);
            fieldInfo.SetValueDirect(obj, value);
            return itr;
        }

        public static Span<byte> Serialize(Span<byte> itr, in T value)
        {
            TypedReference resultRef = __makeref(Unsafe.AsRef(value));

            foreach (FieldSerializationData play in _Playbook)
            {
                itr = play.SerializeFunc(itr, play.FieldInfo, resultRef);
            }

            return itr;
        }

        public static Span<byte> SerializeField(Span<byte> itr, FieldInfo fieldInfo, TypedReference obj)
        {
            object? valueAsObject = fieldInfo.GetValueDirect(obj);
            return Serialize(itr, in Unsafe.Unbox<T>(valueAsObject));
        }
    }
}
