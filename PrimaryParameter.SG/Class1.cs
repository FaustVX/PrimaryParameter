using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace PrimaryParameter.SG
{
    [Generator]
    internal class Class1 : ISourceGenerator
    {
        private readonly Receiver _receiver = new();
        public void Execute(GeneratorExecutionContext context)
        {
            foreach (var item in _receiver.ParameterList)
            {
                context.AddSource($"{item.Namespace}.{item.TypeName}.{item.ParamName}.g.cs", $$"""
                namespace {{item.Namespace}};
                public partial {{(item.IsClass ? "class" : "struct")}} {{item.TypeName}}
                {
                    private readonly {{item.ParamType}} _{{item.ParamName}} = {{item.ParamName}};
                }
                """);
                break;
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
            context.RegisterForSyntaxNotifications(Create);
        }

        private ISyntaxContextReceiver Create()
            => _receiver;

        public class Receiver : ISyntaxContextReceiver
        {
            private readonly List<Parameter> _parameterList = new();
            public IReadOnlyList<Parameter> ParameterList => _parameterList;

            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                if (context.Node is ParameterListSyntax { Parent: ClassDeclarationSyntax or StructDeclarationSyntax } parameterList)
                {
                    foreach(var item in parameterList.Parameters)
                        foreach (var attr in HasAttribute<FieldAttribute>(item, context.SemanticModel))
                        _parameterList.Add(new(GetNamespace(parameterList.Parent!), parameterList.Parent is ClassDeclarationSyntax, GetTypeName(parameterList), item.Identifier.Text, context.SemanticModel.GetTypeInfo(item.Type!).Type!));
                }
            }
        }

        static string GetTypeName(ParameterListSyntax parameterList)
            => parameterList.Parent switch
            {
                ClassDeclarationSyntax c => c.Identifier.Text,
                StructDeclarationSyntax s => s.Identifier.Text,
                _ => throw new NotImplementedException(),
            };

        static string GetNamespace(SyntaxNode syntaxNode)
            => string.Join(".", syntaxNode
                    .Ancestors()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .Reverse()
                    .Select(s => s.Name));

        static IEnumerable<T> HasAttribute<T>(ParameterSyntax parameter, SemanticModel semanticModel)
            where T : Attribute
        {
            foreach (var attrs in parameter.AttributeLists)
            {
                foreach (var attr in attrs.Attributes)
                {
                    if (TypeSymbolMatchesType(semanticModel.GetTypeInfo(attr).Type, typeof(T), semanticModel))
                        yield return default!;
                }
            }
        }
        static bool TypeSymbolMatchesType(ITypeSymbol? typeSymbol, Type type, SemanticModel semanticModel)
            => GetTypeSymbolForType(type, semanticModel).Equals(typeSymbol);

        static INamedTypeSymbol GetTypeSymbolForType(Type type, SemanticModel semanticModel)
        {

            if (!type.IsConstructedGenericType)
            {
                return semanticModel.Compilation.GetTypeByMetadataName(type.FullName)!;
            }

            // get all typeInfo's for the Type arguments 
            var typeArgumentsTypeInfos = type.GenericTypeArguments.Select(a => GetTypeSymbolForType(a, semanticModel));

            var openType = type.GetGenericTypeDefinition();
            var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(openType.FullName);
            return typeSymbol!.Construct(typeArgumentsTypeInfos.ToArray<ITypeSymbol>());
        }

        public record Parameter(string Namespace, bool IsClass, string TypeName, string ParamName, ITypeSymbol ParamType);
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
    public sealed class FieldAttribute : Attribute
    {
    }
}

namespace System.Runtime.CompilerServices
{
    sealed class IsExternalInit
    {
    }
}