using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
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
#if DEBUG
        //if (!System.Diagnostics.Debugger.IsAttached)
        //    System.Diagnostics.Debugger.Launch();
#endif
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, Options);

        GenerateSourceFromAttribute(context, Field);
        GenerateSourceFromAttribute(context, RefField);
        GenerateSourceFromAttribute(context, Property);
        GenerateSourceFromAttribute(context, DoNotUse);
    }

    private void GenerateSourceFromAttribute(IncrementalGeneratorInitializationContext context, string attributeName, [CallerArgumentExpression(nameof(attributeName))] string type = null!)
    {
        context.RegisterPostInitializationOutput(_sources[type]);
        // Do a simple filter for parameter
        var paramDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(attributeName,
                predicate: IsSyntaxTargetForGeneration, // select params with attributes
                transform: GetSemanticTargetForGeneration) // select the param with the [Field], [RefField], [Property] or [DoNotUse] attribute
            .Where(static m => m is not null)!; // filter out attributed parameters that we don't care about

        // Combine the selected parameters with the `Compilation`
        var compilationAndParameters = context.CompilationProvider.Combine(paramDeclarations.Collect());

        // Generate the source using the compilation and parameters
        context.RegisterSourceOutput(compilationAndParameters, (spc, source) => Execute(source.Left, source.Right!, spc, type));
    }

    private static void Options(SourceProductionContext context, AnalyzerConfigOptionsProvider provider)
    {
        if (GetStringProperty(provider.GlobalOptions, $"Field_{nameof(GenerateField.DefaultScope)}", out var defaultFieldScope))
            GenerateField.DefaultScope = defaultFieldScope!;
        if (GetBoolProperty(provider.GlobalOptions, $"Field_{nameof(GenerateField.DefaultReadonly)}", out var defaultFieldReadonly))
            GenerateField.DefaultReadonly = defaultFieldReadonly;

        if (GetStringProperty(provider.GlobalOptions, $"RefField_{nameof(GenerateRefField.DefaultScope)}", out var defaultRefFieldScope))
            GenerateRefField.DefaultScope = defaultRefFieldScope!;
        if (GetBoolProperty(provider.GlobalOptions, $"RefField_{nameof(GenerateRefField.DefaultRefReadonly)}", out var defaultRefFieldRefReadonly))
            GenerateRefField.DefaultRefReadonly = defaultRefFieldRefReadonly;
        if (GetBoolProperty(provider.GlobalOptions, $"RefField_{nameof(GenerateRefField.DefaultReadonlyRef)}", out var defaultRefFieldReadonlyRef))
            GenerateRefField.DefaultReadonlyRef = defaultRefFieldReadonlyRef;

        if (GetStringProperty(provider.GlobalOptions, $"Property_{nameof(GenerateProperty.DefaultScope)}", out var defaultPropertyScope))
            GenerateProperty.DefaultScope = defaultPropertyScope!;
        if (GetStringProperty(provider.GlobalOptions, $"Property_{nameof(GenerateProperty.DefaultSetter)}", out var defaultPropertySetter))
            GenerateProperty.DefaultSetter = defaultPropertySetter!;

        static bool GetProperty(AnalyzerConfigOptions options, string name, out string? str)
            => options.TryGetValue($"build_property.{nameof(PrimaryParameter)}_{name}", out str) && str is not (null or "");

        static bool GetBoolProperty(AnalyzerConfigOptions options, string name, out bool value)
        {
            if (GetProperty(options, name, out var str))
            {
                value = "true".Equals(str, StringComparison.OrdinalIgnoreCase);
                return true;
            }
            value = false;
            return false;
        }

        static bool GetStringProperty(AnalyzerConfigOptions options, string name, out string? value)
        {
            if (GetProperty(options, name, out var str))
            {
                value = str;
                return true;
            }
            value = default;
            return false;
        }
    }

    private const string Field = "PrimaryParameter.SG.FieldAttribute";
    private const string RefField = "PrimaryParameter.SG.RefFieldAttribute";
    private const string Property = "PrimaryParameter.SG.PropertyAttribute";
    private const string DoNotUse = "PrimaryParameter.SG.DoNotUseAttribute";
    private readonly List<Diagnostic> _diagnostics = [];
    private static readonly IReadOnlyDictionary<string, Action<IncrementalGeneratorPostInitializationContext>> _sources = new Dictionary<string, Action<IncrementalGeneratorPostInitializationContext>>()
    {
        [nameof(Field)] = static ctx => ctx.AddSource("FieldAttribute.g.cs", """
                // <auto-generated/>
                using global::System;
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                    sealed class FieldAttribute : Attribute
                    {
                        public string Name { get; init; }
                        public string AssignFormat { get; init; }
                        public Type Type { get; init; }
                        public bool IsReadonly { get; init; }
                        public string Scope { get; init; }
                    }
                }

                """),
        [nameof(RefField)] = static ctx => ctx.AddSource("RefFieldAttribute.g.cs", """
                // <auto-generated/>
                using global::System;
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                    sealed class RefFieldAttribute : Attribute
                    {
                        public string Name { get; init; }
                        public string Scope { get; init; }
                        public bool IsReadonlyRef { get; init; }
                        public bool IsRefReadonly { get; init; }
                    }
                }

                """),
        [nameof(Property)] = static ctx => ctx.AddSource("PropertyAttribute.g.cs", """
                // <auto-generated/>
                using global::System;
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = true)]
                    sealed class PropertyAttribute : Attribute
                    {
                        public string Name { get; init; }
                        public string AssignFormat { get; init; }
                        public Type Type { get; init; }
                        public string Setter { get; init; }
                        public string Scope { get; init; }
                    }
                }

                """),
        [nameof(DoNotUse)] = static ctx => ctx.AddSource("DoNotUseAttribute.g.cs", """
                // <auto-generated/>
                using global::System;
                namespace PrimaryParameter.SG
                {
                    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
                    sealed class DoNotUseAttribute : Attribute
                    {
                        public bool AllowInMemberInit { get; init; }
                    }
                }

                """),
    };

    static bool IsSyntaxTargetForGeneration(SyntaxNode s, CancellationToken token)
        => s is ParameterSyntax { AttributeLists.Count: > 0 };

    ParameterSyntax? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context, CancellationToken token)
    {
        // we know the node is a ParameterSyntax thanks to IsSyntaxTargetForGeneration
        var parameterSyntax = (ParameterSyntax)context.TargetNode;

        // loop through all the attributes on the method
        foreach (var attributeListSyntax in parameterSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol { ContainingType: var attributeContainingTypeSymbol })
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                // Is the attribute the [Field], [RefField], [Property] or [DoNotUse] attribute?
                if (fullName is Field or RefField or Property or DoNotUse)
                {
                    if (parameterSyntax is not { Parent.Parent: ClassDeclarationSyntax or StructDeclarationSyntax })
                    {
                        _diagnostics.Add(Diagnostic.Create(Diagnostics.WarningOnNonPrimaryParameter, attributeSyntax.GetLocation()));
                        return null;
                    }
                    var hasDiagnostics = false;
                    if (fullName is RefField && !(parameterSyntax is { Parent.Parent: StructDeclarationSyntax { Modifiers: var typeModifiers } } && typeModifiers.Any(static mod => mod.IsKind(SyntaxKind.RefKeyword))))
                    {
                        _diagnostics.Add(Diagnostic.Create(Diagnostics.ErrorWhenRefFieldInNonRefStruct, attributeSyntax.GetLocation(), ((BaseTypeDeclarationSyntax)parameterSyntax.Parent.Parent).Identifier.Text));
                        hasDiagnostics = true;
                    }
                    if (fullName is RefField && !(parameterSyntax is { Modifiers: var paramModifiers } && paramModifiers.Any(static mod => mod.IsKind(SyntaxKind.RefKeyword))))
                    {
                        _diagnostics.Add(Diagnostic.Create(Diagnostics.ErrorWhenRefFieldOnNonRefParam, attributeSyntax.GetLocation(), parameterSyntax.Identifier.Text));
                        hasDiagnostics = true;
                    }

                    if (hasDiagnostics)
                        return null;
                    else
                        // return the parameter
                        return parameterSyntax;
                }
            }
        }

        // we didn't find the attribute we were looking for
        return null;
    }

    void Execute(Compilation compilation, ImmutableArray<ParameterSyntax> parameters, SourceProductionContext context, string type)
    {
        foreach (var diagnostic in _diagnostics)
        {
            context.ReportDiagnostic(diagnostic);
        }
        _diagnostics.Clear();
        if (parameters.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        // I'm not sure if this is actually necessary, but `[LoggerMessage]` does it, so seems like a good idea!
        var distinctParams = parameters.Distinct();

        // Convert each ParameterSyntax to a Parameter
        var paramsToGenerate = GetTypesToGenerate(compilation, distinctParams, context);

        GenerateFile(paramsToGenerate, context, type);
    }

    static IEnumerable<Parameter> GetTypesToGenerate(Compilation compilation, IEnumerable<ParameterSyntax> parameters, SourceProductionContext context)
    {
        if ((compilation.GetTypeByMetadataName(Field),
            compilation.GetTypeByMetadataName(RefField),
            compilation.GetTypeByMetadataName(Property),
            compilation.GetTypeByMetadataName(DoNotUse)) is not (INamedTypeSymbol fieldAttributeSymbol,
            INamedTypeSymbol refFieldAttributeSymbol,
            INamedTypeSymbol propertyAttributeSymbol,
            INamedTypeSymbol DoNotUseAttributeSymbol))
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
            if (semanticModel.GetDeclaredSymbol(paramSyntax) is not ISymbol)
            {
                // something went wrong, bail out
                continue;
            }

            var containingType = (BaseTypeDeclarationSyntax)((ParameterListSyntax)paramSyntax.Parent!).Parent!;

            var isReadonlyType = containingType.Modifiers.Any(static mod => mod.IsKind(SyntaxKind.ReadOnlyKeyword));

            var semanticType = semanticModel.GetDeclaredSymbol(containingType)!;

            var memberNames = new HashSet<IGeneratedMember>();

            var allowInMemberInit = true;

            foreach (var list in paramSyntax.AttributeLists)
                foreach (var attribute in list.Attributes)
                {
                    // stop if we're asked to
                    context.CancellationToken.ThrowIfCancellationRequested();

                    var operation = (IAttributeOperation)semanticModel.GetOperation(attribute)!;
                    var objectCreationOperation = (IObjectCreationOperation)operation.Operation!;
                    if (fieldAttributeSymbol.Equals(objectCreationOperation.Type, SymbolEqualityComparer.Default))
                    {
                        var name = GetAttributeProperty<string>(operation, "Name", out var nameLocation) ?? ("_" + paramSyntax.Identifier.Text);
                        nameLocation ??= attribute.GetLocation();
                        var format = GetAttributeProperty<string>(operation, "AssignFormat", out _) ?? "{0}";
                        var type = GetAttributePropertyTypeOf(operation, "Type", out _);
                        var isReadonly = isReadonlyType || GetAttributeProperty<bool>(operation, "IsReadonly", out _, defaultValue: GenerateField.DefaultReadonly);
                        var scope = GetAttributeProperty<string>(operation, "Scope", out _) ?? GenerateField.DefaultScope;
                        if (semanticType.MemberNames.Contains(name))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, effectiveSeverity: DiagnosticSeverity.Error, null, null, name));
                        else if (!memberNames.Add(new GenerateField(name, isReadonly, scope, format, type)))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, name));
                    }
                    else if (refFieldAttributeSymbol.Equals(objectCreationOperation.Type, SymbolEqualityComparer.Default))
                    {
                        var name = GetAttributeProperty<string>(operation, "Name", out var nameLocation) ?? ("_" + paramSyntax.Identifier.Text);
                        nameLocation ??= attribute.GetLocation();
                        var isReadonlyRef = isReadonlyType || GetAttributeProperty<bool>(operation, "IsReadonlyRef", out _, defaultValue: GenerateRefField.DefaultReadonlyRef);
                        var isRefReadonly = GetAttributeProperty<bool>(operation, "IsRefReadonly", out _, defaultValue: GenerateRefField.DefaultRefReadonly);
                        var scope = GetAttributeProperty<string>(operation, "Scope", out _) ?? GenerateRefField.DefaultScope;
                        if (semanticType.MemberNames.Contains(name))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, effectiveSeverity: DiagnosticSeverity.Error, null, null, name));
                        else if (!memberNames.Add(new GenerateRefField(name, isReadonlyRef, isRefReadonly, scope)))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, name));
                    }
                    else if (propertyAttributeSymbol.Equals(objectCreationOperation.Type, SymbolEqualityComparer.Default))
                    {
                        var name = GetAttributeProperty<string>(operation, "Name", out var nameLocation) ?? (char.ToUpper(paramSyntax.Identifier.Text[0]) + paramSyntax.Identifier.Text.Substring(1));
                        nameLocation ??= attribute.GetLocation();
                        var format = GetAttributeProperty<string>(operation, "AssignFormat", out _) ?? "{0}";
                        var type = GetAttributePropertyTypeOf(operation, "Type", out _);
                        var setter = GetAttributeProperty<string>(operation, "Setter", out _) ?? GenerateProperty.DefaultSetter;
                        var scope = GetAttributeProperty<string>(operation, "Scope", out _) ?? GenerateProperty.DefaultScope;
                        if (semanticType.MemberNames.Contains(name))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, effectiveSeverity: DiagnosticSeverity.Error, null, null, name));
                        else if (!memberNames.Add(new GenerateProperty(name, setter, scope, format, type)))
                            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.WarningOnUsedMember, nameLocation, name));
                    }
                    else if (DoNotUseAttributeSymbol.Equals(objectCreationOperation.Type, SymbolEqualityComparer.Default))
                        allowInMemberInit &= GetAttributeProperty<bool>(operation, "AllowInMemberInit", out _, defaultValue: true);
                }
            var parameter = new Parameter(GetNamespace(containingType), ParentClass.GetParentClasses(containingType)!, paramSyntax.Identifier.Text, semanticModel.GetTypeInfo(paramSyntax.Type!).Type!.ToDisplayString(), [.. memberNames]);
            yield return parameter;
            containingType.Accept(new ReportErrorWhenAccessingPrimaryParameter(paramSyntax, semanticModel, context, parameter, allowInMemberInit));
        }
    }

    static T? GetAttributeProperty<T>(IAttributeOperation attributeData, string propertyName, out Location? location)
    {
        // This is the attribute, check all of the named arguments
        var objectCreation = (IObjectCreationOperation)attributeData.Operation;
#pragma warning disable IDE0220 // Add explicit cast
        foreach (IAssignmentOperation namedArgument in objectCreation.Initializer?.Initializers ?? Enumerable.Empty<IOperation>())
#pragma warning restore IDE0220 // Add explicit cast
        {
            // Is this the Name argument?
            if (((IPropertyReferenceOperation)namedArgument.Target).Property.Name == propertyName && namedArgument.Value.ConstantValue is { HasValue: true, Value: T n })
            {
                location = namedArgument.Value.Syntax.GetLocation();
                return n;
            }
        }

        location = null;
        return default;
    }

    static T? GetAttributeProperty<T>(IAttributeOperation attributeData, string propertyName, out Location? location, T? defaultValue)
    {
        // This is the attribute, check all of the named arguments
        var objectCreation = (IObjectCreationOperation)attributeData.Operation;
#pragma warning disable IDE0220 // Add explicit cast
        foreach (IAssignmentOperation namedArgument in objectCreation.Initializer?.Initializers ?? Enumerable.Empty<IOperation>())
#pragma warning restore IDE0220 // Add explicit cast
        {
            // Is this the Name argument?
            if (((IPropertyReferenceOperation)namedArgument.Target).Property.Name == propertyName && namedArgument.Value.ConstantValue is { HasValue: true, Value: T n })
            {
                location = namedArgument.Value.Syntax.GetLocation();
                return n;
            }
        }

        location = null;
        return defaultValue;
    }

    static string? GetAttributePropertyTypeOf(IAttributeOperation attributeData, string propertyName, out Location? location)
    {
        // This is the attribute, check all of the named arguments
        var objectCreation = (IObjectCreationOperation)attributeData.Operation;
#pragma warning disable IDE0220 // Add explicit cast
        foreach (IAssignmentOperation namedArgument in objectCreation.Initializer?.Initializers ?? Enumerable.Empty<IOperation>())
#pragma warning restore IDE0220 // Add explicit cast
        {
            // Is this the Name argument?
            if (((IPropertyReferenceOperation)namedArgument.Target).Property.Name == propertyName && namedArgument.Value is ITypeOfOperation { TypeOperand: var type })
            {
                location = namedArgument.Value.Syntax.GetLocation();
                return type.ToDisplayString();
            }
        }

        location = null;
        return default;
    }

    static void GenerateFile(IEnumerable<Parameter> parameters, SourceProductionContext context, string type)
    {
        if (parameters.ToArray() is not { Length: > 0 } list || type == nameof(DoNotUse))
            return;
        context.AddSource($"FaustVX.PrimaryParameter.SG.{type}.g.cs", string.Concat(list.Where(Where).Select(Select)));

        static bool Where(Parameter parameter)
            => parameter.FieldNames.Length > 0;

        static string Select(Parameter parameter)
            => GetResource(parameter.Namespace, parameter.TypeName, parameter.FieldNames.Select(n => n.GenerateMember(parameter)));
    }

    static string GetResource(string nameSpace, ParentClass? parentClass, IEnumerable<string> inner)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        var parentsCount = 0;

        // If we don't have a namespace, generate the code in the "default"
        // namespace, either global:: or a different <RootNamespace>
        var hasNamespace = !string.IsNullOrEmpty(nameSpace);
        if (hasNamespace)
        {
            // We could use a file-scoped namespace here which would be a little simpler,
            // but that requires C# 10, which might not be available.
            // Depends what you want to support!
            sb
                .Append("namespace ")
                .AppendLine(nameSpace)
                .AppendLine("{");
            parentsCount++;
        }

        // Loop through the full parent type hierarchy, starting with the outermost
        while (parentClass is not null)
        {
            sb
                .Append(new string(' ', 4 * parentsCount))
                .Append("partial ")
                .Append(parentClass.Keyword) // e.g. class/struct/record
                .Append(' ')
                .AppendLine(parentClass.Name) // e.g. Outer/Generic<T>
                .Append(new string(' ', 4 * parentsCount))
                .AppendLine("{");
            parentClass = parentClass.Child; // repeat with the next child
            parentsCount++; // keep track of how many layers deep we are
        }

        foreach (var item in inner)
        {
            // Write the actual target generation code here
            sb.Append(new string(' ', 4 * parentsCount));
            sb.AppendLine(item);
        }

        // We need to "close" each of the parent types, so write
        // the required number of '}'
        for (; parentsCount > 0; parentsCount--)
        {
            sb
                .Append(new string(' ', 4 * (parentsCount - 1)))
                .AppendLine("}");
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
