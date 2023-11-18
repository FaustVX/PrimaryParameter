using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace PrimaryParameter.SG.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAttribute)), Shared]
public class RemoveAttribute : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(Diagnostics.WarningOnNonPrimaryParameter.Id, Diagnostics.WarningOnUsedMember.Id, Diagnostics.ErrorWhenRefFieldInNonRefStruct.Id, Diagnostics.ErrorWhenRefFieldOnNonRefParam.Id);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if ((root?.FindNode(context.Span)) is null)
            return;
        foreach (var diagnostic in context.Diagnostics)
            context.RegisterCodeFix(CodeAction.Create("Remove Attribute", new Fixer(context.Document, diagnostic, root).Fix, $"{diagnostic.Id}_RemoveAttribute"), diagnostic);
    }

    class Fixer(Document document, Diagnostic diagnostic, SyntaxNode root)
    {
        // Based on https://denace.dev/fixing-mistakes-with-roslyn-code-fixes
        public Task<Document> Fix(CancellationToken cancellationToken)
        {
            // find the token at the additional location we reported in the analyzer
            var attributeSyntax = GetAttribute(root.FindNode(diagnostic.Location.SourceSpan));
            var attributeListSyntax = (AttributeListSyntax)attributeSyntax.Parent!;
            var parameterSyntax = (ParameterSyntax)attributeListSyntax.Parent!;

            var newRoot = root.ReplaceNode(parameterSyntax, ReplaceAttributeList(parameterSyntax, attributeSyntax));

            return Task.FromResult(document.WithSyntaxRoot(newRoot));

            static AttributeSyntax GetAttribute(SyntaxNode node)
            => node switch
            {
                AttributeSyntax s => s,
                _ => GetAttribute(node.Parent!)
            };
        }

        static ParameterSyntax ReplaceAttributeList(ParameterSyntax parameter, AttributeSyntax attribute)
        {
            parameter = parameter.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia)!;
            for (var i = 0; i < parameter.AttributeLists.Count; i++)
                if (parameter.AttributeLists[i].Attributes.Count == 0)
                    parameter = parameter.RemoveNode(parameter.AttributeLists[i], SyntaxRemoveOptions.KeepNoTrivia)!;
            return parameter;
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
