// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static partial class EditorConfigNamingStyleParser
{
    private static bool TryGetSerializableNamingRule(
        string namingRuleTitle,
        SymbolSpecification symbolSpec,
        NamingStyle namingStyle,
        IReadOnlyDictionary<string, string> conventionsDictionary,
        [NotNullWhen(true)] out SerializableNamingRule? serializableNamingRule)
    {
        if (!TryGetRuleSeverity(namingRuleTitle, conventionsDictionary, out var severity))
        {
            serializableNamingRule = null;
            return false;
        }

        serializableNamingRule = new SerializableNamingRule()
        {
            EnforcementLevel = severity,
            NamingStyleID = namingStyle.ID,
            SymbolSpecificationID = symbolSpec.ID
        };

        return true;
    }

    internal static bool TryGetRuleSeverity(
        string namingRuleName,
        IReadOnlyDictionary<string, (string value, TextLine? line)> conventionsDictionary,
        out (ReportDiagnostic severity, TextLine? line) value)
        => TryGetRuleSeverity(namingRuleName, conventionsDictionary, x => x.value, x => x.line, out value);

    private static bool TryGetRuleSeverity(
        string namingRuleName,
        IReadOnlyDictionary<string, string> conventionsDictionary,
        out ReportDiagnostic severity)
    {
        var result = TryGetRuleSeverity<string, object?>(
            namingRuleName,
            conventionsDictionary,
            x => x,
            x => null, // we don't have a tuple
            out var tuple);
        severity = tuple.severity;
        return result;
    }

    private static bool TryGetRuleSeverity<T, V>(
        string namingRuleName,
        IReadOnlyDictionary<string, T> conventionsDictionary,
        Func<T, string> valueSelector,
        Func<T, V> partSelector,
        out (ReportDiagnostic severity, V value) value)
    {
        if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out var result))
        {
            var severity = ParseEnforcementLevel(valueSelector(result) ?? string.Empty);
            value = (severity, partSelector(result));
            return true;
        }

        value = default;
        return false;
    }

    private static ReportDiagnostic ParseEnforcementLevel(string ruleSeverity)
    {
        switch (ruleSeverity)
        {
            case EditorConfigSeverityStrings.None:
                return ReportDiagnostic.Suppress;

            case EditorConfigSeverityStrings.Refactoring:
            case EditorConfigSeverityStrings.Silent:
                return ReportDiagnostic.Hidden;

            case EditorConfigSeverityStrings.Suggestion: return ReportDiagnostic.Info;
            case EditorConfigSeverityStrings.Warning: return ReportDiagnostic.Warn;
            case EditorConfigSeverityStrings.Error: return ReportDiagnostic.Error;
            default: return ReportDiagnostic.Hidden;
        }
    }
}
