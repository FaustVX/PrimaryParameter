using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace PrimaryParameter.SG.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CS0282)), Shared]
public class CS0282 : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("CS0282");

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if ((root?.FindNode(context.Span)) is null)
            return;
        foreach (var diagnostic in context.Diagnostics)
            context.RegisterCodeFix(CodeAction.Create("Add StructLayout", new Fixer(context.Document, diagnostic, root).Fix, $"{diagnostic.Id}_StructLayout"), diagnostic);
    }

    class Fixer(Document document, Diagnostic diagnostic, SyntaxNode root)
    {
        // Based on https://denace.dev/fixing-mistakes-with-roslyn-code-fixes
        public Task<Document> Fix(CancellationToken cancellationToken)
        {
            // find the token at the additional location we reported in the analyzer
            var baseTypeDeclaration = (TypeDeclarationSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var layoutKind = SyntaxFactory.AttributeArgument(SyntaxFactory.IdentifierName("System.Runtime.InteropServices.LayoutKind.Auto"));
            var structLayoutSyntax = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("System.Runtime.InteropServices.StructLayout"), SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(layoutKind)));
            var updatedToken = baseTypeDeclaration.AddAttributeLists(SyntaxFactory.AttributeList(new SeparatedSyntaxList<AttributeSyntax>().Add(structLayoutSyntax)));
            var newRoot = root.ReplaceNode(baseTypeDeclaration, updatedToken);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
