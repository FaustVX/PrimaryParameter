using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrimaryParameter.SG;

class SyntaxWalker(ParameterSyntax paramSyntax, SemanticModel semanticModel, SourceProductionContext context, Parameter parameter) : CSharpSyntaxWalker
{
    private readonly ISymbol _paramSymbol = semanticModel.GetDeclaredSymbol(paramSyntax)!;
    public static readonly DiagnosticDescriptor DiagnosticDescriptor = new(
        id: "PC01",
        title: "Accessing a Primary Parameter",
        messageFormat: "Can't access a primary parameter ('{0}') with a [Field] or [Property] attribute, use {1}",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var nodeSymbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (_paramSymbol.Equals(nodeSymbol, SymbolEqualityComparer.Default))
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, node.GetLocation(), nodeSymbol.Name, string.Join(" or ", parameter.FieldNames.Select(static n => $"'{n.Name}'"))));
    }
}
