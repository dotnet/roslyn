using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

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
            var severity= GetRuleSeverity(namingRuleTitle, conventionsDictionary);
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

            return default(DiagnosticSeverity); ;
        }

        private static DiagnosticSeverity ParseEnforcementLevel(string ruleSeverity)
        {
            switch (ruleSeverity)
            {
                case "silent": return DiagnosticSeverity.Hidden;
                case "suggestion": return DiagnosticSeverity.Info;
                case "warning": return DiagnosticSeverity.Warning;
                case "error": return DiagnosticSeverity.Error;
                default: return DiagnosticSeverity.Hidden;
            }
        }
    }
}
