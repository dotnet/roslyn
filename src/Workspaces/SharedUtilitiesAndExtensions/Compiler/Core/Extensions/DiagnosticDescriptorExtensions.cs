// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class DiagnosticDescriptorExtensions
    {
        private const string DotnetAnalyzerDiagnosticPrefix = "dotnet_analyzer_diagnostic";
        private const string DotnetDiagnosticPrefix = "dotnet_diagnostic";
        private const string CategoryPrefix = "category";
        private const string SeveritySuffix = "severity";

        private const string DotnetAnalyzerDiagnosticSeverityKey = DotnetAnalyzerDiagnosticPrefix + "." + SeveritySuffix;

        public static ImmutableArray<string> ImmutableCustomTags(this DiagnosticDescriptor descriptor)
        {
            Debug.Assert(descriptor.CustomTags is ImmutableArray<string>);
            return (ImmutableArray<string>)descriptor.CustomTags;
        }

        /// <summary>
        /// Gets project-level effective severity of the given <paramref name="descriptor"/> accounting for severity configurations from both the following sources:
        /// 1. Compilation options from ruleset file, if any, and command line options such as /nowarn, /warnaserror, etc.
        /// 2. Analyzer config documents at the project root directory or in ancestor directories.
        /// </summary>
        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions compilationOptions, AnalyzerConfigOptionsResult? analyzerConfigOptions)
        {
            var effectiveSeverity = descriptor.GetEffectiveSeverity(compilationOptions);

            // Apply analyzer config options, unless configured with a non-default value in compilation options.
            // Note that compilation options (/nowarn, /warnaserror) override analyzer config options.
            if (analyzerConfigOptions.HasValue &&
                (!compilationOptions.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var reportDiagnostic) ||
                 reportDiagnostic == ReportDiagnostic.Default))
            {
                if (analyzerConfigOptions.Value.TreeOptions.TryGetValue(descriptor.Id, out reportDiagnostic) && reportDiagnostic != ReportDiagnostic.Default ||
                    TryGetSeverityFromBulkConfiguration(descriptor, analyzerConfigOptions.Value, out reportDiagnostic))
                {
                    Debug.Assert(reportDiagnostic != ReportDiagnostic.Default);
                    effectiveSeverity = reportDiagnostic;
                }
            }

            return effectiveSeverity;
        }

        public static bool IsDefinedInEditorConfig(this DiagnosticDescriptor descriptor, AnalyzerConfigOptions analyzerConfigOptions)
        {
            // Check if the option is defined explicitly in the editorconfig
            var diagnosticKey = $"{DotnetDiagnosticPrefix}.{descriptor.Id}.{SeveritySuffix}";
            if (analyzerConfigOptions.TryGetValue(diagnosticKey, out var value) &&
                EditorConfigSeverityStrings.TryParse(value, out var severity))
            {
                return true;
            }

            // Check if the option is defined as part of a bulk configuration
            // Analyzer bulk configuration does not apply to:
            //  1. Disabled by default diagnostics
            //  2. Compiler diagnostics
            //  3. Non-configurable diagnostics
            if (!descriptor.IsEnabledByDefault ||
                descriptor.ImmutableCustomTags().Any(tag => tag is WellKnownDiagnosticTags.Compiler or WellKnownDiagnosticTags.NotConfigurable))
            {
                return false;
            }

            // If user has explicitly configured default severity for the diagnostic category, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.category-security.severity = error'
            var categoryBasedKey = $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{descriptor.Category}.{SeveritySuffix}";
            if (analyzerConfigOptions.TryGetValue(categoryBasedKey, out value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return true;
            }

            // Otherwise, if user has explicitly configured default severity for all analyzer diagnostics, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.severity = error'
            if (analyzerConfigOptions.TryGetValue(DotnetAnalyzerDiagnosticSeverityKey, out value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return true;
            }

            // option not defined in editorconfig, assumed to be the default
            return false;
        }

        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, AnalyzerConfigOptions analyzerConfigOptions)
        {
            // Check if the option is defined explicitly in the editorconfig
            var diagnosticKey = $"{DotnetDiagnosticPrefix}.{descriptor.Id}.{SeveritySuffix}";
            if (analyzerConfigOptions.TryGetValue(diagnosticKey, out var value) &&
                EditorConfigSeverityStrings.TryParse(value, out var severity))
            {
                return severity;
            }

            // Check if the option is defined as part of a bulk configuration
            // Analyzer bulk configuration does not apply to:
            //  1. Disabled by default diagnostics
            //  2. Compiler diagnostics
            //  3. Non-configurable diagnostics
            if (!descriptor.IsEnabledByDefault ||
                descriptor.ImmutableCustomTags().Any(tag => tag is WellKnownDiagnosticTags.Compiler or WellKnownDiagnosticTags.NotConfigurable))
            {
                return ReportDiagnostic.Default;
            }

            // If user has explicitly configured default severity for the diagnostic category, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.category-security.severity = error'
            var categoryBasedKey = $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{descriptor.Category}.{SeveritySuffix}";
            if (analyzerConfigOptions.TryGetValue(categoryBasedKey, out value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return severity;
            }

            // Otherwise, if user has explicitly configured default severity for all analyzer diagnostics, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.severity = error'
            if (analyzerConfigOptions.TryGetValue(DotnetAnalyzerDiagnosticSeverityKey, out value) &&
                EditorConfigSeverityStrings.TryParse(value, out severity))
            {
                return severity;
            }

            // option not defined in editorconfig, assumed to be the default
            return ReportDiagnostic.Default;
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
                descriptor.ImmutableCustomTags().Any(tag => tag is WellKnownDiagnosticTags.Compiler or WellKnownDiagnosticTags.NotConfigurable))
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

        public static bool IsCompilationEnd(this DiagnosticDescriptor descriptor)
            => descriptor.ImmutableCustomTags().Contains(WellKnownDiagnosticTags.CompilationEnd);

        // TODO: the value stored in descriptor should already be valid URI (https://github.com/dotnet/roslyn/issues/59205)
        internal static Uri? GetValidHelpLinkUri(this DiagnosticDescriptor descriptor)
           => Uri.TryCreate(descriptor.HelpLinkUri, UriKind.Absolute, out var uri) &&
              (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) ? uri : null;
    }
}
