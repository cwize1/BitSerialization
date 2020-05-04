using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BitSerialization.SourceGen
{
    internal static class SourceGenUtils
    {
        public static bool IsIntegerType(this ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt64:
            case SpecialType.System_Int64:
                return true;

            default:
                return false;
            }
        }

        public static T CreateAttributeInstance<T>(AttributeData attributeData)
        {
            Type type = typeof(T);
            T result = (T)Activator.CreateInstance(typeof(T), attributeData.ConstructorArguments.Select((arg) => arg.Value).ToArray());

            foreach (var kvp in attributeData.NamedArguments)
            {
                type.GetProperty(kvp.Key).SetValue(result, kvp.Value.Value);
            }

            return result;
        }
    }
}
