using System;

namespace BitSerialization.Common.Implementation
{
    internal static class ReflectionExtensions
    {
        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }
    }
}
