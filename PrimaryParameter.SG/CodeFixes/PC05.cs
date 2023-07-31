using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace PrimaryParameter.SG.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PC05)), Shared]
public class PC05 : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(Diagnostics.ErrorWhenRefFieldOnNonRefParam.Id);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if ((root?.FindNode(context.Span)) is null)
            return;
        foreach (var diagnostic in context.Diagnostics)
            context.RegisterCodeFix(CodeAction.Create("Make ref parameter", new Fixer(context.Document, diagnostic, root).Fix, $"PC05{diagnostic.GetHashCode()}"), diagnostic);
    }

    class Fixer(Document document, Diagnostic diagnostic, SyntaxNode root)
    {
        // Based on https://denace.dev/fixing-mistakes-with-roslyn-code-fixes
        public async Task<Document> Fix(CancellationToken cancellationToken)
        {
            // find the token at the additional location we reported in the analyzer
            var attribute = (AttributeSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var attributeList = (AttributeListSyntax)attribute.Parent!;
            var parameter = (ParameterSyntax)attributeList.Parent!;
            var updatedToken = parameter.AddModifiers(SyntaxFactory.Token(SyntaxKind.RefKeyword));
            var newRoot = root.ReplaceNode(parameter, updatedToken);

            return document.WithSyntaxRoot(newRoot);
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
