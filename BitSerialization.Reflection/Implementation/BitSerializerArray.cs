using System;
using System.Collections.Generic;
using System.Reflection;

namespace BitSerialization.Reflection.Implementation
{
    internal abstract class BitSerializerArray<T>
        where T : struct
    {
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

        public ReadOnlySpan<byte> DeserializeField(ReadOnlySpan<byte> itr, FieldInfo fieldInfo, TypedReference obj)
        {
            T[] value;
            itr = Deserialize(itr, out value);
            fieldInfo.SetValueDirect(obj, value);
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

        public Span<byte> SerializeField(Span<byte> itr, FieldInfo fieldInfo, TypedReference obj)
        {
            itr = Serialize(itr, (T[])fieldInfo.GetValueDirect(obj)!);
            return itr;
        }

        protected abstract ReadOnlySpan<byte> DeserializeItem(ReadOnlySpan<byte> itr, out T value);
        protected abstract Span<byte> SerializeItem(Span<byte> itr, in T value);
    }
}
