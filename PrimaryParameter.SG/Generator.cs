using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("PrimaryParameter.Tests")]

namespace PrimaryParameter.SG;

// using https://andrewlock.net/series/creating-a-source-generator/
[Generator]
internal class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource("FieldAttribute.g.cs", """
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                    public sealed class FieldAttribute : Attribute
                    {
                        public string Name { get; init; }
                    }
                }
                """)
        );
        // Do a simple filter for enums
        var enumDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsSyntaxTargetForGeneration, // select enums with attributes
                transform: GetSemanticTargetForGeneration) // sect the enum with the [EnumExtensions] attribute
            .Where(static m => m is not null)!; // filter out attributed enums that we don't care about

        // Combine the selected enums with the `Compilation`
        var compilationAndEnums = context.CompilationProvider.Combine(enumDeclarations.Collect());

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(compilationAndEnums, static (spc, source) => Execute(source.Left, source.Right!, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode s, CancellationToken token)
        => s is ParameterSyntax { AttributeLists.Count: > 0, Parent.Parent: ClassDeclarationSyntax or StructDeclarationSyntax };

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
        var paramsToGenerate = GetTypesToGenerate(compilation, distinctParams, context);

        GenerateFiles(paramsToGenerate, context);


    }

    static IEnumerable<Parameter> GetTypesToGenerate(Compilation compilation, IEnumerable<ParameterSyntax> parameters, SourceProductionContext context)
    {
        if (compilation.GetTypeByMetadataName("PrimaryParameter.SG.FieldAttribute") is not INamedTypeSymbol fieldAttributeSymbol)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            yield break;
        }

        foreach (var paramSyntax in parameters)
        {
            // stop if we're asked to
            context.CancellationToken.ThrowIfCancellationRequested();

            // Get the semantic representation of the parameter syntax
            var semanticModel = compilation.GetSemanticModel(paramSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(paramSyntax) is not ISymbol paramSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            var containingType = (BaseTypeDeclarationSyntax)((ParameterListSyntax)paramSyntax.Parent!).Parent!;

            var fieldNames = new HashSet<string>();

            foreach (var attribute in paramSymbol.GetAttributes())
            {
                if (!fieldAttributeSymbol.Equals(attribute.AttributeClass, SymbolEqualityComparer.Default))
                {
                    // This isn't the [Field] attribute
                    continue;
                }

                fieldNames.Add(GetFieldName(attribute) ?? "_" + paramSyntax.Identifier.Text);
            }
            var parameter = new Parameter(GetNamespace(containingType), ParentClass.GetParentClasses(containingType)!, paramSyntax.Identifier.Text, semanticModel.GetTypeInfo(paramSyntax.Type!).Type!.ToDisplayString(), fieldNames.ToArray());
            yield return parameter;
            containingType.Accept(new SyntaxWalker(paramSyntax, semanticModel, context, parameter));
        }
    }

    static string? GetFieldName(AttributeData attributeData)
    {
        // This is the attribute, check all of the named arguments
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            // Is this the Name argument?
            if (namedArgument.Key == "Name" && namedArgument.Value.Value?.ToString() is { } n)
            {
                return n;
            }
        }

        return null;
    }

    static void GenerateFiles(IEnumerable<Parameter> parameters, SourceProductionContext context)
    {
        foreach (var item in parameters)
        {
            context.AddSource($"{item.Namespace}.{item.TypeName.ConcatTypeName()}.{item.ParamName}.g.cs", GetResource(item.Namespace, item.TypeName, item.FieldNames.Select(n => $"private readonly {item.ParamType} {n} = {item.ParamName};")));
        }
    }

    static string GetResource(string nameSpace, ParentClass? parentClass, IEnumerable<string> inner)
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
                .AppendLine(nameSpace)
                .AppendLine("{");
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
                .AppendLine(parentClass.Constraints) // e.g. where T: new()
                .Append(new string(' ', 4 * (parentsCount + 1)))
                .AppendLine(@"{");
            parentsCount++; // keep track of how many layers deep we are
            parentClass = parentClass.Child; // repeat with the next child
        }

        foreach (var item in inner)
        {
            // Write the actual target generation code here
            sb.Append(new string(' ', 4 * (parentsCount + 1)));
            sb.AppendLine(item);
        }

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
}