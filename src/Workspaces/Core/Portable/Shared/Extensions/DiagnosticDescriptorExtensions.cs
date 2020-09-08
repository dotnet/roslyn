// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticDescriptorExtensions
    {
        private const string DotnetAnalyzerDiagnosticPrefix = "dotnet_analyzer_diagnostic";
        private const string CategoryPrefix = "category";
        private const string SeveritySuffix = "severity";

        private const string DotnetAnalyzerDiagnosticSeverityKey = DotnetAnalyzerDiagnosticPrefix + "." + SeveritySuffix;

        /// <summary>
        /// Gets project-level effective severity of the given <paramref name="descriptor"/> accounting for severity configurations from both the following sources:
        /// 1. Compilation options from ruleset file, if any, and command line options such as /nowarn, /warnaserror, etc.
        /// 2. Analyzer config documents at the project root directory or in ancestor directories.
        /// </summary>
        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions compilationOptions, AnalyzerConfigOptionsResult? analyzerConfigOptions)
        {
            var effectiveSeverity = descriptor.GetEffectiveSeverity(compilationOptions);

            // Apply analyzer config options on top of compilation options.
            // Note that they override any diagnostic settings from compilation options (/nowarn, /warnaserror).
            if (analyzerConfigOptions.HasValue)
            {
                if (analyzerConfigOptions.Value.TreeOptions.TryGetValue(descriptor.Id, out var reportDiagnostic) && reportDiagnostic != ReportDiagnostic.Default ||
                    TryGetSeverityFromBulkConfiguration(descriptor, analyzerConfigOptions.Value, out reportDiagnostic))
                {
                    Debug.Assert(reportDiagnostic != ReportDiagnostic.Default);
                    effectiveSeverity = reportDiagnostic;
                }
            }

            return effectiveSeverity;
        }

        /// <summary>
        /// Tries to get configured severity for the given <paramref name="descriptor"/>
        /// from bulk configuration analyzer config options, i.e.
        ///     'dotnet_analyzer_diagnostic.category-%RuleCategory%.severity = %severity%'
        ///         or
        ///     'dotnet_analyzer_diagnostic.severity = %severity%'
        /// Docs: https://docs.microsoft.com/visualstudio/code-quality/use-roslyn-analyzers?view=vs-2019#set-rule-severity-of-multiple-analyzer-rules-at-once-in-an-editorconfig-file for details
        /// </summary>
        private static bool TryGetSeverityFromBulkConfiguration(
            DiagnosticDescriptor descriptor,
            AnalyzerConfigOptionsResult analyzerConfigOptions,
            out ReportDiagnostic severity)
        {
            Debug.Assert(!analyzerConfigOptions.TreeOptions.ContainsKey(descriptor.Id));

            // Analyzer bulk configuration does not apply to:
            //  1. Disabled by default diagnostics
            //  2. Compiler diagnostics
            //  3. Non-configurable diagnostics
            if (!descriptor.IsEnabledByDefault ||
                descriptor.CustomTags.Any(tag => tag == WellKnownDiagnosticTags.Compiler || tag == WellKnownDiagnosticTags.NotConfigurable))
            {
                severity = default;
                return false;
            }

            // If user has explicitly configured default severity for the diagnostic category, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.category-security.severity = error'
            var categoryBasedKey = $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{descriptor.Category}.{SeveritySuffix}";
            if (analyzerConfigOptions.AnalyzerOptions.TryGetValue(categoryBasedKey, out var value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return true;
            }

            // Otherwise, if user has explicitly configured default severity for all analyzer diagnostics, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.severity = error'
            if (analyzerConfigOptions.AnalyzerOptions.TryGetValue(DotnetAnalyzerDiagnosticSeverityKey, out value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return true;
            }

            severity = default;
            return false;
        }
    }
}
