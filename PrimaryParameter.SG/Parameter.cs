
namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateField(string Name, bool IsReadonly, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonly ? " readonly " : " ")}{Type ?? param.ParamType} {Name} = {string.Format(AssignFormat, param.ParamName)};";
}

record GenerateRefField(string Name, bool IsReadonlyRef, bool IsRefReadonly, string Scope) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonlyRef ? " readonly " : " ")}ref{(IsRefReadonly ? " readonly " : " ")}{param.ParamType} {Name} = ref {param.ParamName};";
}

record GenerateProperty(string Name, bool WithInit, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => $$"""{{Scope}} {{Type ?? param.ParamType}} {{Name}} { get; {{(WithInit ? "init; " : "")}}} = {{string.Format(AssignFormat, param.ParamName)}};""";
}
