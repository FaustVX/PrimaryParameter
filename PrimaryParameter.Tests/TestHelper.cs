using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace PrimaryParameter.Tests;

public static class TestHelper
{
    public static Task Verify(string source, VerifyBase verify)
    {
        // Create references for assemblies we require
        // We could add multiple references if required
        IEnumerable<PortableExecutableReference> references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };

        // Create a Roslyn compilation for the syntax tree.
        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
            references: references);

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new SG.Generator());

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return verify
            .Verify(driver)
            .UseDirectory("Snapshots");
    }
}
