using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Immutable;

namespace PrimaryParameter.SG;

class ReportErrorWhenAccessingPrimaryParameter(ParameterSyntax paramSyntax, SemanticModel semanticModel, SourceProductionContext context, Parameter parameter, bool allowInMemberInit) : CSharpSyntaxWalker
{
    private readonly ParameterSyntax _parameterSyntax = paramSyntax;
    private readonly ISymbol _paramSymbol = semanticModel.GetDeclaredSymbol(paramSyntax)!;

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        var nodeSymbol = semanticModel.GetSymbolInfo(node).Symbol;
        if (_paramSymbol.Equals(nodeSymbol, SymbolEqualityComparer.Default))
        {
            if (allowInMemberInit && Contains(node, static node => node switch
            {
                FieldDeclarationSyntax => true,
                PrimaryConstructorBaseTypeSyntax => true,
                PropertyDeclarationSyntax { Initializer: not null, ExpressionBody: null } => true,
                _ => false,
            }))
                return;
            if (!parameter.FieldNames.Any(n => n.Name == _paramSymbol.Name) && !IsIOperation<INameOfOperation>(node) && !IsInParameterListSyntax((ParameterListSyntax)_parameterSyntax.Parent!, node))
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ErrorWhenAccessingPrimaryParameter, node.GetLocation(), ImmutableDictionary.Create<string, string?>().Add("fields", string.Join(" ", parameter.FieldNames.Select(static n => n.Name))), nodeSymbol.Name, string.Join(" or ", parameter.FieldNames.Select(static n => $"'{n.Name}'"))));
        }
    }

    private bool IsInParameterListSyntax(ParameterListSyntax parameterList, SyntaxNode node)
        => parameterList.IsEquivalentTo(node) || (node.Parent is not null && IsInParameterListSyntax(parameterList, node.Parent));

    private bool IsIOperation<TOp>(SyntaxNode node)
        where TOp : IOperation
        => semanticModel.GetOperation(node) is TOp || (node.Parent is not null && IsIOperation<TOp>(node.Parent));


    private bool Contains(SyntaxNode node, Func<SyntaxNode, bool> contains)
    {
        if (node == null)
            return false;
        if (contains(node))
            return true;
        if (node.Parent is not null)
            return Contains(node.Parent, contains);
        return false;
    }
}
