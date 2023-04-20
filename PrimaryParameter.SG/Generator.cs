﻿using Microsoft.CodeAnalysis;
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
        //System.Diagnostics.Debugger.Launch();
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
            ctx.AddSource("PropertyAttribute.g.cs", """
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                    public sealed class PropertyAttribute : Attribute
                    {
                        public string Name { get; init; }
                        public bool WithInit { get; init; }
                    }
                }
                """);
        });
        // Do a simple filter for paramerter
        var paramDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsSyntaxTargetForGeneration, // select params with attributes
                transform: GetSemanticTargetForGeneration) // sect the param with the [Field] or [Property] attribute
            .Where(static m => m is not null)!; // filter out attributed parameters that we don't care about

        // Combine the selected parameters with the `Compilation`
        var compilationAndParameters = context.CompilationProvider.Combine(paramDeclarations.Collect());

        // Generate the source using the compilation and parameters
        context.RegisterSourceOutput(compilationAndParameters, static (spc, source) => Execute(source.Left, source.Right!, spc));
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

                // Is the attribute the [Field] or [Property] attribute?
                if (fullName is "PrimaryParameter.SG.FieldAttribute" or "PrimaryParameter.SG.PropertyAttribute")
                {
                    // return the parameter
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
        if (compilation.GetTypeByMetadataName("PrimaryParameter.SG.PropertyAttribute") is not INamedTypeSymbol propertyAttributeSymbol)
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

            var memberNames = new HashSet<IGeneratedMember>();

            foreach (var attribute in paramSymbol.GetAttributes())
            {
                if (fieldAttributeSymbol.Equals(attribute.AttributeClass, SymbolEqualityComparer.Default))
                {
                    memberNames.Add(new GenerateField(GetAttributeProperty<string>(attribute, "Name") ?? ("_" + paramSyntax.Identifier.Text)));
                }
                else if (propertyAttributeSymbol.Equals(attribute.AttributeClass, SymbolEqualityComparer.Default))
                {
                    memberNames.Add(new GenerateProperty(GetAttributeProperty<string>(attribute, "Name") ?? (char.ToUpper(paramSyntax.Identifier.Text[0]) + paramSyntax.Identifier.Text[1..]), GetAttributeProperty<bool>(attribute, "WithInit")));
                }
                else
                {
                    // This isn't the [Field] nor [Property] attribute
                    continue;
                }

            }
            var parameter = new Parameter(GetNamespace(containingType), ParentClass.GetParentClasses(containingType)!, paramSyntax.Identifier.Text, semanticModel.GetTypeInfo(paramSyntax.Type!).Type!.ToDisplayString(), memberNames.ToArray());
            yield return parameter;
            containingType.Accept(new SyntaxWalker(paramSyntax, semanticModel, context, parameter));
        }
    }

    static T? GetAttributeProperty<T>(AttributeData attributeData, string propertyName)
    {
        // This is the attribute, check all of the named arguments
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            // Is this the Name argument?
            if (namedArgument.Key == propertyName && namedArgument.Value.Value is T n)
            {
                return n;
            }
        }

        return default;
    }

    static void GenerateFiles(IEnumerable<Parameter> parameters, SourceProductionContext context)
    {
        context.AddSource("FaustVX.PrimaryParameter.SG.g.cs", string.Concat(parameters.Select(static item => GetResource(item.Namespace, item.TypeName, item.FieldNames.Select(n => n.GenerateMember(item))))));
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
            parentsCount++; // keep track of how many layers deep we are
            sb
                .Append(new string(' ', 4 * parentsCount))
                .Append("partial ")
                .Append(parentClass.Keyword) // e.g. class/struct/record
                .Append(' ')
                .AppendLine(parentClass.Name) // e.g. Outer/Generic<T>
                .Append(new string(' ', 4 * parentsCount))
                .AppendLine("{");
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
        for (; parentsCount > 0; parentsCount--)
        {
            sb
                .Append(new string(' ', 4 * parentsCount))
                .AppendLine("}");
        }

        // Close the namespace, if we had one
        if (hasNamespace)
        {
            sb.AppendLine("}");
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