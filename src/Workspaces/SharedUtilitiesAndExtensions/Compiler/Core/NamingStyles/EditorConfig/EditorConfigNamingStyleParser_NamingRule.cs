﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static partial class EditorConfigNamingStyleParser
    {
        private static bool TryGetSerializableNamingRule(
            string namingRuleTitle,
            SymbolSpecification symbolSpec,
            NamingStyle namingStyle,
            IReadOnlyDictionary<string, string> conventionsDictionary,
            out SerializableNamingRule serializableNamingRule)
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

        private static bool TryGetRuleSeverity(
            string namingRuleName,
            IReadOnlyDictionary<string, string> conventionsDictionary,
            out ReportDiagnostic severity)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out var result))
            {
                severity = ParseEnforcementLevel(result ?? string.Empty);
                return true;
            }

            severity = default;
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
}
