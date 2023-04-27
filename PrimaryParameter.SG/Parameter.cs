
namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateField(string Name, string AssignFormat, string? Type) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $"private readonly {Type ?? param.ParamType} {Name} = {string.Format(AssignFormat, param.ParamName)};";
}

record GenerateProperty(string Name, bool WithInit, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $$"""{{Scope}} {{Type ?? param.ParamType}} {{Name}} { get; {{(WithInit ? "init; " : "")}}} = {{string.Format(AssignFormat, param.ParamName)}};""";
}
