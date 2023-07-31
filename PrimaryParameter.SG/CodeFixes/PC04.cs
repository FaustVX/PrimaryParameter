using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;

namespace PrimaryParameter.SG.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PC04)), Shared]
public class PC04 : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(Diagnostics.ErrorWhenRefFieldInNonRefStruct.Id);

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
#if DEBUG
        //if (!System.Diagnostics.Debugger.IsAttached)
        //    System.Diagnostics.Debugger.Launch();
#endif
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
        if ((root?.FindNode(context.Span)) is not { } id)
            return;
        foreach (var diagnostic in context.Diagnostics)
            context.RegisterCodeFix(CodeAction.Create($"Make ref struct", new Fixer(context.Document, diagnostic).Fix, $"PC04{diagnostic.GetHashCode()}"), diagnostic);
    }

    class Fixer(Document document, Diagnostic diagnostic)
    {
        // Based on https://denace.dev/fixing-mistakes-with-roslyn-code-fixes
        public async Task<Document> Fix(CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            // the document does not have a syntax tree - nothing to do
            if (root is null)
                return document;

            // find the token at the additional location we reported in the analyzer
            var attribute = (AttributeSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var attributeList = (AttributeListSyntax)attribute.Parent!;
            var parameter = (ParameterSyntax)attributeList.Parent!;
            var parameterList = (ParameterListSyntax)parameter.Parent!;
            var baseTypeDeclaration = (StructDeclarationSyntax)parameterList.Parent!;
            var partialModifierIndex = baseTypeDeclaration.Modifiers.IndexOf(SyntaxKind.PartialKeyword);
            if (partialModifierIndex == -1)
                return document;
            var updatedToken = baseTypeDeclaration.WithModifiers(baseTypeDeclaration.Modifiers.RemoveAt(partialModifierIndex).Add(SyntaxFactory.Token(SyntaxKind.RefKeyword).WithTriviaFrom(baseTypeDeclaration.Modifiers[^1])).Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTriviaFrom(baseTypeDeclaration.Modifiers[^1])));
            var newRoot = root.ReplaceNode(baseTypeDeclaration, updatedToken);

            return document.WithSyntaxRoot(newRoot);
        }
    }

    public sealed override FixAllProvider? GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;
}
