//
// Copyright (c) 2020 Chris Gunn
//

using System;

namespace BitSerialization.Reflection.Utilities
{
    internal static class ReflectionExtensions
    {
        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsEnum && !type.IsPrimitive;
        }
    }
}
