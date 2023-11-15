// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            // Regular expressions for detecting 'CAxxxx' and 'IDExxxx' diagnostic IDs.
            const string CodeQualityPattern = "[cC][aA][0-9]{4}";
            const string CodeStylePattern = "[iI][dD][eE][0-9]{4}";

            var codeQualityWarningAndErrorIdCount = 0;
            var codeStyleWarningAndErrorIdCount = 0;
            using var _1 = PooledHashSet<string>.GetInstance(out var configuredCodeQualityIdsBuilder);
            using var _2 = PooledHashSet<string>.GetInstance(out var configuredCodeStyleIdsBuilder);
            using var _3 = PooledHashSet<string>.GetInstance(out var uniqueWarningAndErrorIds);
            foreach (var (diagnosticId, severity) in treeOptions.Concat(globalOptions))
            {
                HandleEntry(diagnosticId, severity, uniqueWarningAndErrorIds, configuredCodeQualityIdsBuilder, CodeQualityPattern, ref codeQualityWarningAndErrorIdCount);
                HandleEntry(diagnosticId, severity, uniqueWarningAndErrorIds, configuredCodeStyleIdsBuilder, CodeStylePattern, ref codeStyleWarningAndErrorIdCount);
            }

            var codeQualitySummary = new AnalyzerConfigSummary(codeQualityWarningAndErrorIdCount, configuredCodeQualityIdsBuilder.ToImmutableHashSet(), CodeQualityPattern);
            var codeStyleSummary = new AnalyzerConfigSummary(codeStyleWarningAndErrorIdCount, configuredCodeStyleIdsBuilder.ToImmutableHashSet(), CodeStylePattern);
            return new FirstPartyAnalyzerConfigSummary(codeQualitySummary, codeStyleSummary);

            static void HandleEntry(
                string diagnosticId,
                ReportDiagnostic severity,
                PooledHashSet<string> uniqueWarningAndErrorIds,
                PooledHashSet<string> configuredIdsBuilder,
                string pattern,
                ref int warningAndErrorIdCount)
            {
                if (!Regex.Match(diagnosticId, pattern).Success)
                    return;

                configuredIdsBuilder.Add(diagnosticId);
                if (severity is ReportDiagnostic.Warn or ReportDiagnostic.Error
                    && uniqueWarningAndErrorIds.Add(diagnosticId))
                {
                    warningAndErrorIdCount++;
                }
            }
        }
    }
}
