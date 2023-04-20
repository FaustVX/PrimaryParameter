using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PrimaryParameter.SG;

record ParentClass(string Keyword, string Name, string Constraints, ParentClass? Child)
{
    private IEnumerable<ParentClass> Iterate()
    {
        foreach (var child in Child?.Iterate() ?? Enumerable.Empty<ParentClass>())
            yield return child;
        yield return this;
    }
    public string ConcatTypeName()
        => string.Join(".", Iterate().Select(static c => c.Name));

    public static ParentClass? GetParentClasses(BaseTypeDeclarationSyntax typeSyntax)
    {
        // Try and get the parent syntax. If it isn't a type like class/struct, this will be null
        var parentSyntax = typeSyntax.Parent as TypeDeclarationSyntax;
        var parentClassInfo = CreateParentClass((TypeDeclarationSyntax)typeSyntax, null);

        // Keep looping while we're in a supported nested type
        while (parentSyntax != null && parentSyntax.Kind() is SyntaxKind.ClassDeclaration or SyntaxKind.StructDeclaration or SyntaxKind.RecordDeclaration or SyntaxKind.RecordStructDeclaration)
        {
            // Record the parent type keyword (class/struct etc), name, and constraints
            parentClassInfo = CreateParentClass(parentSyntax, parentClassInfo);

            // Move to the next outer type
            parentSyntax = (parentSyntax.Parent as TypeDeclarationSyntax);
        }

        // return a link to the outermost parent type
        return parentClassInfo;

        static ParentClass CreateParentClass(TypeDeclarationSyntax typeSyntax, ParentClass? parent)
            => new(
                Keyword: typeSyntax.Keyword.ValueText + (typeSyntax is RecordDeclarationSyntax { ClassOrStructKeyword.Value: string cors } ? " " + cors : ""),
                Name: typeSyntax.Identifier.ToString() + typeSyntax.TypeParameterList,
                Constraints: typeSyntax.ConstraintClauses.ToString(),
                Child: parent);
    }
}
