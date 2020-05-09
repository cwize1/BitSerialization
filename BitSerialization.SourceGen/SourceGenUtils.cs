using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
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

        private static object ConvertTypedConstantToObject(TypedConstant typedConstant)
        {
            if (typedConstant.Type.TypeKind == TypeKind.Enum)
            {
                Type type = Type.GetType($"{typedConstant.Type.ContainingNamespace}.{typedConstant.Type.Name}, {typedConstant.Type.ContainingAssembly}", throwOnError: true);
                return Enum.ToObject(type, typedConstant.Value);
            }
            return typedConstant.Value;
        }

        public static T CreateAttributeInstance<T>(AttributeData attributeData)
        {
            Type type = typeof(T);

            object[] contructorArgs = attributeData.ConstructorArguments.Select(ConvertTypedConstantToObject).ToArray();
            T result = (T)Activator.CreateInstance(typeof(T), contructorArgs);

            foreach (var kvp in attributeData.NamedArguments)
            {
                type.GetProperty(kvp.Key).SetValue(result, kvp.Value.Value);
            }

            return result;
        }

        public static bool HasAttribute(ISymbol symbol, INamedTypeSymbol attributeSymbol)
        {
            return symbol.GetAttributes().Any((ad) => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        }

        public static AttributeData GetAttributeData(ISymbol symbol, INamedTypeSymbol attributeSymbol)
        {
            return symbol.GetAttributes().FirstOrDefault((ad) => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
        }

        public static T GetAttribute<T>(ISymbol symbol, INamedTypeSymbol attributeSymbol) where
            T : class
        {
            AttributeData attributeData = GetAttributeData(symbol, attributeSymbol);
            if (attributeData == null)
            {
                return null;
            }
            return CreateAttributeInstance<T>(attributeData);
        }

        public static string GetTypeFullName(ITypeSymbol typeSymbol)
        {
            return $"{GetTypeNamespace(typeSymbol)}.{typeSymbol.Name}";
        }

        public static string GetTypeNamespace(ITypeSymbol typeSymbol)
        {
            StringBuilder result = new StringBuilder("global::");
            result.Append(typeSymbol.ContainingNamespace);

            foreach (ITypeSymbol containingType in GetContainingTypesList(typeSymbol))
            {
                result.Append(".");
                result.Append(containingType.Name);
            }

            return result.ToString();
        }

        public static ITypeSymbol[] GetContainingTypesList(ITypeSymbol typeSymbol)
        {
            Stack<ITypeSymbol> containingTypes = new Stack<ITypeSymbol>();
            for (ITypeSymbol containingType = typeSymbol.ContainingType; containingType != null; containingType = containingType.ContainingType)
            {
                containingTypes.Push(containingType);
            }

            return containingTypes.ToArray();
        }

        public static string GetAccessibilityString(Accessibility accessibility)
        {
            switch (accessibility)
            {
            case Accessibility.Private:
                return "private";

            case Accessibility.ProtectedAndInternal:
                return "private protected";

            case Accessibility.Protected:
                return "protected";

            case Accessibility.Internal:
                return "internal";

            case Accessibility.ProtectedOrInternal:
                return "protected internal";

            case Accessibility.Public:
                return "public";

            case Accessibility.NotApplicable:
            default:
                throw new Exception($"Unknown accessibility value of {accessibility}.");
            }
        }
    }
}
