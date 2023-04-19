using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("PrimaryParameter.Tests")]

namespace PrimaryParameter.SG
{
    // using https://andrewlock.net/series/creating-a-source-generator/
    [Generator]
    internal class Generator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //Debugger.Launch();
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("FieldAttribute.g.cs", """
                    namespace PrimaryParameter.SG
                    {
                        [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                        public sealed class FieldAttribute : Attribute
                        {
                            public string Name { get; init; }
                        }
                    }
                    """);
            });
            // Do a simple filter for enums
            IncrementalValuesProvider<ParameterSyntax> enumDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: IsSyntaxTargetForGeneration, // select enums with attributes
                    transform: GetSemanticTargetForGeneration) // sect the enum with the [EnumExtensions] attribute
                .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

            // Combine the selected enums with the `Compilation`
            var compilationAndEnums = context.CompilationProvider.Combine(enumDeclarations.Collect());

            // Generate the source using the compilation and enums
            context.RegisterSourceOutput(compilationAndEnums, static (spc, source) => Execute(source.Left, source.Right, spc));
        }

        static bool IsSyntaxTargetForGeneration(SyntaxNode s, CancellationToken token)
            => s is ParameterSyntax { AttributeLists.Count: > 0, Parent.Parent: BaseTypeDeclarationSyntax };

        static ParameterSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken token)
        {
            // we know the node is a ParameterSyntax thanks to IsSyntaxTargetForGeneration
            var parameterSyntax = (ParameterSyntax)context.Node;

            // loop through all the attributes on the method
            foreach (var attributeListSyntax in parameterSyntax.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                    {
                        // weird, we couldn't get the symbol, ignore it
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // Is the attribute the [EnumExtensions] attribute?
                    if (fullName == "PrimaryParameter.SG.FieldAttribute")
                    {
                        // return the enum
                        return parameterSyntax;
                    }
                }
            }

            // we didn't find the attribute we were looking for
            return null;
        }

        static void Execute(Compilation compilation, ImmutableArray<ParameterSyntax> parameters, SourceProductionContext context)
        {
            if (parameters.IsDefaultOrEmpty)
            {
                // nothing to do yet
                return;
            }

            // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
            var distinctParams = parameters.Distinct();

            // Convert each EnumDeclarationSyntax to an EnumToGenerate
            var paramsToGenerate = GetTypesToGenerate(compilation, distinctParams, context.CancellationToken);

            GenerateFiles(paramsToGenerate, context);


        }

        static IEnumerable<Parameter> GetTypesToGenerate(Compilation compilation, IEnumerable<ParameterSyntax> parameters, CancellationToken ct)
        {
            if (compilation.GetTypeByMetadataName("PrimaryParameter.SG.FieldAttribute") == null)
            {
                // If this is null, the compilation couldn't find the marker attribute type
                // which suggests there's something very wrong! Bail out..
                yield break;
            }

            foreach (var paramSyntax in parameters)
            {
                // stop if we're asked to
                ct.ThrowIfCancellationRequested();

                // Get the semantic representation of the enum syntax
                var semanticModel = compilation.GetSemanticModel(paramSyntax.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(paramSyntax) is not ISymbol)
                {
                    // something went wrong, bail out
                    continue;
                }

                // Create an EnumToGenerate for use in the generation phase
                var containingType = (BaseTypeDeclarationSyntax)((ParameterListSyntax)paramSyntax.Parent!).Parent!;
                yield return new(GetNamespace(containingType), GetParentClasses(containingType)!, paramSyntax.Identifier.Text, compilation.GetSemanticModel(paramSyntax.SyntaxTree).GetTypeInfo(paramSyntax.Type!).Type!.ToDisplayString());
            }
        }

        static void GenerateFiles(IEnumerable<Parameter> parameters, SourceProductionContext context)
        {
            foreach (var item in parameters)
            {
                context.AddSource($"{item.Namespace}.{item.TypeName.ConcatTypeName()}.{item.ParamName}.g.cs", GetResource(item.Namespace, item.TypeName, $"private readonly {item.ParamType} _{item.ParamName} = {item.ParamName};"));
            }
        }

        static string GetResource(string nameSpace, ParentClass? parentClass, string inner)
        {
            var sb = new StringBuilder();
            var parentsCount = 0;

            // If we don't have a namespace, generate the code in the "default"
            // namespace, either global:: or a different <RootNamespace>
            var hasNamespace = !string.IsNullOrEmpty(nameSpace);
            if (hasNamespace)
            {
                // We could use a file-scoped namespace here which would be a little impler, 
                // but that requires C# 10, which might not be available. 
                // Depends what you want to support!
                sb
                    .Append("namespace ")
                    .Append(nameSpace)
                    .AppendLine(@"
    {");
            }

            // Loop through the full parent type hiearchy, starting with the outermost
            while (parentClass is not null)
            {
                sb
                    .Append("    partial ")
                    .Append(parentClass.Keyword) // e.g. class/struct/record
                    .Append(' ')
                    .Append(parentClass.Name) // e.g. Outer/Generic<T>
                    .Append(' ')
                    .Append(parentClass.Constraints) // e.g. where T: new()
                    .AppendLine(@"
        {");
                parentsCount++; // keep track of how many layers deep we are
                parentClass = parentClass.Child; // repeat with the next child
            }

            // Write the actual target generation code here. Not shown for brevity
            sb.Append(inner);

            // We need to "close" each of the parent types, so write
            // the required number of '}'
            for (var i = 0; i < parentsCount; i++)
            {
                sb.AppendLine(@"    }");
            }

            // Close the namespace, if we had one
            if (hasNamespace)
            {
                sb.Append('}').AppendLine();
            }

            return sb.ToString();
        }

        // determine the namespace the class/enum/struct is declared in, if any
        static string GetNamespace(BaseTypeDeclarationSyntax syntax)
        {
            // If we don't have a namespace at all we'll return an empty string
            // This accounts for the "default namespace" case
            var nameSpace = "";

            // Get the containing syntax node for the type declaration
            // (could be a nested type, for example)
            var potentialNamespaceParent = syntax.Parent;

            // Keep moving "out" of nested classes etc until we get to a namespace
            // or until we run out of parents
            while (potentialNamespaceParent != null && potentialNamespaceParent is not NamespaceDeclarationSyntax && potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
            {
                potentialNamespaceParent = potentialNamespaceParent.Parent;
            }

            // Build up the final namespace by looping until we no longer have a namespace declaration
            if (potentialNamespaceParent is BaseNamespaceDeclarationSyntax namespaceParent)
            {
                // We have a namespace. Use that as the type
                nameSpace = namespaceParent.Name.ToString();

                // Keep moving "out" of the namespace declarations until we 
                // run out of nested namespace declarations
                while (true)
                {
                    if (namespaceParent.Parent is not NamespaceDeclarationSyntax parent)
                    {
                        break;
                    }

                    // Add the outer namespace as a prefix to the final namespace
                    nameSpace = $"{namespaceParent.Name}.{nameSpace}";
                    namespaceParent = parent;
                }
            }

            // return the final namespace
            return nameSpace;
        }

        static ParentClass? GetParentClasses(BaseTypeDeclarationSyntax typeSyntax)
        {
            // Try and get the parent syntax. If it isn't a type like class/struct, this will be null
            var parentSyntax = typeSyntax.Parent as TypeDeclarationSyntax;
            var parentClassInfo = CreateParentClass((TypeDeclarationSyntax)typeSyntax, null);

            // Keep looping while we're in a supported nested type
            while (parentSyntax != null && IsAllowedKind(parentSyntax.Kind()))
            {
                // Record the parent type keyword (class/struct etc), name, and constraints
                parentClassInfo = CreateParentClass(parentSyntax, parentClassInfo);

                // Move to the next outer type
                parentSyntax = (parentSyntax.Parent as TypeDeclarationSyntax);
            }

            // return a link to the outermost parent type
            return parentClassInfo;

            static ParentClass CreateParentClass(TypeDeclarationSyntax typeSyntax, ParentClass? parent)
                => new(
                    Keyword: typeSyntax.Keyword.ValueText,
                    Name: typeSyntax.Identifier.ToString() + typeSyntax.TypeParameterList,
                    Constraints: typeSyntax.ConstraintClauses.ToString(),
                    Child: parent);
        }

        // We can only be nested in class/struct/record
        static bool IsAllowedKind(SyntaxKind kind)
            => kind is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.RecordDeclaration;

        record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType);

        record ParentClass(string Keyword, string Name, string Constraints, ParentClass? Child)
        {
            private IEnumerable<ParentClass> Iterate()
            {
                foreach (var child in Child?.Iterate() ?? Enumerable.Empty<ParentClass>())
                    yield return child;
                yield return this;
            }
            public string ConcatTypeName()
                => string.Join(".", Iterate().Select(static c => c.Name));
        }
    }
}

namespace System.Runtime.CompilerServices
{
    sealed class IsExternalInit
    { }
}