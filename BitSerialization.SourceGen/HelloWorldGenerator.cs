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
        struct IntegerTypeInfo
        {
            public string Name;
            public int Size;
            public bool HasEndianess;

            public IntegerTypeInfo(string name, int size, bool hasEndianess)
            {
                this.Name = name;
                this.Size = size;
                this.HasEndianess = hasEndianess;
            }
        }

        private IntegerTypeInfo[] IntegerTypeInfoList = new IntegerTypeInfo[]
        {
            new IntegerTypeInfo(nameof(Byte), sizeof(Byte), false),
            new IntegerTypeInfo(nameof(SByte), sizeof(SByte), false),
            new IntegerTypeInfo(nameof(UInt16), sizeof(UInt16), true),
            new IntegerTypeInfo(nameof(Int16), sizeof(Int16), true),
            new IntegerTypeInfo(nameof(UInt32), sizeof(UInt32), true),
            new IntegerTypeInfo(nameof(Int32), sizeof(Int32), true),
            new IntegerTypeInfo(nameof(UInt64), sizeof(UInt64), true),
            new IntegerTypeInfo(nameof(Int64), sizeof(Int64), true),
        };

        private string[] EndianNameList = new string[] { "LittleEndian", "BigEndian" };

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
using System;
using System.Buffers.Binary;

namespace BitSerialization.Generated
{
    internal static class BitPrimitivesSerializer
    {
        public static bool TrySerializeByte(Span<byte> output, byte value, out Span<byte> outputNew)
        {
            if (output.Length < 1)
            {
                outputNew = default;
                return false;
            }
            output[0] = value;
            outputNew = output.Slice(1);
            return true;
        }

        public static bool TryDeserializeByte(ReadOnlySpan<byte> input, out byte value, out ReadOnlySpan<byte> inputNew)
        {
            if (input.Length < 1)
            {
                value = default;
                inputNew = default;
                return false;
            }
            value = input[0];
            inputNew = input.Slice(1);
            return true;
        }

        public static bool TrySerializeSByte(Span<byte> output, sbyte value, out Span<byte> outputNew)
        {
            if (output.Length < 1)
            {
                outputNew = default;
                return false;
            }
            output[0] = (byte)value;
            outputNew = output.Slice(1);
            return true;
        }

        public static bool TryDeserializeSByte(ReadOnlySpan<byte> input, out sbyte value, out ReadOnlySpan<byte> inputNew)
        {
            if (input.Length < 1)
            {
                value = default;
                inputNew = default;
                return false;
            }
            value = (sbyte)input[0];
            inputNew = input.Slice(1);
            return true;
        }
");

