// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            IReadOnlyDictionary<string, object> conventionsDictionary,
            out SerializableNamingRule serializableNamingRule)
        {
            if(!TryGetRuleSeverity(namingRuleTitle, conventionsDictionary, out var severity))
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
            IReadOnlyDictionary<string, object> conventionsDictionary,
            out ReportDiagnostic severity)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out object result))
            {
                severity = ParseEnforcementLevel(result as string ?? string.Empty);
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
