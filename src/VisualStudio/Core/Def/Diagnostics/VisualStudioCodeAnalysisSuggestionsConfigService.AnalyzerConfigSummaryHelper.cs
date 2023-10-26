// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeAnalysisSuggestions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeAnalysisSuggestions;

using TreeOptions = ImmutableDictionary<string, ReportDiagnostic>;

internal sealed partial class VisualStudioCodeAnalysisSuggestionsConfigService
{
    private record class AnalyzerConfigSummary(int WarningsAndErrorsCount, ImmutableHashSet<string> ConfiguredDiagnosticIds, string DiagnosticIdPattern);
    private record class FirstPartyAnalyzerConfigSummary(AnalyzerConfigSummary CodeQualitySummary, AnalyzerConfigSummary CodeStyleSummary);

    private static class AnalyzerConfigSummaryHelper
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, FirstPartyAnalyzerConfigSummary?> s_summaryCache = new();

        public static FirstPartyAnalyzerConfigSummary? GetAnalyzerConfigSummary(Project project, IGlobalOptionService globalOptions)
        {
            if (!globalOptions.GetOption(CodeAnalysisSuggestionsOptionsStorage.ShowCodeAnalysisSuggestionsInLightbulb))
                return null;

            if (s_summaryCache.TryGetValue(project.AnalyzerOptions, out var summary))
                return summary;

            if (project.GetAnalyzerConfigOptions() is { } analyzerConfigOptions
                && project.GetGlobalAnalyzerConfigOptions() is { } globalAnalyzerConfigOptions)
            {
                summary = CreateAnalyzerConfigSummary(analyzerConfigOptions.TreeOptions, globalAnalyzerConfigOptions.TreeOptions);
            }
            else
            {
                summary = null;
            }

            return s_summaryCache.GetValue(project.AnalyzerOptions, new(_ => summary));
        }

        private static FirstPartyAnalyzerConfigSummary? CreateAnalyzerConfigSummary(TreeOptions treeOptions, TreeOptions globalOptions)
        {
            const string CodeQualityPattern = "[cC][aA][0-9]{4}";
            const string CodeStylePattern = "[iI][dD][eE][0-9]{4}";

            var codeQualityWarningsAndErrorsCount = 0;
            var codeStyleWarningsAndErrorsCount = 0;
            using var _1 = PooledHashSet<string>.GetInstance(out var configuredCodeQualityIdsBuilder);
            using var _2 = PooledHashSet<string>.GetInstance(out var configuredCodeStyleIdsBuilder);
            using var _3 = PooledHashSet<string>.GetInstance(out var uniqueWarnsAndErrors);
            foreach (var (diagnosticId, severity) in GetAllOptionPairs(treeOptions, globalOptions))
            {
                HandleEntry(diagnosticId, severity, uniqueWarnsAndErrors, configuredCodeQualityIdsBuilder, CodeQualityPattern, ref codeQualityWarningsAndErrorsCount);
                HandleEntry(diagnosticId, severity, uniqueWarnsAndErrors, configuredCodeStyleIdsBuilder, CodeStylePattern, ref codeStyleWarningsAndErrorsCount);
            }

            var codeQualitySummary = new AnalyzerConfigSummary(codeQualityWarningsAndErrorsCount, configuredCodeQualityIdsBuilder.ToImmutableHashSet(), CodeQualityPattern);
            var codeStyleSummary = new AnalyzerConfigSummary(codeStyleWarningsAndErrorsCount, configuredCodeStyleIdsBuilder.ToImmutableHashSet(), CodeStylePattern);
            return new FirstPartyAnalyzerConfigSummary(codeQualitySummary, codeStyleSummary);

            static IEnumerable<(string DiagnosticId, ReportDiagnostic severity)> GetAllOptionPairs(TreeOptions treeOptions, TreeOptions globalOptions)
            {
                foreach (var (diagnosticId, severity) in treeOptions)
                    yield return (diagnosticId, severity);

                foreach (var (diagnosticId, severity) in globalOptions)
                    yield return (diagnosticId, severity);
            }

            static void HandleEntry(
                string diagnosticId,
                ReportDiagnostic severity,
                PooledHashSet<string> uniqueWarnsAndErrors,
                PooledHashSet<string> configuredIdsBuilder,
                string pattern,
                ref int warningsAndErrorsCount)
            {
                if (!Regex.Match(diagnosticId, pattern).Success)
                    return;

                configuredIdsBuilder.Add(diagnosticId);
                if (severity is ReportDiagnostic.Warn or ReportDiagnostic.Error
                    && uniqueWarnsAndErrors.Add(diagnosticId))
                {
                    warningsAndErrorsCount++;
                }
            }
        }
    }
}
