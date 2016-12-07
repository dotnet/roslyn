// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

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
            var severity = GetRuleSeverity(namingRuleTitle, conventionsDictionary);
            serializableNamingRule = new SerializableNamingRule()
            {
                EnforcementLevel = severity,
                NamingStyleID = namingStyle.ID,
                SymbolSpecificationID = symbolSpec.ID
            };

            return true;
        }

        private static DiagnosticSeverity GetRuleSeverity(
            string namingRuleName,
            IReadOnlyDictionary<string, object> conventionsDictionary)
        {
            if (conventionsDictionary.TryGetValue($"dotnet_naming_rule.{namingRuleName}.severity", out object result))
            {
                return ParseEnforcementLevel(result as string ?? string.Empty);
            }

            return default(DiagnosticSeverity);
        }

        private static DiagnosticSeverity ParseEnforcementLevel(string ruleSeverity)
        {
            switch (ruleSeverity)
            {
                case EditorConfigSeverityStrings.Silent: return DiagnosticSeverity.Hidden;
                case EditorConfigSeverityStrings.Suggestion: return DiagnosticSeverity.Info;
                case EditorConfigSeverityStrings.Warning: return DiagnosticSeverity.Warning;
                case EditorConfigSeverityStrings.Error: return DiagnosticSeverity.Error;
                default: return DiagnosticSeverity.Hidden;
            }
        }
    }
}
