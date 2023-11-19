
namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateField(string Name, bool IsReadonly, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "private";
    public static bool DefaultReadonly { get; internal set; } = true;
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonly ? " readonly " : " ")}{Type ?? param.ParamType} {Name} = {string.Format(AssignFormat, param.ParamName)};";
}

record GenerateRefField(string Name, bool IsReadonlyRef, bool IsRefReadonly, string Scope) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "private";
    public static bool DefaultRefReadonly { get; internal set; } = true;
    public static bool DefaultReadonlyRef { get; internal set; } = true;
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonlyRef ? " readonly " : " ")}ref{(IsRefReadonly ? " readonly " : " ")}{param.ParamType} {Name} = ref {param.ParamName};";
}

record GenerateProperty(string Name, string Setter, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "public";
    public static string DefaultSetter { get; internal set; } = "init";
    public string GenerateMember(Parameter param)
        => $$"""{{Scope}} {{Type ?? param.ParamType}} {{Name}} { get; {{(!string.IsNullOrWhiteSpace(Setter) ? Setter + "; " : "")}}} = {{string.Format(AssignFormat, param.ParamName)}};""";
}
