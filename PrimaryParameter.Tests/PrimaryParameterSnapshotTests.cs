namespace PrimaryParameter.Tests;

[UsesVerify] // 👈 Adds hooks for Verify into XUnit
public class PrimaryParameterSnapshotTests
{
    [Fact]
    public Task GeneratesAttributesCorrectly()
    {
        // The source code to test
        var source = """
            public class C
            {}
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesFieldCorrectly()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field] int i)
            {}
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesPC01()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field] int i)
            {
                int M() => i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task CodeFixPC01()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field] int i)
            {
                int M() => i;
            }
            """;

        return TestHelper.Verify<SG.CodeFixes.PC01>(source, SG.Diagnostics.ErrorWhenAccessingPrimaryParameter);
    }

    [Fact]
    public Task DontGeneratesPC01UsingParameter()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field] int i)
            {
                int M(int i) => i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DontGeneratesPC01UsingField()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Field] int i)
            {
                int M() => _i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}
