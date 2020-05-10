//
// Copyright (c) 2020 Chris Gunn
//

using BitSerialization.Common;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BitSerialization.Reflection.PreCalculated.Implementation
{
    internal abstract class BitSerializerArray<T>
        where T : struct
    {
        // The serialization settings for the array.
        private BitArrayAttribute _Settings;

        public BitSerializerArray(BitArrayAttribute settings)
        {
            _Settings = settings;
        }

        public ReadOnlySpan<byte> Deserialize(ReadOnlySpan<byte> itr, out T[] value)
        {
            switch (_Settings.SizeType)
            {
            case BitArraySizeType.Const:
            {
                T[] result = new T[_Settings.ConstSize];

                for (int i = 0; i != _Settings.ConstSize; ++i)
                {
                    itr = DeserializeItem(itr, out result[i]);
                }

                value = result;
                return itr;
            }
            case BitArraySizeType.EndFill:
            {
                List<T> list = new List<T>();

                while (!itr.IsEmpty)
                {
                    T item;
                    itr = DeserializeItem(itr, out item);
                    list.Add(item);
                }

                value = list.ToArray();
                return itr;
            }
            default:
                throw new Exception($"Unknown BitArraySizeType value {_Settings.SizeType}");
            }
        }

        public ReadOnlySpan<byte> DeserializeField(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, object obj)
        {
            itr = Deserialize(itr, out var tmp);
            fieldInfo.SetValue(obj, tmp);
            return itr;
        }

        public Span<byte> Serialize(Span<byte> itr, T[]? value)
        {
            switch (_Settings.SizeType)
            {
            case BitArraySizeType.Const:
            {
                int collectionCount = value?.Length ?? 0;
                if (collectionCount > _Settings.ConstSize)
                {
                    throw new Exception($"List of type ${nameof(T)} is too large.");
                }

                if (value != null)
                {
                    foreach (T item in value)
                    {
                        itr = SerializeItem(itr, in item);
                    }
                }

                int backfillCount = _Settings.ConstSize - collectionCount;
                if (backfillCount > 0)
                {
                    T defaultValue = default;
                    for (int i = 0; i != backfillCount; ++i)
                    {
                        itr = SerializeItem(itr, in defaultValue);
                    }
                }

                return itr;
            }
            case BitArraySizeType.EndFill:
            {
                if (value != null)
                {
                    foreach (T item in value)
                    {
                        itr = SerializeItem(itr, in item);
                    }
                }

                return itr;
            }
            default:
                throw new Exception($"Unknown BitArraySizeType value {_Settings.SizeType}");
            }
        }

        public Span<byte> SerializeField(Span<byte> itr, FieldInfo fieldInfo, object obj)
        {
            itr = Serialize(itr, (T[]?)fieldInfo.GetValue(obj));
            return itr;
        }

        protected abstract ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value);
        protected abstract Span<byte> SerializeItem(Span<byte> itr, in T value);
    }
}
