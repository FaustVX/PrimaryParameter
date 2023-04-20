
namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateField(string Name) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $"private readonly {param.ParamType} {Name} = {param.ParamName};";
}

record GenerateProperty(string Name, bool WithInit) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $$"""private {{param.ParamType}} {{Name}} { get; {{(WithInit ? "init; " : "")}}} = {{param.ParamName}};""";
}
