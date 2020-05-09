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
    public class SerializerClassGenerator :
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
            INamedTypeSymbol bitStructAttributeSymbol = context.Compilation.GetTypeByMetadataName("BitSerialization.Common.BitStructAttribute");
            INamedTypeSymbol bitArrayAttributeSymbol = context.Compilation.GetTypeByMetadataName("BitSerialization.Common.BitArrayAttribute");

            List<INamedTypeSymbol> bitStructClasses = FindAllBitStructClasses(context, bitStructAttributeSymbol, bitArrayAttributeSymbol);
            if (bitStructClasses == null)
            {
                return;
            }

            Dictionary<INamedTypeSymbol, ClassSerializeSizeInfo> classesSizeInfo = GenerateClassSizeInfo(context, bitStructClasses, bitStructAttributeSymbol, bitArrayAttributeSymbol);

            WritePrimitivesSerializerClass(context);

            WriteSerializerClasses(context, bitStructClasses, classesSizeInfo, bitStructAttributeSymbol, bitArrayAttributeSymbol);
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

        private List<INamedTypeSymbol> FindAllBitStructClasses(SourceGeneratorContext context, INamedTypeSymbol bitStructAttributeSymbol, INamedTypeSymbol bitArrayAttributeSymbol)
        {
            // retreive the populated receiver
            SyntaxReceiver receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null)
            {
                return null;
            }

            List<INamedTypeSymbol> result = new List<INamedTypeSymbol>();
            foreach (TypeDeclarationSyntax classDeclarationSyntax in receiver.CandidateClasses)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                INamedTypeSymbol classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax);

                if (SourceGenUtils.HasAttribute(classSymbol, bitStructAttributeSymbol))
                {
                    result.Add(classSymbol);
                }
            }

            return result;
        }

        private IEnumerable<IFieldSymbol> GetClassFieldMembers(INamedTypeSymbol classSymbol)
        {
            foreach (ISymbol classMember in classSymbol.GetMembers())
            {
                IFieldSymbol classMemberAsField = classMember as IFieldSymbol;
                if (classMemberAsField == null ||
                    classMemberAsField.IsStatic ||
                    classMemberAsField.IsConst)
                {
                    continue;
                }

                yield return classMemberAsField;
            }
        }

        private Dictionary<INamedTypeSymbol, ClassSerializeSizeInfo> GenerateClassSizeInfo(SourceGeneratorContext context, IReadOnlyList<INamedTypeSymbol> bitStructClasses,
            INamedTypeSymbol bitStructAttributeSymbol, INamedTypeSymbol bitArrayAttributeSymbol)
        {
            var classesSizeInfo = new Dictionary<INamedTypeSymbol, ClassSerializeSizeInfo>();
            foreach (INamedTypeSymbol classSymbol in bitStructClasses)
            {
                classesSizeInfo.Add(classSymbol, new ClassSerializeSizeInfo()
                {
                    Type = ClassSerializeSizeType.Dynamic,
                    ConstSize = 0,
                });
            }

            for (bool changed = true; changed; changed = false)
            {
                foreach (INamedTypeSymbol classSymbol in bitStructClasses)
                {
                    ClassSerializeSizeInfo classSizeInfo = new ClassSerializeSizeInfo()
                    {
                        Type = ClassSerializeSizeType.Const,
                        ConstSize = 0,
                    };

                    foreach (IFieldSymbol classFieldMember in GetClassFieldMembers(classSymbol))
                    {
                        if (classFieldMember.Type.IsIntegerType() ||
                            classFieldMember.Type.TypeKind == TypeKind.Enum)
                        {
                            INamedTypeSymbol fieldType = (INamedTypeSymbol)classFieldMember.Type;
                            IntegerOrEnumTypeInfo fieldTypeInfo = GetIntegerOrEnumTypeInfo(fieldType, BitEndianess.LittleEndian);

                            classSizeInfo.ConstSize += fieldTypeInfo.TypeSize;
                        }
                        else if (classFieldMember.Type.TypeKind == TypeKind.Class ||
                            classFieldMember.Type.TypeKind == TypeKind.Struct)
                        {
                            if (!SourceGenUtils.HasAttribute(classFieldMember.Type, bitStructAttributeSymbol))
                            {
                                throw new Exception($"Type {classFieldMember.Type.Name} must have a BitStruct attribute.");
                            }

                            INamedTypeSymbol fieldType = (INamedTypeSymbol)classFieldMember.Type;
                            ClassSerializeSizeInfo fieldTypeSizeInfo = classesSizeInfo[fieldType];

                            if (fieldTypeSizeInfo.Type == ClassSerializeSizeType.Const)
                            {
                                classSizeInfo.ConstSize += fieldTypeSizeInfo.ConstSize;
                            }
                            else
                            {
                                classSizeInfo.Type = ClassSerializeSizeType.Dynamic;
                            }
                        }
                        else if (classFieldMember.Type.TypeKind == TypeKind.Array)
                        {
                            IArrayTypeSymbol arrayType = (IArrayTypeSymbol)classFieldMember.Type;

                            BitArrayAttribute bitArrayAttribute = SourceGenUtils.GetAttribute<BitArrayAttribute>(classFieldMember, bitArrayAttributeSymbol);
                            if (bitArrayAttribute == null)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Field {classFieldMember.Name} of type {classSymbol.Name} must have a BitArray attribute.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                                continue;
                            }

                            ClassSerializeSizeInfo elementTypeSizeInfo;

                            if (arrayType.ElementType.IsIntegerType() ||
                                arrayType.ElementType.TypeKind == TypeKind.Enum)
                            {
                                IntegerOrEnumTypeInfo elementTypeInfo = GetIntegerOrEnumTypeInfo((INamedTypeSymbol)arrayType.ElementType, BitEndianess.LittleEndian);
                                elementTypeSizeInfo = new ClassSerializeSizeInfo()
                                {
                                    Type = ClassSerializeSizeType.Const,
                                    ConstSize = elementTypeInfo.TypeSize,
                                };
                            }
                            else if (arrayType.ElementType.TypeKind == TypeKind.Class ||
                                arrayType.ElementType.TypeKind == TypeKind.Struct)
                            {
                                INamedTypeSymbol elementType = (INamedTypeSymbol)arrayType.ElementType;
                                elementTypeSizeInfo = classesSizeInfo[elementType];
                            }
                            else
                            {
                                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Can't serialize type of {arrayType.ElementType.Name}.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                                continue;
                            }

                            switch (bitArrayAttribute.SizeType)
                            {
                            case BitArraySizeType.Const:
                                if (elementTypeSizeInfo.Type == ClassSerializeSizeType.Dynamic)
                                {
                                    classSizeInfo.Type = ClassSerializeSizeType.Dynamic;
                                }
                                else
                                {
                                    classSizeInfo.ConstSize += elementTypeSizeInfo.ConstSize * bitArrayAttribute.ConstSize;
                                }
                                break;

                            case BitArraySizeType.EndFill:
                                classSizeInfo.Type = ClassSerializeSizeType.Dynamic;
                                break;

                            default:
                                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Unknown BitArraySizeType value of {bitArrayAttribute.SizeType}.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                                continue;
                            }
                        }
                        else
                        {
                            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Can't serialize type of {classSymbol.Name}.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                            continue;
                        }
                    }

                    if (classSizeInfo != classesSizeInfo[classSymbol])
                    {
                        classesSizeInfo[classSymbol] = classSizeInfo;
                        changed = true;
                    }
                }
            }

            return classesSizeInfo;
        }

        private void WriteSerializerClasses(SourceGeneratorContext context, List<INamedTypeSymbol> bitStructClasses, Dictionary<INamedTypeSymbol, ClassSerializeSizeInfo> classesSizeInfo,
            INamedTypeSymbol bitStructAttributeSymbol, INamedTypeSymbol bitArrayAttributeSymbol)
        {
            foreach (INamedTypeSymbol classSymbol in bitStructClasses)
            {
                ClassSerializeSizeInfo classSizeInfo = classesSizeInfo[classSymbol];

                string classFullName = SourceGenUtils.GetTypeFullName(classSymbol);
                string serializerClassName = CreateSerializerName(classSymbol);

                BitStructAttribute bitStructAttribute = SourceGenUtils.GetAttribute<BitStructAttribute>(classSymbol, bitStructAttributeSymbol);

                var sourceBuilder = new StringBuilder();
                sourceBuilder.Append($@"
namespace {classSymbol.ContainingNamespace}
{{
");
                ITypeSymbol[] classContainingTypes = SourceGenUtils.GetContainingTypesList(classSymbol);
                foreach (ITypeSymbol containingType in classContainingTypes)
                {
                    switch (containingType.TypeKind)
                    {
                    case TypeKind.Class:
                        sourceBuilder.Append($@"
    partial class {containingType.Name} {{
");
                        break;

                    case TypeKind.Struct:
                        sourceBuilder.Append($@"
    partial struct {containingType.Name} {{
");
                        break;

                    default:
                        context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("TMP", "TMP", $"Only expecting struct or class containing types. Have {containingType.TypeKind}.", "TMP", DiagnosticSeverity.Error, true), Location.Create("TMP", new TextSpan(), new LinePositionSpan())));
                        return;
                    }
                }

                sourceBuilder.Append($@"
    {SourceGenUtils.GetAccessibilityString(classSymbol.DeclaredAccessibility)} static class {serializerClassName}
    {{
");

                var sizeFuncBuilder = new StringBuilder();
                var serializeFuncBuilder = new StringBuilder();
                var deserializeFuncBuilder = new StringBuilder();

                if (classSizeInfo.Type == ClassSerializeSizeType.Const)
                {
                    sizeFuncBuilder.Append($@"
        public const int Size = {classSizeInfo.ConstSize};
");
                }

                sizeFuncBuilder.Append($@"
        public static int CalculateSize({classFullName} value)
        {{
            int result = {classSizeInfo.ConstSize};
");

                serializeFuncBuilder.Append($@"
        public static global::System.Span<byte> Serialize(global::System.Span<byte> output, {classFullName} value)
        {{
");

                deserializeFuncBuilder.Append($@"
        public static global::System.ReadOnlySpan<byte> Deserialize(global::System.ReadOnlySpan<byte> input, out {classFullName} value)
        {{
            value = new {classFullName}();
");

                foreach (IFieldSymbol classFieldMember in GetClassFieldMembers(classSymbol))
                {
                    if (classFieldMember.Type.IsIntegerType() ||
                        classFieldMember.Type.TypeKind == TypeKind.Enum)
                    {
                        INamedTypeSymbol fieldType = (INamedTypeSymbol)classFieldMember.Type;
                        IntegerOrEnumTypeInfo fieldTypeInfo = GetIntegerOrEnumTypeInfo(fieldType, bitStructAttribute.Endianess);

                        serializeFuncBuilder.Append($@"
            if (!{fieldTypeInfo.SerializeFuncName}(output, {fieldTypeInfo.SerializeTypeCast}value.{classFieldMember.Name}))
            {{
                throw new global::System.Exception(string.Format(""Not enough space to serialize field {{0}} from type {{1}}."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
            }}
            output = output.Slice({fieldTypeInfo.TypeSize});
");

                        deserializeFuncBuilder.Append($@"
            {{
                if (!{fieldTypeInfo.DeserializeFuncName}(input, out var fieldValue))
                {{
                    throw new global::System.Exception(string.Format(""Not enough data to deserialize field {{0}} from type {{1}}."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
                }}
                value.{classFieldMember.Name} = {fieldTypeInfo.DeserializeTypeCast}fieldValue;
                input = input.Slice({fieldTypeInfo.TypeSize});
            }}
");
                    }
                    else if (classFieldMember.Type.TypeKind == TypeKind.Class ||
                        classFieldMember.Type.TypeKind == TypeKind.Struct)
                    {
                        if (!SourceGenUtils.HasAttribute(classFieldMember.Type, bitStructAttributeSymbol))
                        {
                            continue;
                        }

                        INamedTypeSymbol fieldType = (INamedTypeSymbol)classFieldMember.Type;
                        ClassSerializeSizeInfo fieldTypeSizeInfo = classesSizeInfo[fieldType];

                        string serializerClassFullName = CreateSerializerFullName(fieldType);

                        if (fieldTypeSizeInfo.Type == ClassSerializeSizeType.Dynamic)
                        {
                            sizeFuncBuilder.Append($@"
            result += {serializerClassFullName}.CalculateSize(value.{classFieldMember.Name});
");
                        }

                        serializeFuncBuilder.Append($@"
            output = {serializerClassFullName}.Serialize(output, value.{classFieldMember.Name});
");

                        deserializeFuncBuilder.Append($@"
            {{
                input = {serializerClassFullName}.Deserialize(input, out var fieldValue);
                value.{classFieldMember.Name} = fieldValue;
            }}
");
                    }
                    else if (classFieldMember.Type.TypeKind == TypeKind.Array)
                    {
                        BitArrayAttribute bitArrayAttribute = SourceGenUtils.GetAttribute<BitArrayAttribute>(classFieldMember, bitArrayAttributeSymbol);
                        if (bitArrayAttribute == null)
                        {
                            continue;
                        }

                        IArrayTypeSymbol arrayType = (IArrayTypeSymbol)classFieldMember.Type;
                        string elementTypeFullName = SourceGenUtils.GetTypeFullName(arrayType.ElementType);

                        ClassSerializeSizeInfo elementTypeSizeInfo;
                        string calculateElementSize = null;
                        string serializeItem;
                        string deserializeItem;

                        if (arrayType.ElementType.IsIntegerType() ||
                            arrayType.ElementType.TypeKind == TypeKind.Enum)
                        {
                            IntegerOrEnumTypeInfo elementTypeInfo = GetIntegerOrEnumTypeInfo((INamedTypeSymbol)arrayType.ElementType, bitStructAttribute.Endianess);
                            elementTypeSizeInfo = new ClassSerializeSizeInfo()
                            {
                                Type = ClassSerializeSizeType.Const,
                                ConstSize = elementTypeInfo.TypeSize,
                            };

                            calculateElementSize = $"result += {elementTypeInfo.TypeSize};";

                            serializeItem = $@"
                        if (!{elementTypeInfo.SerializeFuncName}(output, {elementTypeInfo.SerializeTypeCast}item))
                        {{
                            throw new global::System.Exception(string.Format(""Not enough space to serialize item from list {{0}} from type {{1}}."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
                        }}
                        output = output.Slice({elementTypeInfo.TypeSize});
";

                            deserializeItem = $@"
                        if (!{elementTypeInfo.DeserializeFuncName}(input, out var tmp))
                        {{
                            throw new global::System.Exception(string.Format(""Not enough data to deserialize item from list {{0}} from type {{1}}."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
                        }}
                        var item = {elementTypeInfo.DeserializeTypeCast}tmp;
                        input = input.Slice({elementTypeInfo.TypeSize});
";
                        }
                        else if (arrayType.ElementType.TypeKind == TypeKind.Class ||
                            arrayType.ElementType.TypeKind == TypeKind.Struct)
                        {
                            INamedTypeSymbol elementType = (INamedTypeSymbol)arrayType.ElementType;
                            elementTypeSizeInfo = classesSizeInfo[elementType];

                            string elementSerializerClassFullName = CreateSerializerFullName(elementType);

                            if (elementTypeSizeInfo.Type == ClassSerializeSizeType.Const)
                            {
                                calculateElementSize = $@"result += {elementSerializerClassFullName}.Size;";
                            }
                            else
                            {
                                calculateElementSize = $@"result += {elementSerializerClassFullName}.CalculateSize(item);";
                            }

                            serializeItem = $@"output = {elementSerializerClassFullName}.Serialize(output, item);";
                            deserializeItem = $@"input = {elementSerializerClassFullName}.Deserialize(input, out var item);";
                        }
                        else
                        {
                            continue;
                        }

                        switch (bitArrayAttribute.SizeType)
                        {
                        case BitArraySizeType.Const:
                            if (elementTypeSizeInfo.Type == ClassSerializeSizeType.Dynamic)
                            {
                                sizeFuncBuilder.Append($@"
            {{
                var array = value.{classFieldMember.Name};
                int collectionCount = array?.Length ?? 0;
                if (collectionCount > {bitArrayAttribute.ConstSize})
                {{
                    throw new global::System.Exception(string.Format($""Constant size list {{0}} from type {{1}} has too many items."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
                }}

                if (array != null)
                {{
                    foreach (var item in array)
                    {{
                        {calculateElementSize}
                    }}
                }}

                int backfillCount = {bitArrayAttribute.ConstSize} - collectionCount;
                if (backfillCount > 0)
                {{
                    {elementTypeFullName} item = default;
                    for (int i = 0; i != backfillCount; ++i)
                    {{
                        {calculateElementSize}
                    }}
                }}
            }}
");
                            }

                            serializeFuncBuilder.Append($@"
            {{
                var array = value.{classFieldMember.Name};
                int collectionCount = array?.Length ?? 0;
                if (collectionCount > {bitArrayAttribute.ConstSize})
                {{
                    throw new global::System.Exception(string.Format($""Constant size list {{0}} from type {{1}} has too many items."", ""{classFieldMember.Name}"", ""{classSymbol.Name}""));
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

                value.{classFieldMember.Name} = array;
            }}
");

                            break;

                        case BitArraySizeType.EndFill:
                            sizeFuncBuilder.Append($@"
            {{
                var array = value.{classFieldMember.Name};
                if (array != null)
                {{
                    foreach (var item in array)
                    {{
                        {calculateElementSize}
                    }}
                }}
            }}
");

                            serializeFuncBuilder.Append($@"
            {{
                var array = value.{classFieldMember.Name};
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

                value.{classFieldMember.Name} = list.ToArray();
");

                            break;

                        default:
                            // Unknown BitArraySizeType.
                            continue;
                        }
                    }
                    else
                    {
                        // Can't serialize type.
                        continue;
                    }
                }

                sizeFuncBuilder.Append($@"
            return result;
        }}
");

                serializeFuncBuilder.Append($@"
            return output;
        }}
");

                deserializeFuncBuilder.Append($@"
            return input;
        }}
");

                sourceBuilder.Append(sizeFuncBuilder.ToString());
                sourceBuilder.Append(serializeFuncBuilder.ToString());
                sourceBuilder.Append(deserializeFuncBuilder.ToString());

                for (int i = 0; i != classContainingTypes.Length + 1; ++i)
                {
                    sourceBuilder.Append($@"
    }}
");
                }

                sourceBuilder.Append($@"
}}
");

                string sourceCode = sourceBuilder.ToString();
                context.AddSource($"{serializerClassName}.cs", SourceText.From(sourceCode, Encoding.UTF8));
            }
        }

        private static string CreateSerializerName(INamedTypeSymbol classSymbol)
        {
            return $"{classSymbol.Name}Serializer";
        }

        private static string CreateSerializerFullName(INamedTypeSymbol classSymbol)
        {
            return $"global::{SourceGenUtils.GetTypeNamespace(classSymbol)}.{CreateSerializerName(classSymbol)}";
        }
    }
}
