using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitSerialization.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace BitSerialization.SourceGen
{
    [Generator]
    public class HelloWorldGenerator :
        ISourceGenerator
    {
        public void Initialize(InitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        class SyntaxReceiver :
            ISyntaxReceiver
        {
            public List<TypeDeclarationSyntax> CandidateClasses { get; } = new List<TypeDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode.Kind() == SyntaxKind.ClassDeclaration ||
                     syntaxNode.Kind() == SyntaxKind.StructDeclaration)
                {
                    var typeDeclarationSyntax = (TypeDeclarationSyntax)syntaxNode;
                    if (typeDeclarationSyntax.AttributeLists.Count > 0)
                    {
                        CandidateClasses.Add(typeDeclarationSyntax);
                    }
                }
            }
        }

        public void Execute(SourceGeneratorContext context)
        {
            WritePrimitivesSerializerClass(context);
            WriteSerializerClasses(context);
        }

        public void WritePrimitivesSerializerClass(SourceGeneratorContext context)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.Append(@"
namespace BitSerialization.Generated
{
    internal static class BitPrimitivesSerializer
    {
        public static bool TryWriteByte(global::System.Span<byte> destination, byte value)
        {
            if (destination.Length < 1)
            {
                return false;
            }
            destination[0] = value;
            return true;
        }

        public static bool TryWriteSByte(global::System.Span<byte> destination, sbyte value)
        {
            if (destination.Length < 1)
            {
                return false;
            }
            destination[0] = (byte)value;
            return true;
        }

        public static bool TryReadByte(global::System.ReadOnlySpan<byte> source, out byte value)
        {
            if (source.Length < 1)
            {
                value = default;
                return false;
            }
            value = source[0];
            return true;
        }

        public static bool TryReadSByte(global::System.ReadOnlySpan<byte> source, out sbyte value)
        {
            if (source.Length < 1)
            {
                value = default;
                return false;
            }
            value = (sbyte)source[0];
            return true;
        }
    }
}
");

            string sourceCode = sourceBuilder.ToString();
            context.AddSource("BitPrimitivesSerializer.cs", SourceText.From(sourceCode, Encoding.UTF8));
        }

        private struct IntegerOrEnumTypeInfo
        {
            public string SerializeTypeCast;
            public string DeserializeTypeCast;
            public string SerializeFuncName;
            public string DeserializeFuncName;
            public int TypeSize;
        }

        private static IntegerOrEnumTypeInfo GetIntegerOrEnumTypeInfo(INamedTypeSymbol typeSymbol, BitEndianess endianess)
        {
            IntegerOrEnumTypeInfo result;

            string endianessName = endianess == BitEndianess.BigEndian ?
                "BigEndian" :
                "LittleEndian";

            ITypeSymbol fieldUnderlyingType = typeSymbol;
            result.SerializeTypeCast = string.Empty;
            result.DeserializeTypeCast = string.Empty;

            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                fieldUnderlyingType = typeSymbol.EnumUnderlyingType;
                result.SerializeTypeCast = $"({SourceGenUtils.GetTypeFullName(fieldUnderlyingType)})";
                result.DeserializeTypeCast = $"({SourceGenUtils.GetTypeFullName(typeSymbol)})";
            }

            switch (fieldUnderlyingType.SpecialType)
            {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
                result.SerializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryWrite{fieldUnderlyingType.Name}";
                result.DeserializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryRead{fieldUnderlyingType.Name}";
                break;

            case SpecialType.System_UInt16:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt64:
            case SpecialType.System_Int64:
                result.SerializeFuncName = $"global::System.Buffers.Binary.BinaryPrimitives.TryWrite{fieldUnderlyingType.Name}{endianessName}";
                result.DeserializeFuncName = $"global::System.Buffers.Binary.BinaryPrimitives.TryRead{fieldUnderlyingType.Name}{endianessName}";
                break;

            default:
                throw new Exception();
            }

            switch (fieldUnderlyingType.SpecialType)
            {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
                result.TypeSize = 1;
                break;

            case SpecialType.System_UInt16:
            case SpecialType.System_Int16:
                result.TypeSize = 2;
                break;

            case SpecialType.System_UInt32:
            case SpecialType.System_Int32:
                result.TypeSize = 4;
                break;

            case SpecialType.System_UInt64:
            case SpecialType.System_Int64:
                result.TypeSize = 8;
                break;

            default:
                throw new Exception();
            }

            return result;
        }

        public void WriteSerializerClasses(SourceGeneratorContext context)
        {
            // retreive the populated receiver
            SyntaxReceiver receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null)
            {
                return;
            }

            INamedTypeSymbol bitStructAttributeSymbol = context.Compilation.GetTypeByMetadataName("BitSerialization.Common.BitStructAttribute");
            INamedTypeSymbol bitArrayAttributeSymbol = context.Compilation.GetTypeByMetadataName("BitSerialization.Common.BitArrayAttribute");

            foreach (TypeDeclarationSyntax classDeclarationSyntax in receiver.CandidateClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                INamedTypeSymbol classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax);

                BitStructAttribute bitStructAttribute = SourceGenUtils.GetAttribute<BitStructAttribute>(classSymbol, bitStructAttributeSymbol);
                if (bitStructAttribute == null)
                {
                    continue;
                }

                string classFullName = SourceGenUtils.GetTypeFullName(classSymbol);
                string serializerClassName = CreateSerializerName(classSymbol);

                var sourceBuilder = new StringBuilder();
                sourceBuilder.Append($@"
namespace {classSymbol.ContainingNamespace}
{{
    internal static class {serializerClassName}
    {{
");

                var serializeFuncBuilder = new StringBuilder();
                var deserializeFuncBuilder = new StringBuilder();

                serializeFuncBuilder.Append($@"
        public static global::System.Span<byte> Serialize(global::System.Span<byte> output, {classFullName} value)
        {{
");

                deserializeFuncBuilder.Append($@"
        public static global::System.ReadOnlySpan<byte> Deserialize(global::System.ReadOnlySpan<byte> input, out {classFullName} value)
        {{
            value = new {classFullName}();
");

                foreach (ISymbol classMember in classSymbol.GetMembers())
                {
                    IFieldSymbol classMemberAsField = classMember as IFieldSymbol;
                    if (classMemberAsField == null ||
                        classMemberAsField.IsStatic ||
                        classMemberAsField.IsConst)
                    {
                        continue;
                    }

                    if (classMemberAsField.Type.IsIntegerType() ||
                        classMemberAsField.Type.TypeKind == TypeKind.Enum)
                    {
                        INamedTypeSymbol fieldType = (INamedTypeSymbol)classMemberAsField.Type;

                        IntegerOrEnumTypeInfo fieldTypeInfo = GetIntegerOrEnumTypeInfo(fieldType, bitStructAttribute.Endianess);

                        serializeFuncBuilder.Append($@"
            if (!{fieldTypeInfo.SerializeFuncName}(output, {fieldTypeInfo.SerializeTypeCast}value.{classMemberAsField.Name}))
            {{
                throw new global::System.Exception(string.Format(""Not enough space to serialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
            }}
            output = output.Slice({fieldTypeInfo.TypeSize});
");

                        deserializeFuncBuilder.Append($@"
            {{
                if (!{fieldTypeInfo.DeserializeFuncName}(input, out var fieldValue))
                {{
                    throw new global::System.Exception(string.Format(""Not enough data to deserialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                }}
                value.{classMemberAsField.Name} = {fieldTypeInfo.DeserializeTypeCast}fieldValue;
                input = input.Slice({fieldTypeInfo.TypeSize});
            }}
");
                    }
                    else if (classMemberAsField.Type.TypeKind == TypeKind.Class ||
                        classMemberAsField.Type.TypeKind == TypeKind.Struct)
                    {
                        if (SourceGenUtils.GetAttributeData(classMemberAsField.Type, bitStructAttributeSymbol) == null)
                        {
                            throw new Exception($"Type {classMemberAsField.Type.Name} must have a BitStruct attribute.");
                        }

                        string serializerClassFullName = CreateSerializerFullName((INamedTypeSymbol)classMemberAsField.Type);

                        serializeFuncBuilder.Append($@"
            output = {serializerClassFullName}.Serialize(output, value.{classMemberAsField.Name});
");

                        deserializeFuncBuilder.Append($@"
            {{
                input = {serializerClassFullName}.Deserialize(input, out var fieldValue);
                value.{classMemberAsField.Name} = fieldValue;
            }}
");
                    }
                    else if (classMemberAsField.Type.TypeKind == TypeKind.Array)
                    {
                        var attributeData = SourceGenUtils.GetAttributeData(classMemberAsField, bitArrayAttributeSymbol);

                        BitArrayAttribute bitArrayAttribute = SourceGenUtils.GetAttribute<BitArrayAttribute>(classMemberAsField, bitArrayAttributeSymbol);
                        if (bitArrayAttribute == null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Field {classMemberAsField.Name} of type {classSymbol.Name} must have a BitArray attribute.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                            continue;
                        }

                        IArrayTypeSymbol arrayType = (IArrayTypeSymbol)classMemberAsField.Type;
                        string elementTypeFullName = SourceGenUtils.GetTypeFullName(arrayType.ElementType);

                        string serializeItem = string.Empty;
                        string deserializeItem = string.Empty;

                        if (arrayType.ElementType.IsIntegerType() ||
                            arrayType.ElementType.TypeKind == TypeKind.Enum)
                        {
                            IntegerOrEnumTypeInfo elementTypeInfo = GetIntegerOrEnumTypeInfo((INamedTypeSymbol)arrayType.ElementType, bitStructAttribute.Endianess);

                            serializeItem = $@"
                        if (!{elementTypeInfo.SerializeFuncName}(output, {elementTypeInfo.SerializeTypeCast}item))
                        {{
                            throw new global::System.Exception(string.Format(""Not enough space to serialize item from list {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                        }}
                        output = output.Slice({elementTypeInfo.TypeSize});
";

                            deserializeItem = $@"
                        if (!{elementTypeInfo.DeserializeFuncName}(input, out var item))
                        {{
                            throw new global::System.Exception(string.Format(""Not enough data to deserialize item from list {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                        }}
                        input = input.Slice({elementTypeInfo.TypeSize});
";
                        }
                        else if (arrayType.ElementType.TypeKind == TypeKind.Class ||
                            arrayType.ElementType.TypeKind == TypeKind.Struct)
                        {
                            string elementSerializerClassFullName = CreateSerializerFullName((INamedTypeSymbol)arrayType.ElementType);
                            serializeItem = $@"output = {elementSerializerClassFullName}.Serialize(output, item);";
                            deserializeItem = $@"input = {elementSerializerClassFullName}.Deserialize(input, out var item);";
                        }
                        else
                        {
                            //throw new Exception($"Can't serialize type {arrayType.ElementType.Name}.");
                        }

                        switch (bitArrayAttribute.SizeType)
                        {
                        case BitArraySizeType.Const:
                            serializeFuncBuilder.Append($@"
            {{
                var array = value.{classMemberAsField.Name};
                int collectionCount = array?.Length ?? 0;
                if (collectionCount > {bitArrayAttribute.ConstSize})
                {{
                    throw new global::System.Exception(string.Format($""Constant size list {{0}} from type {{1}} has too many items."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                }}

                if (array != null)
                {{
                    foreach (var item in array)
                    {{
                        {serializeItem}
                    }}
                }}

                int backfillCount = {bitArrayAttribute.ConstSize} - collectionCount;
                if (backfillCount > 0)
                {{
                    {elementTypeFullName} item = default;
                    for (int i = 0; i != backfillCount; ++i)
                    {{
                        {serializeItem}
                    }}
                }}
            }}
");

                            deserializeFuncBuilder.Append($@"
            {{
                var array = new {elementTypeFullName}[{bitArrayAttribute.ConstSize}];

                for (int i = 0; i != {bitArrayAttribute.ConstSize}; ++i)
                {{
                    {deserializeItem}
                    array[i] = item;
                }}

                value.{classMemberAsField.Name} = array;
            }}
");

                            break;

                        case BitArraySizeType.EndFill:
                            serializeFuncBuilder.Append($@"
            {{
                var array = value.{classMemberAsField.Name};
                if (array != null)
                {{
                    foreach (var item in array)
                    {{
                        {serializeItem}
                    }}
                }}
            }}
");
                            deserializeFuncBuilder.Append($@"
                var list = new global::System.Collections.Generic.List<{elementTypeFullName}>();

                while (!input.IsEmpty)
                {{
                    {deserializeItem}
                    list.Add(item);
                }}

                value.{classMemberAsField.Name} = list.ToArray();
");

                            break;

                        default:
                            throw new Exception($"Unknown BitArraySizeType value of {bitArrayAttribute.SizeType}");
                        }
                    }
                    else
                    {
                        //throw new Exception($"Can't serialize type {classMemberAsField.Type.Name}.");
                    }
                }

                serializeFuncBuilder.Append($@"
            return output;
        }}
");

                deserializeFuncBuilder.Append($@"
            return input;
        }}
");

                sourceBuilder.Append(serializeFuncBuilder.ToString());
                sourceBuilder.Append(deserializeFuncBuilder.ToString());
                sourceBuilder.Append($@"
    }}
}}
");

                string sourceCode = sourceBuilder.ToString();
                context.AddSource($"{serializerClassName}.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
        }

        private static string CreateSerializerName(INamedTypeSymbol classSymbol)
        {
            return classSymbol.ContainingType != null ?
                $"{classSymbol.ContainingType.Name}_{classSymbol.Name}Serializer" :
                $"{classSymbol.Name}Serializer";
        }

        private static string CreateSerializerFullName(INamedTypeSymbol classSymbol)
        {
            return $"global::{classSymbol.ContainingNamespace}.{CreateSerializerName(classSymbol)}";
        }
    }
}
