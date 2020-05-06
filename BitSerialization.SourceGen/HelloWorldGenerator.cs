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
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitSerialization.Generated
{
    internal static class BitPrimitivesSerializer
    {
        public static bool TryWriteByte(Span<byte> destination, byte value)
        {
            if (destination.Length < 1)
            {
                return false;
            }
            destination[0] = value;
            return true;
        }

        public static bool TryWriteSByte(Span<byte> destination, sbyte value)
        {
            if (destination.Length < 1)
            {
                return false;
            }
            destination[0] = (byte)value;
            return true;
        }

        public static bool TryReadByte(ReadOnlySpan<byte> source, out byte value)
        {
            if (source.Length < 1)
            {
                value = default;
                return false;
            }
            value = source[0];
            return true;
        }

        public static bool TryReadSByte(ReadOnlySpan<byte> source, out sbyte value)
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

                        string fieldSerializeTypeCast = string.Empty;
                        string fieldDeserializeTypeCast = string.Empty;
                        if (classMemberAsField.Type.TypeKind == TypeKind.Enum)
                        {
                            fieldUnderlyingType = fieldType.EnumUnderlyingType;
                            fieldSerializeTypeCast = $"({fieldUnderlyingType.Name})";
                            fieldDeserializeTypeCast = $"({fieldType.Name})";
                        }

                        string fieldSerializeFuncName;
                        string fieldDeserializeFuncName;

                        switch (fieldUnderlyingType.SpecialType)
                        {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                            fieldSerializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryWrite{fieldUnderlyingType.Name}";
                            fieldDeserializeFuncName = $"global::BitSerialization.Generated.BitPrimitivesSerializer.TryRead{fieldUnderlyingType.Name}";
                            break;

                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Int64:
                            fieldSerializeFuncName = $"global::System.Buffers.Binary.BinaryPrimitives.TryWrite{fieldUnderlyingType.Name}{endianessName}";
                            fieldDeserializeFuncName = $"global::System.Buffers.Binary.BinaryPrimitives.TryRead{fieldUnderlyingType.Name}{endianessName}";
                            break;

                        default:
                            throw new Exception();
                        }

                        int typeSize;
                        switch (fieldUnderlyingType.SpecialType)
                        {
                        case SpecialType.System_Byte:
                        case SpecialType.System_SByte:
                            typeSize = 1;
                            break;

                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int16:
                            typeSize = 2;
                            break;

                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int32:
                            typeSize = 4;
                            break;

                        case SpecialType.System_UInt64:
                        case SpecialType.System_Int64:
                            typeSize = 8;
                            break;

                        default:
                            throw new Exception();
                        }

                            serializeFuncBuilder.Append($@"
            if (!{fieldSerializeFuncName}(output, {fieldSerializeTypeCast}value.{classMemberAsField.Name}))
            {{
                throw new Exception(string.Format(""Not enough space to serialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
            }}
            output = output.Slice({typeSize});
");

                        deserializeFuncBuilder.Append($@"
            {{
                if (!{fieldDeserializeFuncName}(input, out var fieldValue))
                {{
                    throw new Exception(string.Format(""Not enough space to deserialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{classSymbol.Name}""));
                }}
                value.{classMemberAsField.Name} = {fieldDeserializeTypeCast}fieldValue;
                input = input.Slice({typeSize});
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

                        switch (bitArrayAttribute.SizeType)
                        {
                        case BitArraySizeType.Const:
                            break;

                        case BitArraySizeType.EndFill:
                            break;

                        default:
                            throw new Exception($"Unknown BitArraySizeType value of {bitArrayAttribute.SizeType}");
                        }
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
