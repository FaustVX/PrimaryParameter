using System.Text;

namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateSummary(IGeneratedMember Generator, string Summary) : IGeneratedMember
{
    string IGeneratedMember.Name => Generator.Name;
    string IGeneratedMember.GenerateMember(Parameter param)
    {
        var sb = new StringBuilder()
        .AppendLine("/// <summary>");
        foreach (var line in Summary.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries).Select(static text => $"/// {text}"))
            sb.AppendLine(line);
        return sb.AppendLine("/// </summary>")
        .AppendLine(Generator.GenerateMember(param))
        .ToString();
    }
}

record GenerateField(string Name, bool IsReadonly, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "private";
    public static bool DefaultReadonly { get; internal set; } = true;
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonly ? " readonly " : " ")}{Type ?? param.ParamType} {Name} = {string.Format(AssignFormat, param.ParamName)};";
    public IGeneratedMember TryCreateSummary(string? Summary)
    {
        if (string.IsNullOrEmpty(Summary))
            return this;
        return new GenerateSummary(this, Summary!);
    }
}

record GenerateRefField(string Name, bool IsReadonlyRef, bool IsRefReadonly, string Scope) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "private";
    public static bool DefaultRefReadonly { get; internal set; } = true;
    public static bool DefaultReadonlyRef { get; internal set; } = true;
    public string GenerateMember(Parameter param)
        => $"{Scope}{(IsReadonlyRef ? " readonly " : " ")}ref{(IsRefReadonly ? " readonly " : " ")}{param.ParamType} {Name} = ref {param.ParamName};";
    public IGeneratedMember TryCreateSummary(string? Summary)
    {
        if (string.IsNullOrEmpty(Summary))
            return this;
        return new GenerateSummary(this, Summary!);
    }
}

record GenerateProperty(string Name, string Setter, string Scope, string AssignFormat, string? Type) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "public";
    public static string DefaultSetter { get; internal set; } = "init";
    public string GenerateMember(Parameter param)
        => $$"""{{Scope}} {{Type ?? param.ParamType}} {{Name}} { get; {{(!string.IsNullOrWhiteSpace(Setter) ? Setter + "; " : "")}}} = {{string.Format(AssignFormat, param.ParamName)}};""";
    public IGeneratedMember TryCreateSummary(string? Summary)
    {
        if (string.IsNullOrEmpty(Summary))
            return this;
        return new GenerateSummary(this, Summary!);
    }
}
