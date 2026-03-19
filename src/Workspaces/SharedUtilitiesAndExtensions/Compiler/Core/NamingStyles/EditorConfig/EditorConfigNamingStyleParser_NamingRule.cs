// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.EditorConfig.Parsing;
using Microsoft.CodeAnalysis.EditorConfig.Parsing.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Text;

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

    internal static bool TryGetRule(
        Section section,
        string namingRuleTitle,
        ApplicableSymbolInfo applicableSymbolInfo,
        NamingScheme namingScheme,
        IReadOnlyDictionary<string, string> entries,
        IReadOnlyDictionary<string, TextLine> lines,
        [NotNullWhen(true)] out NamingStyleOption? rule,
        out int priority)
    {
        if (TryGetRuleProperties(
            namingRuleTitle,
            entries,
            out var severity,
            out var priorityComponent) &&
            severity.Value.HasValue)
        {
            // all rules must have a severity so we consider this its location
            var location = severity.GetSpan(lines);

            priority = priorityComponent.Value;
            rule = new NamingStyleOption(
                Section: section,
                RuleName: new(section, location, namingRuleTitle),
                ApplicableSymbolInfo: applicableSymbolInfo,
                NamingScheme: namingScheme,
                Severity: new(section, location, severity.Value.Value));

            return true;
        }

        rule = null;
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
