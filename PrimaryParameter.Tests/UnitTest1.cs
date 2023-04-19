using System.Runtime.CompilerServices;

namespace PrimaryParameter.Tests;

[TestClass]
public class GeneratorSnapshotTests : VerifyBase
{
    //[TestMethod]
    public Task GeneratesFieldAttributeCorrectly()
    {
        // The source code to test
        var source = "";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, this);
    }
    [TestMethod]
    public Task GeneratesFieldCorrectly()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field]int i)
            { }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source, this);
    }
}

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
        => VerifySourceGenerators.Initialize();
}
