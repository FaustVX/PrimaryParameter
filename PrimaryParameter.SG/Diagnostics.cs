﻿using Microsoft.CodeAnalysis;

namespace PrimaryParameter.SG;

[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking")]
static class Diagnostics
{
    public static readonly DiagnosticDescriptor ErrorWhenAccessingPrimaryParameter = new(
        id: "PC01",
        title: "Accessing a Primary Parameter",
        messageFormat: "Can't access a primary parameter ('{0}') with a [Field] or [Property] attribute, use {1}",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WarningOnNonPrimaryParameter = new(
        id: "PC02",
        title: "Attribute generate nothing",
        messageFormat: "Use this attributes only on primary parameter",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor WarningOnUsedMember = new(
        id: "PC03",
        title: "Attribute generate nothing",
        messageFormat: "This member's name ('{0}') is already used",
        category: "tests",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
