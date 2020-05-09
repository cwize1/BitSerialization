using BitSerialization.Common;
using BitSerialization.Reflection.Utilities;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BitSerialization.Reflection.OnTheFly
{
    public static class BitSerializer
    {
        public static int Serialize<T>(T value, Span<byte> output)
             where T : struct
        {
            Span<byte> itr = SerializeInternal(value, output);

            // Calculate the number of bytes used.
            return output.Length - itr.Length;
        }

        private static Span<byte> SerializeInternal(object value, Span<byte> output)
        {
            Span<byte> itr = output;

            Type type = value.GetType();
            BitStructAttribute structAttribute = type.GetCustomAttribute<BitStructAttribute>() ?? BitStructAttribute.Default;

            if (type.StructLayoutAttribute == null || type.StructLayoutAttribute.Value != LayoutKind.Sequential)
            {
                throw new Exception($"Type {type.Name} must have a LayoutKind.Sequential struct layout.");
            }

            // Gets the type's fields in the order in which they are declared.
            // Note: Sorting by MetadataToken sorts the fields by declaration order when StructLayout is set to LayoutKind.Sequential.
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy((field) => field.MetadataToken);

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                object fieldValueAsObject = field.GetValue(value)!;

                if (fieldType.IsArray)
                {
                    Array list = (Array)fieldValueAsObject;
                    Type elementType = fieldType.GetElementType()!;

                    BitArrayAttribute? arrayAttribute = field.GetCustomAttribute<BitArrayAttribute>();
                    if (arrayAttribute == null)
                    {
                        throw new Exception($"Field ${field.Name} from type ${type.Name} must be annotated with BitArrayAttribute.");
                    }

                    int backfillCount = 0;

                    switch (arrayAttribute.SizeType)
                    {
                    case BitArraySizeType.Const:
                        int collectionCount = list?.Length ?? 0;

                        if (collectionCount > arrayAttribute.ConstSize)
                        {
                            throw new Exception($"List ${field.Name} from type ${type.Name} is too large.");
                        }

                        backfillCount = arrayAttribute.ConstSize - collectionCount;
                        break;

                    case BitArraySizeType.EndFill:
                        break;

                    default:
                        throw new Exception($"Unknown BitArraySizeType value {arrayAttribute.SizeType}");
                    }

                    if (list != null)
                    {
                        foreach (object? item in list)
                        {
                            itr = SerializeValue(structAttribute.Endianess, item!, elementType!, field.Name, itr);
                        }
                    }

                    if (backfillCount > 0)
                    {
                        object defaultValue = Activator.CreateInstance(elementType!)!;
                        for (int i = 0; i != backfillCount; ++i)
                        {
                            itr = SerializeValue(structAttribute.Endianess, defaultValue, elementType!, field.Name, itr);
                        }
                    }
                }
                else
                {
                    itr = SerializeValue(structAttribute.Endianess, fieldValueAsObject, fieldType, field.Name, itr);
                }
            }

            return itr;
        }

        private static Span<byte> SerializeValue(BitEndianess endianess, object fieldValueAsObject, Type fieldType, string fieldName, Span<byte> itr)
        {
            // Check if the field's type is a struct.
            if (fieldType.IsStruct())
            {
                return SerializeInternal(fieldValueAsObject, itr);
            }

            if (fieldType.IsEnum)
            {
                fieldType = fieldType.GetEnumUnderlyingType();
                fieldValueAsObject = Convert.ChangeType(fieldValueAsObject, fieldType);
            }

            bool success;
            int fieldSize;

            switch (fieldValueAsObject)
            {
            case byte fieldValue:
                fieldSize = sizeof(byte);
                success = TryWriteUInt8(itr, fieldValue);
                break;

            case sbyte fieldValue:
                fieldSize = sizeof(sbyte);
                success = TryWriteInt8(itr, fieldValue);
                break;

            case short fieldValue:
                fieldSize = sizeof(short);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteInt16BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteInt16LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            case ushort fieldValue:
                fieldSize = sizeof(ushort);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteUInt16BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteUInt16LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            case int fieldValue:
                fieldSize = sizeof(int);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteInt32BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteInt32LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            case uint fieldValue:
                fieldSize = sizeof(uint);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteUInt32BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteUInt32LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            case long fieldValue:
                fieldSize = sizeof(long);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteInt64BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteInt64LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            case ulong fieldValue:
                fieldSize = sizeof(ulong);

                switch (endianess)
                {
                case BitEndianess.BigEndian:
                    success = BinaryPrimitives.TryWriteUInt64BigEndian(itr, fieldValue);
                    break;

                default:
                case BitEndianess.LittleEndian:
                    success = BinaryPrimitives.TryWriteUInt64LittleEndian(itr, fieldValue);
                    break;
                }
                break;

            default:
                throw new Exception($"Can't serialize type of {fieldType.Name} from field {fieldName}.");
            }

            if (!success)
            {
                throw new Exception($"Not enough space to serialize type {fieldType.Name} from field {fieldName}.");
            }

            return itr.Slice(fieldSize);
        }

        public static int Deserialize<T>(ReadOnlySpan<byte> input, out T value)
             where T : struct
        {
            object valueAsObject;
            ReadOnlySpan<byte> itr = DeserializeInternal(typeof(T), input, out valueAsObject);

            value = (T)valueAsObject;

            // Calculate the number of bytes used.
            return input.Length - itr.Length;
        }

        private static ReadOnlySpan<byte> DeserializeInternal(Type type, ReadOnlySpan<byte> input, out object value)
        {
            object result = Activator.CreateInstance(type)!;
            ReadOnlySpan<byte> itr = input;

            BitStructAttribute structAttribute = type.GetCustomAttribute<BitStructAttribute>() ?? BitStructAttribute.Default;

            if (type.StructLayoutAttribute == null || type.StructLayoutAttribute.Value != LayoutKind.Sequential)
            {
                throw new Exception($"Type {type.Name} must a LayoutKind.Sequential struct layout.");
            }

            // Gets the type's fields in the order in which they are declared.
            // Note: Sorting by MetadataToken sorts the fields by declaration order when StructLayout is set to LayoutKind.Sequential.
            IEnumerable<FieldInfo> fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy((field) => field.MetadataToken);

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                object fieldValue;

                if (fieldType.IsArray)
                {
                    Type elementType = fieldType.GetElementType()!;

                    BitArrayAttribute? arrayAttribute = field.GetCustomAttribute<BitArrayAttribute>();
                    if (arrayAttribute == null)
                    {
                        throw new Exception($"Field ${field.Name} from type ${type.Name} must be annotated with BitArrayAttribute.");
                    }

                    switch (arrayAttribute.SizeType)
                    {
                    case BitArraySizeType.Const:
                    {
                        int collectionSize = arrayAttribute.ConstSize;

                        Array list = Array.CreateInstance(elementType, collectionSize);
                        for (int i = 0; i != collectionSize; ++i)
                        {
                            object itemValue;
                            itr = DeserializeValue(structAttribute.Endianess, elementType, itr, field.Name, out itemValue);
                            list.SetValue(itemValue, i);
                        }

                        fieldValue = list;
                        break;
                    }
                    case BitArraySizeType.EndFill:
                    {
                        Type listType = typeof(List<>).MakeGenericType(elementType);
                        IList list = (IList)Activator.CreateInstance(listType)!;

                        while (!itr.IsEmpty)
                        {
                            object itemValue;
                            itr = DeserializeValue(structAttribute.Endianess, elementType!, itr, field.Name, out itemValue);
                            list.Add(itemValue);
                        }

                        fieldValue = listType.GetMethod(nameof(List<int>.ToArray))!.Invoke(list, null)!;
                        break;
                    }
                    default:
                        throw new Exception($"Unknown BitArraySizeType value {arrayAttribute.SizeType}");
                    }
                }
                else
                {
                    itr = DeserializeValue(structAttribute.Endianess, fieldType, itr, field.Name, out fieldValue);
                }

                field.SetValue(result, fieldValue);
            }

            value = result;
            return itr;
        }

        private static ReadOnlySpan<byte> DeserializeValue(BitEndianess endianess, Type valueType, ReadOnlySpan<byte> itr, string fieldName, out object value)
        {
            // Check if the field's type is a struct.
            if (valueType.IsValueType && !valueType.IsEnum && !valueType.IsPrimitive)
            {
                itr = DeserializeInternal(valueType, itr, out value);
                return itr;
            }

            Type underlyingValueType = valueType;
            if (valueType.IsEnum)
            {
                underlyingValueType = valueType.GetEnumUnderlyingType();
            }

            bool success;
            int fieldSize;

            if (underlyingValueType == typeof(byte))
            {
                fieldSize = sizeof(byte);

                byte fieldValue;
                success = TryReadUInt8(itr, out fieldValue);
                value = fieldValue;
            }
            else if (underlyingValueType == typeof(sbyte))
            {
                fieldSize = sizeof(sbyte);

                sbyte fieldValue;
                success = TryReadInt8(itr, out fieldValue);
                value = fieldValue;
            }
            else if (underlyingValueType == typeof(short))
            {
                fieldSize = sizeof(short);

                short fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadInt16BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadInt16LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else if (underlyingValueType == typeof(ushort))
            {
                fieldSize = sizeof(ushort);

                ushort fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadUInt16BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadUInt16LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else if (underlyingValueType == typeof(int))
            {
                fieldSize = sizeof(int);

                int fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadInt32BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadInt32LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else if (underlyingValueType == typeof(uint))
            {
                fieldSize = sizeof(uint);

                uint fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadUInt32BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadUInt32LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else if (underlyingValueType == typeof(long))
            {
                fieldSize = sizeof(long);

                long fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadInt64BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadInt64LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else if (underlyingValueType == typeof(ulong))
            {
                fieldSize = sizeof(ulong);

                ulong fieldValue;
                success = endianess == BitEndianess.BigEndian ?
                    BinaryPrimitives.TryReadUInt64BigEndian(itr, out fieldValue) :
                    BinaryPrimitives.TryReadUInt64LittleEndian(itr, out fieldValue);

                value = fieldValue;
            }
            else
            {
                throw new Exception($"Can't deserialize type of {valueType.Name} from field {fieldName}.");
            }

            if (!success)
            {
                throw new Exception($"Not enough bytes to deserialize field {fieldName} of type {valueType.Name}.");
            }

            if (valueType.IsEnum)
            {
                value = Enum.ToObject(valueType, value);
            }

            return itr.Slice(fieldSize);
        }

        private static bool TryWriteInt8(Span<byte> destination, sbyte value)
        {
            if (destination.Length < sizeof(sbyte))
            {
                return false;
            }

            destination[0] = (byte)value;
            return true;
        }

        private static bool TryWriteUInt8(Span<byte> destination, byte value)
        {
            if (destination.Length < sizeof(byte))
            {
                return false;
            }

            destination[0] = value;
            return true;
        }

        private static bool TryReadInt8(ReadOnlySpan<byte> source, out sbyte value)
        {
            if (source.Length < sizeof(sbyte))
            {
                value = default;
                return false;
            }

            value = (sbyte)source[0];
            return true;
        }

        private static bool TryReadUInt8(ReadOnlySpan<byte> source, out byte value)
        {
            if (source.Length < sizeof(byte))
            {
                value = default;
                return false;
            }

            value = source[0];
            return true;
        }
    }
}
