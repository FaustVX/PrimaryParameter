using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using PrimaryParameter.SG;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace PrimaryParameter.Tests;

internal static class TestHelper
{
    private const string DefaultProjectName = "ProjectUnderTest.csproj";
    private const string DefaultDocumentName = "SourceUnderTest.cs";

    /// <summary>
    /// These are the common references to be added to the test solution. They are sourced from the actual references loaded into this project.
    /// </summary>
    private static readonly MetadataReference[] _commonReferences =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    ];

    private static readonly VerifySettings _settings = new();

    static TestHelper()
    {
        _settings.UseDirectory("Verify");
        _settings.UseUniqueDirectory();
    }

    public static Task<ImmutableArray<Diagnostic>> Verify(string source, [CallerFilePath] string filePath = null!)
    {
        var diagnostics = RunGenerator(source, out var driver);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver, settings: _settings, filePath)
            .ToTask()
            .ContinueWith(_ => diagnostics);
    }

    // Based on https://denace.dev/testing-roslyn-analyzers-and-code-fixes
    public static async Task Verify<TCodeFixProvider>(string source, DiagnosticDescriptor descriptor, [CallerFilePath] string filePath = null!)
    where TCodeFixProvider : CodeFixProvider, new()
    {
        // Pass the source code to our helper and snapshot test the output
        var diagnostics = RunGenerator(source, out _);
        var ws = CreateWorkspace(SourceText.From(source));
        var originalDocument = ws.GetProjectUnderTest().GetDocumentUnderTest();
        foreach (var diagnostic in diagnostics)
            if (diagnostic.Id == descriptor.Id)
            {
                var i = 1;
                var solutions = ApplyCodeFix<TCodeFixProvider>(originalDocument, diagnostic);
                await foreach (var solution in solutions)
                {
                    var modifiedDocument = solution.GetProjectUnderTest().GetDocumentUnderTest();
                    await Verify((await modifiedDocument.GetSyntaxTreeAsync()) ?? throw new(), $"{diagnostic.Id}-{i++}", filePath);
                }
            }
    }

    private static ImmutableArray<Diagnostic> RunGenerator(string source, out GeneratorDriver driver)
    {
        // Parse the provided string into a C# syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: "Tests",
            syntaxTrees: [syntaxTree],
            references: _commonReferences); // 👈 pass the references to the compilation

        // Create an instance of our EnumGenerator incremental source generator
        var generator = new Generator();

        // The GeneratorDriver is used to run our generator against a compilation
        driver = CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);
        return diagnostics;
    }

    private static Task Verify(SyntaxTree syntaxTree, string parameters, [CallerFilePath] string filePath = null!)
    {
        var settings = new VerifySettings(_settings);
        settings.UseTextForParameters(parameters);
        return Verifier.Verify(syntaxTree.GetRoot().ToFullString(), settings: settings, filePath);
    }

    /// <summary>
    /// Creates a temporary workspace using the <see cref="DefaultProjectName"/> as the project name
    /// and <see cref="DefaultDocumentName"/> containing the <paramref name="source"/> as a single existing document.
    /// <see cref="_commonReferences"/> are the only external references added to the project.
    /// </summary>
    private static AdhocWorkspace CreateWorkspace(this SourceText source)
    {
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId, DefaultDocumentName);

        var sourceTextLoader = TextLoader.From(TextAndVersion.Create(source, VersionStamp.Create()));
        var document = DocumentInfo.Create(documentId, DefaultDocumentName)
                                   .WithTextLoader(sourceTextLoader);
        var project = ProjectInfo.Create(id: projectId,
                                     version: VersionStamp.Create(),
                                     name: DefaultProjectName,
                                     assemblyName: DefaultProjectName,
                                     language: LanguageNames.CSharp)
                                 .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                 .WithDocuments([document])
                                 .WithMetadataReferences(_commonReferences);

        var workspace = new AdhocWorkspace();
        var updatedSolution = workspace.CurrentSolution;
        updatedSolution = updatedSolution.AddProject(project);

        workspace.TryApplyChanges(updatedSolution);

        return workspace;
    }

    /// <summary>
    /// Gets the <see cref="DefaultProjectName"/> project
    /// </summary>
    private static Project GetProjectUnderTest(this AdhocWorkspace workspace)
        => GetProjectUnderTest(workspace.CurrentSolution);

    /// <summary>
    /// Gets the <see cref="DefaultProjectName"/> project
    /// </summary>
    private static Project GetProjectUnderTest(this Solution solution)
        => solution.Projects.First(x => x.Name == DefaultProjectName);

    /// <summary>
    /// Gets the <see cref="DefaultDocumentName"/> document
    /// </summary>
    private static Document GetDocumentUnderTest(this Project project)
        => project.Documents.First(x => x.Name == DefaultDocumentName);

    private static async IAsyncEnumerable<Solution> ApplyCodeFix<TCodeFixProvider>(Document document, Diagnostic singleDiagnostic)
    where TCodeFixProvider : CodeFixProvider, new()
    {
        var codeFixProvider = new TCodeFixProvider();
        List<CodeAction> actions = [];
        var context = new CodeFixContext(document, singleDiagnostic,
            (a, _) => actions.Add(a),
            CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context);
        foreach (var codeAction in actions)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            if (operations.IsDefaultOrEmpty)
            {
                continue;
            }

            var changedSolution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
            yield return changedSolution;
        }
    }
}
