// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET_ANALYZERS || TEXT_ANALYZERS

using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    internal static partial class DiagnosticDescriptorHelper
    {
        public static DiagnosticDescriptor Create(
            string id,
            LocalizableString title,
            LocalizableString messageFormat,
            string category,
            RuleLevel ruleLevel,
            LocalizableString? description,
            bool isPortedFxCopRule,
            bool isDataflowRule,
            bool isEnabledByDefaultInAggressiveMode = true,
            bool isReportedAtCompilationEnd = false,
            params string[] additionalCustomTags)
        {
            // PERF: Ensure that all DFA rules are disabled by default in NetAnalyzers package.
            Debug.Assert(!isDataflowRule || ruleLevel == RuleLevel.Disabled || ruleLevel == RuleLevel.CandidateForRemoval);

            // Ensure 'isEnabledByDefaultInAggressiveMode' is not false for enabled rules in default mode
            Debug.Assert(isEnabledByDefaultInAggressiveMode || ruleLevel == RuleLevel.Disabled || ruleLevel == RuleLevel.CandidateForRemoval);

            var (defaultSeverity, enabledByDefault) = GetDefaultSeverityAndEnabledByDefault(ruleLevel);
#pragma warning restore CA1308 // Normalize strings to uppercase

            var customTags = GetDefaultCustomTags(isPortedFxCopRule, isDataflowRule, isEnabledByDefaultInAggressiveMode);
            if (isReportedAtCompilationEnd)
            {
                customTags = customTags.Concat(WellKnownDiagnosticTagsExtensions.CompilationEnd).ToArray();
            }

            if (additionalCustomTags.Length > 0)
            {
                customTags = customTags.Concat(additionalCustomTags).ToArray();
            }

#pragma warning disable RS0030 // The symbol 'DiagnosticDescriptor.DiagnosticDescriptor.#ctor' is banned in this project: Use 'DiagnosticDescriptorHelper.Create' instead
            return new DiagnosticDescriptor(id, title, messageFormat, category, defaultSeverity, enabledByDefault, description, $"https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/{id.ToLowerInvariant()}", customTags);
#pragma warning restore RS0030

            static (DiagnosticSeverity defaultSeverity, bool enabledByDefault) GetDefaultSeverityAndEnabledByDefault(RuleLevel ruleLevel)
            {
                return ruleLevel switch
                {
                    RuleLevel.BuildWarning => (DiagnosticSeverity.Warning, true),
                    RuleLevel.IdeSuggestion => (DiagnosticSeverity.Info, true),
                    RuleLevel.IdeHidden_BulkConfigurable => (DiagnosticSeverity.Hidden, true),
                    RuleLevel.Disabled => (DiagnosticSeverity.Warning, false),
                    RuleLevel.CandidateForRemoval => (DiagnosticSeverity.Warning, false),
                    RuleLevel.BuildError => (DiagnosticSeverity.Error, true),
                    _ => throw new System.NotImplementedException(),
                };
            }

            static string[] GetDefaultCustomTags(
                bool isPortedFxCopRule,
                bool isDataflowRule,
                bool isEnabledByDefaultInAggressiveMode)
            {
                if (isEnabledByDefaultInAggressiveMode)
                {
                    return isPortedFxCopRule ?
                    (isDataflowRule ? FxCopWellKnownDiagnosticTags.PortedFxCopDataflowRuleEnabledInAggressiveMode : FxCopWellKnownDiagnosticTags.PortedFxCopRuleEnabledInAggressiveMode) :
                    (isDataflowRule ? WellKnownDiagnosticTagsExtensions.DataflowAndTelemetryEnabledInAggressiveMode : WellKnownDiagnosticTagsExtensions.TelemetryEnabledInAggressiveMode);
                }
                else
                {
                    return isPortedFxCopRule ?
                    (isDataflowRule ? FxCopWellKnownDiagnosticTags.PortedFxCopDataflowRule : FxCopWellKnownDiagnosticTags.PortedFxCopRule) :
                    (isDataflowRule ? WellKnownDiagnosticTagsExtensions.DataflowAndTelemetry : WellKnownDiagnosticTagsExtensions.Telemetry);
                }
            }
        }
    }
}

#endif
