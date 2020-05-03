using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Compilation compilation = context.Compilation;

            // retreive the populated receiver
            SyntaxReceiver receiver = context.SyntaxReceiver as SyntaxReceiver;
            if (receiver == null)
            {
                return;
            }

            INamedTypeSymbol bitStructAttributeSymbol = compilation.GetTypeByMetadataName("BitSerialization.Common.BitStructAttribute");

            foreach (TypeDeclarationSyntax classDeclarationSyntax in receiver.CandidateClasses)
            {
                SemanticModel model = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
                INamedTypeSymbol classSymbol = model.GetDeclaredSymbol(classDeclarationSyntax);

                AttributeData attributeData = classSymbol.GetAttributes().FirstOrDefault((ad) => ad.AttributeClass.Equals(bitStructAttributeSymbol, SymbolEqualityComparer.Default));
                if (attributeData == null)
                {
                    continue;
                }

                string className = classSymbol.Name;

                string classFullName = classSymbol.ContainingType != null ?
                    $"global::{classSymbol.ContainingType.ContainingNamespace}.{classSymbol.ContainingType.Name}.{classSymbol.Name}" :
                    $"global::{classSymbol.ContainingNamespace}.{classSymbol.Name}";

                string serializerClassName = classSymbol.ContainingType != null ?
                    $"{classSymbol.ContainingType.Name}_{className}Serializer" :
                    $"{className}Serializer";

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
                    if (classMemberAsField == null)
                    {
                        continue;
                    }

                    string serializeException = $@"throw new Exception(string.Format(""Not enough space to serialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{className}""));";
                    string deserializeException = $@"throw new Exception(string.Format(""Not enough space to deserialize field {{0}} from type {{1}}."", ""{classMemberAsField.Name}"", ""{className}""));";

                    switch (classMemberAsField.Type.SpecialType)
                    {
                    case SpecialType.System_Byte:
                    {
                        serializeFuncBuilder.Append($@"
            if (output.Length < 1)
            {{
                {serializeException}
            }}
            output[0] = value.{classMemberAsField.Name};
            output = output.Slice(1);
");

                        deserializeFuncBuilder.Append($@"
            if (input.Length < 1)
            {{
                {deserializeException}
            }}
            value.{classMemberAsField.Name} = input[0];
            input = input.Slice(1);
");
                        break;
                    }
                    case SpecialType.System_SByte:
                    {
                        serializeFuncBuilder.Append($@"
            if (output.Length < 1)
            {{
                {serializeException}
            }}
            output[0] = (byte)value.{classMemberAsField.Name};
            output = output.Slice(1);
");

                        deserializeFuncBuilder.Append($@"
            if (input.Length < 1)
            {{
                {deserializeException}
            }}
            value.{classMemberAsField.Name} = (sbyte)input[0];
            input = input.Slice(1);
");
                        break;
                    }
                    case SpecialType.System_UInt16:
                    {
                        serializeFuncBuilder.Append($@"
            if (!BinaryPrimitives.TryWriteInt16LittleEndian(itr, fieldValue))
            {{
                {serializeException}
            }}
            output = output.Slice(2);
");
                        break;
                    }
                    case SpecialType.System_Int16:
                    {

                        break;
                    }
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
    }
}
