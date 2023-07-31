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
            // find the token at the additional location we reported in the analyzer
            return root.FindNode(diagnostic.Location.SourceSpan) switch
            {
                ArgumentSyntax { Expression: IdentifierNameSyntax token } => Task.FromResult(ReplaceIdentifier(document, newName, root, token)),
                IdentifierNameSyntax token => Task.FromResult(ReplaceIdentifier(document, newName, root, token)),
                _ => Task.FromResult(document),
            };

            static Document ReplaceIdentifier(Document document, string newName, SyntaxNode root, IdentifierNameSyntax token)
            {
                var updatedToken = token.WithIdentifier(SyntaxFactory.Identifier(newName));
                var newRoot = root.ReplaceNode(token, updatedToken);

                return document.WithSyntaxRoot(newRoot);
            }
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
