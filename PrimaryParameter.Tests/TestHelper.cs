using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using PrimaryParameter.SG;

namespace PrimaryParameter.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            options: new(LanguageVersion.Preview));

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
        syntaxTrees: [syntaxTree])
            .WithReferences(MetadataReference.CreateFromFile(typeof(Generator).Assembly.Location));

        // Create an instance of our EnumGenerator incremental source generator
        var generator = GeneratorExtensions.AsSourceGenerator(new Generator());

        // The GeneratorDriver is used to run our generator against a compilation
        var driver = CSharpGeneratorDriver.Create(
            [generator],
            parseOptions: new(LanguageVersion.Preview))
            .RunGenerators(compilation);

        var settings = new VerifySettings();
        settings.UseDirectory("Verify");
        settings.UseUniqueDirectory();
        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver, settings: settings);
    }
}