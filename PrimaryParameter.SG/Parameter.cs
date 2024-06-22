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
    private static readonly char[] _newLineChars = ['\r', '\n'];
    string IGeneratedMember.Name => Generator.Name;
    string IGeneratedMember.GenerateMember(Parameter param) => new StringBuilder()
        .AppendLine("/// <summary>")
        .AppendLineRange(Summary.Split(_newLineChars, StringSplitOptions.RemoveEmptyEntries).Select(static text => $"/// {text}"))
        .AppendLine("/// </summary>")
        .AppendLine(Generator.GenerateMember(param))
        .ToString();
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

record GenerateProperty(string Name, string Setter, string Scope, string AssignFormat, string? Type, bool WithoutBackingStorage) : IGeneratedMember
{
    public static string DefaultScope { get; internal set; } = "public";
    public static string DefaultSetter { get; internal set; } = "init";
    public string GenerateMember(Parameter param)
        => $$"""{{Scope}} {{Type ?? param.ParamType}} {{Name}} { {{GenerateGet(param)}}{{GenerateSetter(param)}} }{{GenerateInitializer(param)}}""";

    private string GenerateGet(Parameter param)
        => WithoutBackingStorage ? $"""get => {string.Format(AssignFormat, param.ParamName)};"""
        : "get;";

    private string GenerateSetter(Parameter param)
        => string.IsNullOrWhiteSpace(Setter) ? ""
        : WithoutBackingStorage ? $""" {Setter} => {string.Format(AssignFormat, param.ParamName)} = value;"""
        : $" {Setter};";

    private string GenerateInitializer(Parameter param)
        => WithoutBackingStorage ? ""
        : $$""" = {{string.Format(AssignFormat, param.ParamName)}};""";

    public IGeneratedMember TryCreateSummary(string? Summary)
    {
        if (string.IsNullOrEmpty(Summary))
            return this;
        return new GenerateSummary(this, Summary!);
    }
}

file static class Ext
{
    public static StringBuilder AppendLineRange(this StringBuilder sb, IEnumerable<string> strings)
    {
        foreach (var item in strings)
            sb.AppendLine(item);
        return sb;
    }
}
