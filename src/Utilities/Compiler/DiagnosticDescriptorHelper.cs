// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if NET_ANALYZERS || FXCOP_ANALYZERS

using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;

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
            bool isEnabledByDefaultInFxCopAnalyzers = true,
            bool isEnabledByDefaultInAggressiveMode = true,
            params string[] additionalCustomTags)
        {
            // PERF: Ensure that all DFA rules are disabled by default in NetAnalyzers package.
            Debug.Assert(!isDataflowRule || ruleLevel == RuleLevel.Disabled || ruleLevel == RuleLevel.CandidateForRemoval);

            // Ensure 'isEnabledByDefaultInAggressiveMode' is not false for enabled rules in default mode
            Debug.Assert(isEnabledByDefaultInAggressiveMode || ruleLevel == RuleLevel.Disabled || ruleLevel == RuleLevel.CandidateForRemoval);

            var (defaultSeverity, enabledByDefault) = GetDefaultSeverityAndEnabledByDefault(ruleLevel, isEnabledByDefaultInFxCopAnalyzers);

#pragma warning disable CA1308 // Normalize strings to uppercase - use lower case ID in help link
            var helpLink = $"https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/{id.ToLowerInvariant()}";
#pragma warning restore CA1308 // Normalize strings to uppercase

            var customTags = GetDefaultCustomTags(isPortedFxCopRule, isDataflowRule, isEnabledByDefaultInAggressiveMode);
            if (additionalCustomTags.Length > 0)
            {
                customTags = customTags.Concat(additionalCustomTags).ToArray();
            }

#pragma warning disable RS0030 // The symbol 'DiagnosticDescriptor.DiagnosticDescriptor.#ctor' is banned in this project: Use 'DiagnosticDescriptorHelper.Create' instead
            return new DiagnosticDescriptor(id, title, messageFormat, category, defaultSeverity, enabledByDefault, description, helpLink, customTags);
#pragma warning restore RS0030

#pragma warning disable CA1801 // Remove unused parameter - parameters used conditionally
            static (DiagnosticSeverity defaultSeverity, bool enabledByDefault) GetDefaultSeverityAndEnabledByDefault(
                RuleLevel ruleLevel,
                bool isEnabledByDefaultInFxCopAnalyzers)
#pragma warning restore CA1801 // Remove unused parameter
            {
#if FXCOP_ANALYZERS
                return (DiagnosticSeverity.Warning, isEnabledByDefaultInFxCopAnalyzers);
#else
                return ruleLevel switch
                {
                    RuleLevel.BuildWarning => (DiagnosticSeverity.Warning, true),
                    RuleLevel.IdeSuggestion => (DiagnosticSeverity.Info, true),
                    RuleLevel.IdeHidden_BulkConfigurable => (DiagnosticSeverity.Hidden, true),
                    RuleLevel.Disabled => (DiagnosticSeverity.Warning, false),
                    RuleLevel.CandidateForRemoval => (DiagnosticSeverity.Warning, false),
                    _ => throw new System.NotImplementedException(),
                };
#endif
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