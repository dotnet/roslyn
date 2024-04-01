// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.InternalUtilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerOptionsExtensions
    {
        private const string DotnetAnalyzerDiagnosticPrefix = "dotnet_analyzer_diagnostic";
        private const string CategoryPrefix = "category";
        private const string SeveritySuffix = "severity";

        private const string DotnetAnalyzerDiagnosticSeverityKey = DotnetAnalyzerDiagnosticPrefix + "." + SeveritySuffix;
        private static readonly ConcurrentLruCache<string, string> s_categoryToSeverityKeyMap = new ConcurrentLruCache<string, string>(50);

        private static string GetCategoryBasedDotnetAnalyzerDiagnosticSeverityKey(string category)
            => s_categoryToSeverityKeyMap.GetOrAdd(category, category, static category => $"{DotnetAnalyzerDiagnosticPrefix}.{CategoryPrefix}-{category}.{SeveritySuffix}");

        /// <summary>
        /// Tries to get configured severity for the given <paramref name="descriptor"/>
        /// for the given <paramref name="tree"/> from bulk configuration analyzer config options, i.e.
        ///     'dotnet_analyzer_diagnostic.category-%RuleCategory%.severity = %severity%'
        ///         or
        ///     'dotnet_analyzer_diagnostic.severity = %severity%'
        /// </summary>
        public static bool TryGetSeverityFromBulkConfiguration(
            this AnalyzerOptions? analyzerOptions,
            SyntaxTree tree,
            Compilation compilation,
            DiagnosticDescriptor descriptor,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity)
        {
            // Analyzer bulk configuration does not apply to:
            //  1. Disabled by default diagnostics
            //  2. Compiler diagnostics
            //  3. Non-configurable diagnostics
            //  4. Custom-configurable diagnostics
            if (analyzerOptions == null ||
                !descriptor.IsEnabledByDefault ||
                descriptor.IsCompilerOrNotConfigurableOrCustomConfigurable())
            {
                severity = default;
                return false;
            }

            // If user has explicitly configured severity for this diagnostic ID, that should be respected and
            // bulk configuration should not be applied.
            // For example, 'dotnet_diagnostic.CA1000.severity = error'
            if (compilation.Options.SpecificDiagnosticOptions.ContainsKey(descriptor.Id) ||
                compilation.Options.SyntaxTreeOptionsProvider?.TryGetDiagnosticValue(tree, descriptor.Id, cancellationToken, out _) == true ||
                compilation.Options.SyntaxTreeOptionsProvider?.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out _) == true)
            {
                severity = default;
                return false;
            }

            var analyzerConfigOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);

            // If user has explicitly configured default severity for the diagnostic category, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.category-security.severity = error'
            var categoryBasedKey = GetCategoryBasedDotnetAnalyzerDiagnosticSeverityKey(descriptor.Category);
            if (analyzerConfigOptions.TryGetValue(categoryBasedKey, out var value) &&
                AnalyzerConfigSet.TryParseSeverity(value, out severity))
            {
                // '/warnaserror' should bump Warning bulk configuration to Error.
                if (severity == ReportDiagnostic.Warn && compilation.Options.GeneralDiagnosticOption == ReportDiagnostic.Error)
                    severity = ReportDiagnostic.Error;

                return true;
            }

            // Otherwise, if user has explicitly configured default severity for all analyzer diagnostics, that should be respected.
            // For example, 'dotnet_analyzer_diagnostic.severity = error'
            if (analyzerConfigOptions.TryGetValue(DotnetAnalyzerDiagnosticSeverityKey, out value) &&
                AnalyzerConfigSet.TryParseSeverity(value, out severity))
            {
                // '/warnaserror' should bump Warning bulk configuration to Error.
                if (severity == ReportDiagnostic.Warn && compilation.Options.GeneralDiagnosticOption == ReportDiagnostic.Error)
                    severity = ReportDiagnostic.Error;

                return true;
            }

            severity = default;
            return false;
        }
    }
}
