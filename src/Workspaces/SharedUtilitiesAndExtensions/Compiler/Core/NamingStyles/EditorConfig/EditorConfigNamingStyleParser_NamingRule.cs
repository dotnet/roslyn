// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    internal static bool TryGetRule(
        string namingRuleTitle,
        SymbolSpecification symbolSpec,
        NamingStyle namingStyle,
        IReadOnlyDictionary<string, string> entries,
        [NotNullWhen(true)] out NamingRule? namingRule,
        out int priority)
    {
        if (TryGetRuleProperties(
            namingRuleTitle,
            entries,
            out var severity,
            out var priorityComponent) &&
            severity.Value.HasValue)
        {
            priority = priorityComponent.Value;
            namingRule = new NamingRule(symbolSpec, namingStyle, severity.Value.Value);
            return true;
        }

        namingRule = null;
        priority = 0;
        return false;
    }

    private static bool TryGetRuleProperties(
        string name,
        IReadOnlyDictionary<string, string> entries,
        out Property<ReportDiagnostic?> severity,
        out Property<int> priority)
    {
        const string group = "dotnet_naming_rule";
        severity = GetProperty(entries, group, name, "severity", ParseEnforcementLevel, defaultValue: null);
        priority = GetProperty(entries, group, name, "priority", static s => int.TryParse(s, out var value) ? value : 0, 0);
        return true;
    }

    private static ReportDiagnostic? ParseEnforcementLevel(string ruleSeverity)
        => ruleSeverity switch
        {
            EditorConfigSeverityStrings.None => ReportDiagnostic.Suppress,
            EditorConfigSeverityStrings.Refactoring or EditorConfigSeverityStrings.Silent => ReportDiagnostic.Hidden,
            EditorConfigSeverityStrings.Suggestion => ReportDiagnostic.Info,
            EditorConfigSeverityStrings.Warning => ReportDiagnostic.Warn,
            EditorConfigSeverityStrings.Error => ReportDiagnostic.Error,
            _ => ReportDiagnostic.Hidden,
        };
}
