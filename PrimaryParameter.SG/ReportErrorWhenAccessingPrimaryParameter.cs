using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrimaryParameter.SG;

class ReportErrorWhenAccessingPrimaryParameter(ParameterSyntax paramSyntax, SemanticModel semanticModel, SourceProductionContext context, Parameter parameter) : CSharpSyntaxWalker
{
    private readonly ISymbol _paramSymbol = semanticModel.GetDeclaredSymbol(paramSyntax)!;

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var nodeSymbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (_paramSymbol.Equals(nodeSymbol, SymbolEqualityComparer.Default) && !parameter.FieldNames.Any(n => n.Name == _paramSymbol.Name))
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ErrorWhenAccessingPrimaryParameter, node.GetLocation(), nodeSymbol.Name, string.Join(" or ", parameter.FieldNames.Select(static n => $"'{n.Name}'"))));
    }
}
