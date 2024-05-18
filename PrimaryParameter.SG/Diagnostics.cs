using Microsoft.CodeAnalysis;

namespace PrimaryParameter.SG;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
static class Diagnostics
{
    /// <summary>
    /// PC01
    /// </summary>
    public static readonly DiagnosticDescriptor ErrorWhenAccessingPrimaryParameter = new(
        id: "PC01",
        title: "Accessing a Primary Parameter",
        messageFormat: "Can't access a primary parameter ('{0}') with a [Field], [RefField], [Property] or [DoNotUse] attribute, use {1}",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PC02
    /// </summary>
    public static readonly DiagnosticDescriptor WarningOnNonPrimaryParameter = new(
        id: "PC02",
        title: "Attribute generate nothing",
        messageFormat: "Use this attributes only on primary parameter",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// PC03
    /// </summary>
    public static readonly DiagnosticDescriptor WarningOnUsedMember = new(
        id: "PC03",
        title: "Attribute generate nothing",
        messageFormat: "This member's name ('{0}') is already used",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// PC04
    /// </summary>
    public static readonly DiagnosticDescriptor ErrorWhenRefFieldInNonRefStruct = new(
        id: "PC04",
        title: "RefField in non ref struct",
        messageFormat: "Can't apply [RefField] in non ref struct '{0}'",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// PC05
    /// </summary>
    public static readonly DiagnosticDescriptor ErrorWhenRefFieldOnNonRefParam = new(
        id: "PC05",
        title: "RefField on non ref parameter",
        messageFormat: "Can't apply [RefField] on non ref parameter '{0}'",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
