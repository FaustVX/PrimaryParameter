using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrimaryParameter.SG;

class SyntaxWalker(ParameterSyntax parameter, SemanticModel semanticModel, SourceProductionContext context) : CSharpSyntaxWalker
{
    private readonly ISymbol _paramSymbol = semanticModel.GetDeclaredSymbol(parameter)!;
    public static readonly DiagnosticDescriptor DiagnosticDescriptor = new(
        id: "PC01",
        title: "Accessing a Primary Parameter",
        messageFormat: "Can't access a primary parameter ('{0}') with a [Field] attribute, use '_{0}'",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var nodeSymbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (_paramSymbol.Equals(nodeSymbol, SymbolEqualityComparer.Default))
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptor, node.GetLocation(), nodeSymbol.Name));
    }
}