            foreach (IntegerTypeInfo integerTypeInfo in IntegerTypeInfoList)
            {
                if (!integerTypeInfo.HasEndianess)
                {
                    continue;
                }

                foreach (string endianName in EndianNameList)
                {
                    sourceBuilder.Append($@"
        public static bool TrySerialize{integerTypeInfo.Name}{endianName}(Span<byte> output, {integerTypeInfo.Name} value, out Span<byte> outputNew)
        {{
            if (!BinaryPrimitives.TryWrite{integerTypeInfo.Name}{endianName}(output, value))
            {{
                outputNew = default;
                return false;
            }}
            outputNew = output.Slice({integerTypeInfo.Size});
            return true;
        }}

        public static bool TryDeserialize{integerTypeInfo.Name}{endianName}(ReadOnlySpan<byte> input, out {integerTypeInfo.Name} value, out ReadOnlySpan<byte> inputNew)
        {{
            if (!BinaryPrimitives.TryRead{integerTypeInfo.Name}{endianName}(input, out value))
            {{
                inputNew = default;
                return false;
            }}
            inputNew = input.Slice({integerTypeInfo.Size});
            return true;
        }}
");
                }
            }

            sourceBuilder.Append($@"
    }}
}}
");

            string sourceCode = sourceBuilder.ToString();
            context.AddSource("BitPrimitivesSerializer.cs", SourceText.From(sourceCode, Encoding.UTF8));
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
            Func<ISymbol, AttributeData> getBitStructAttribute = (symbol) => symbol.GetAttributes().FirstOrDefault((ad) => ad.AttributeClass.Equals(bitStructAttributeSymbol, SymbolEqualityComparer.Default));

            foreach (TypeDeclarationSyntax classDeclarationSyntax in receiver.CandidateClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                INamedTypeSymbol classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax);

                AttributeData attributeData = getBitStructAttribute(classSymbol);
                if (attributeData == null)
                {
                    continue;
                }

                BitStructAttribute bitStructAttribute = SourceGenUtils.CreateAttributeInstance<BitStructAttribute>(attributeData);
                string endianessName = bitStructAttribute.Endianess == BitEndianess.BigEndian ?
                    "BigEndian" :
                    "LittleEndian";

                string classFullName = GetFullName(classSymbol);
                string serializerClassName = CreateSerializerName(classSymbol);

                var sourceBuilder = new StringBuilder();
                sourceBuilder.Append($@"
using System;
namespace {classSymbol.ContainingNamespace}
{{
    internal static class {serializerClassName}
    {{
");

                var serializeFuncBuilder = new StringBuilder();
                var deserializeFuncBuilder = new StringBuilder();

                serializeFuncBuilder.Append($@"
        public static Span<byte> Serialize(Span<byte> output, {classFullName} value)
        {{
");

                deserializeFuncBuilder.Append($@"
        public static ReadOnlySpan<byte> Deserialize(ReadOnlySpan<byte> input, out {classFullName} value)
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

                        INamedTypeSymbol fieldUnderlyingType = fieldType;
                        string fieldSerializeFuncName;
                        string fieldDeserializeFuncName;
                        string fieldSerializeTypeCast = string.Empty;
                        string fieldDeserializeTypeCast = string.Empty;

                        if (classMemberAsField.Type.TypeKind == TypeKind.Enum)
                        {
                            fieldUnderlyingType = fieldType.EnumUnderlyingType;
                            fieldSerializeTypeCast = $"({fieldUnderlyingType.Name})";
                            fieldDeserializeTypeCast = $"({fieldType.Name})";
                        }

                        switch (fieldType.SpecialType)
                        {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                            fieldSerializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TrySerialize{fieldUnderlyingType.Name}";
                            fieldDeserializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryDeserialize{fieldUnderlyingType.Name}";
                            break;

                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Int64:
                        default:
                            fieldSerializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TrySerialize{fieldUnderlyingType.Name}{endianessName}";
                            fieldDeserializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryDeserialize{fieldUnderlyingType.Name}{endianessName}";
                            break;
                        }

                        serializeFuncBuilder.Append($@"
            if (!{fieldSerializeFuncName}(output, {fieldSerializeTypeCast}value.{classMemberAsField.Name}, out output))
            {{
                throw new Exception(string.Format(""Not enough space to serialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
            }}
");

                        deserializeFuncBuilder.Append($@"
            {{
                if (!{fieldDeserializeFuncName}(input, out var fieldValue, out input))
                {{
                    throw new Exception(string.Format(""Not enough space to deserialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                }}
                value.{classMemberAsField.Name} = {fieldDeserializeTypeCast}fieldValue;
            }}
");
                    }
                    else if (classMemberAsField.Type.TypeKind == TypeKind.Class ||
                        classMemberAsField.Type.TypeKind == TypeKind.Struct)
                    {
                        if (getBitStructAttribute(classMemberAsField.Type) == null)
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

                    }
                    else
                    {
                        continue;
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

        private static string GetFullName(INamedTypeSymbol classSymbol)
        {
            return $"{GetClassNamespace(classSymbol)}.{classSymbol.Name}";
        }

        private static string GetClassNamespace(INamedTypeSymbol classSymbol)
        {
            return classSymbol.ContainingType != null ?
                $"global::{classSymbol.ContainingType.ContainingNamespace}.{classSymbol.ContainingType.Name}" :
                $"global::{classSymbol.ContainingNamespace}";
        }

        private static string CreateSerializerName(INamedTypeSymbol classSymbol)
        {
            return classSymbol.ContainingType != null ?
                $"{classSymbol.ContainingType.Name}_{classSymbol.Name}Serializer" :
                $"{classSymbol.Name}Serializer";
        }

        private static string CreateSerializerFullName(INamedTypeSymbol classSymbol)
        {
            return $"{GetClassNamespace(classSymbol)}.{CreateSerializerName(classSymbol)}";
        }
    }
}
