using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace PrimaryParameter.SG.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PC01)), Shared]
public class PC01 : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(Diagnostics.ErrorWhenAccessingPrimaryParameter.Id);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if ((root?.FindNode(context.Span)) is null)
            return;
        foreach (var diagnostic in context.Diagnostics)
            foreach (var newName in diagnostic.Properties["fields"]!.Split(' '))
                context.RegisterCodeFix(CodeAction.Create($"Use {newName} instead", new Fixer(context.Document, newName, diagnostic, root).Fix, $"PC01{newName}{diagnostic.GetHashCode()}"), diagnostic);
    }

    class Fixer(Document document, string newName, Diagnostic diagnostic, SyntaxNode root)
    {
        // Based on https://denace.dev/fixing-mistakes-with-roslyn-code-fixes
        public Task<Document> Fix(CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // the document does not have a syntax tree - nothing to do
            if (root is null)
                return document;

            // find the token at the additional location we reported in the analyzer
            var token = (ArgumentSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var updatedToken = token.WithExpression(((IdentifierNameSyntax)token.Expression).WithIdentifier(SyntaxFactory.Identifier(newName)));
            var newRoot = root.ReplaceNode(token, updatedToken);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
