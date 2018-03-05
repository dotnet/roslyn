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
            out DiagnosticSeverity severity)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out object result))
            {
                severity = ParseEnforcementLevel(result as string ?? string.Empty);
                return true;
            }

            severity = default;
            return false;
        }

        private static DiagnosticSeverity ParseEnforcementLevel(string ruleSeverity)
        {
            switch (ruleSeverity)
            {
                case EditorConfigSeverityStrings.None:
                case EditorConfigSeverityStrings.Silent:
                    return DiagnosticSeverity.Hidden;

                case EditorConfigSeverityStrings.Suggestion: return DiagnosticSeverity.Info;
                case EditorConfigSeverityStrings.Warning: return DiagnosticSeverity.Warning;
                case EditorConfigSeverityStrings.Error: return DiagnosticSeverity.Error;
                default: return DiagnosticSeverity.Hidden;
            }
        }
    }
}
