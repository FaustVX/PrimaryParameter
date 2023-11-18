using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using PrimaryParameter.SG;

namespace PrimaryParameter.Tests;

public static class TestHelper
{
    public static Task Verify(string source)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        // Create references for assemblies we require
        // We could add multiple references if required
        IEnumerable<PortableExecutableReference> references = [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: references); // 👈 pass the references to the compilation

        // Create an instance of our EnumGenerator incremental source generator
        var generator = new Generator();

        // The GeneratorDriver is used to run our generator against a compilation
        var driver = CSharpGeneratorDriver.Create(generator)
            .RunGenerators(compilation);

        var settings = new VerifySettings();
        settings.UseDirectory("Verify");
        settings.UseUniqueDirectory();
        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver, settings: settings);
    }
}