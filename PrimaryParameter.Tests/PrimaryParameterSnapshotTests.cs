namespace PrimaryParameter.Tests;

public class PrimaryParameterSnapshotTests
{
    [Fact]
    public Task GeneratesAttributesCorrectly()
    {
        // The source code to test
        var source = """
            public class C;
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
            public partial class C([Field] int i);
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

    [Fact]
    public Task DefaultPropertySetter_Set()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Property] int i);
            """;

        SG.GenerateProperty.DefaultSetter = "set";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DefaultPropertySetter_Init()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Property] int i);
            """;

        SG.GenerateProperty.DefaultSetter = "init";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DefaultPropertySetter_None()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public partial class C([Property] int i);
            """;

        SG.GenerateProperty.DefaultSetter = "";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratesPC01WithDoNotUse()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse] int i)
            {
                int M() => i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DontGeneratesPC01WithDoNotUseOnMember_Simple()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse(AllowInMemberInit = true)] int i)
            {
                int M = i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DontGeneratesPC01WithDoNotUseOnMember_Complex()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse(AllowInMemberInit = true)] int i)
            {
                string L = i.ToString();
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DontGeneratesPC01WithDoNotUseOnPropertyInitializer()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse(AllowInMemberInit = true)] int i)
            {
                string L { get; } = i.ToString();
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoGeneratesPC01WithDoNotUseOnPropertyBody()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse] int i)
            {
                int M => i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoGeneratesPC01WithDoNotUseOnPropertyGet()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse] int i)
            {
                int M
                {
                    get => i;
                }
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DoGeneratesPC01WithDoNotUseOnMember()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse(AllowInMemberInit = false)] int i)
            {
                int M = i;
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DontNeedPartialModifierOnType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([DoNotUse] int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GenerateSingleLineDocumentation()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([Property(Summary = "Documentation")] int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GenerateMultiLineDocumentation()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class C([Property(Summary = "Documentation\nNew Line")] int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task DonterrorOnBaseCall()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B(int i);
            public class D([DoNotUse]int i) : B(i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptArrayImplicitly()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field]object[] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptArrayAsType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field(Type = typeof(object[]))]object[] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptMultidimensionalArrayAsType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field(Type = typeof(object[,]))]object[,] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptMultidimensionalArrayAsType_Generic()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B<T>([Field(Type = typeof(T[,]))]T[,] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptMultidimensionalArrayAsType_WithoutSpecifyingType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field]object[,] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptArrayOfArrayAsType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field(Type = typeof(object[][]))]object[][] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task AcceptMultidimensionnalArrayOfArrayAsType()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Field]object[][,] i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratePropertyWithoutBackingStorageGetOnly()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Property(WithoutBackingStorage = true, Setter = null)]int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratePropertyWithoutBackingStorageGetSet()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Property(WithoutBackingStorage = true, Setter = "set")]int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratePropertyWithoutBackingStorageGetInit()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Property(WithoutBackingStorage = true, Setter = "init")]int i);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratePropertyWithoutBackingStorageGetSetWithAssignFormat()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Property(WithoutBackingStorage = true, Setter = "set", Type = typeof(char), AssignFormat = "{0}[0]")]char[] c);
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task GeneratePropertyWithPartialModifier()
    {
        // The source code to test
        var source = """
            using PrimaryParameter.SG;
            public class B([Property]int i)
            {
                public partial int I { get => field * 2; init; }
            }
            """;

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}
