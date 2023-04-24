
using Microsoft.CodeAnalysis;

namespace PrimaryParameter.SG;

record Parameter(string Namespace, ParentClass TypeName, string ParamName, string ParamType, IGeneratedMember[] FieldNames);

interface IGeneratedMember
{
    string Name { get; }
    string GenerateMember(Parameter param);
}

record GenerateField(string Name, Location? NameLocation) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => NameLocation switch
        {
            null => $"private readonly {param.ParamType} {Name} = {param.ParamName};",
            _ => $"""
            private readonly {param.ParamType}
            {NameLocation.ToLineDirective()}
            {Name}
            #line hidden
            = {param.ParamName};
            """,
        };
}

record GenerateProperty(string Name, Location? NameLocation, bool WithInit, string Scope, Location? ScopeLocation) : IGeneratedMember
{
    public string GenerateMember(Parameter param)
        => (NameLocation, ScopeLocation) switch
        {
            (null, null) => $$"""{{Scope}} {{param.ParamType}} {{Name}} { get; {{(WithInit ? "init; " : "")}}} = {{param.ParamName}};""",
            (_, null) => $$"""
            {{Scope}} {{param.ParamType}}
            {{NameLocation.ToLineDirective()}}
            {{Name}}
            #line hidden
            { get; {{(WithInit ? "init; " : "")}}} = {{param.ParamName}};
            """,
            (null, _) => $$"""
            {{ScopeLocation.ToLineDirective()}}
            {{Scope}}
            #line hidden
            {{param.ParamType}} {{Name}} { get; {{(WithInit ? "init; " : "")}}} = {{param.ParamName}};
            """,
            (_, _) => $$"""
            {{ScopeLocation.ToLineDirective()}}
            {{Scope}}
            #line hidden
            {{param.ParamType}}
            {{NameLocation.ToLineDirective()}}
            {{Name}}
            #line hidden
            { get; {{(WithInit ? "init; " : "")}}} = {{param.ParamName}};
            """,
        };
}

file static class LocationExtension
{
    public static string ToLineDirective(this Location location)
        => location.GetLineSpan() switch
        {
            { Span: { Start.Line: var sl, Start.Character: var sc, End.Line: var el, End.Character: var ec }, Path: var path } => $"""#line ({sl + 1},{sc + 1})-({el + 1},{ec + 1}) "{path}" """,
        };
}
