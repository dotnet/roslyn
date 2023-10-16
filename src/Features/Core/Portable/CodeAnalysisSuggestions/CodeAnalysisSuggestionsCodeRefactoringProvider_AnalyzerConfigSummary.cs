// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions;

internal sealed partial class CodeAnalysisSuggestionsCodeRefactoringProvider
{
    private record class AnalyzerConfigSummary(int WarningsAndErrorsCount, ImmutableHashSet<string> ConfiguredDiagnosticIds);
    private record class FirstPartyAnalyzerConfigSummary(AnalyzerConfigSummary CodeQualitySummary, AnalyzerConfigSummary CodeStyleSummary);

    private static FirstPartyAnalyzerConfigSummary? GetAnalyzerConfigSummary(Project project, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
    {
        const string CodeQualityPattern = "[cC][aA][0-9]{4}";
        const string CodeStylePattern = "[iI][dD][eE][0-9]{4}";

        if (!(project.GetAnalyzerConfigOptions() is { } analyzerConfigOptions))
            return null;

        if (globalOptions.GetOption(CodeAnalysisSuggestionsOptionsStorage.DisableFirstPartyAnalyzersSuggestions))
            return null;

        var codeQualityWarningsAndErrorsCount = 0;
        var codeStyleWarningsAndErrorsCount = 0;
        using var _1 = PooledHashSet<string>.GetInstance(out var configuredCodeQualityIdsBuilder);
        using var _2 = PooledHashSet<string>.GetInstance(out var configuredCodeStyleIdsBuilder);
        using var _3 = PooledHashSet<string>.GetInstance(out var uniqueWarnsAndErrors);
        foreach (var (diagnosticId, severity) in analyzerConfigOptions.TreeOptions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HandleEntry(diagnosticId, severity, uniqueWarnsAndErrors, configuredCodeQualityIdsBuilder, CodeQualityPattern, ref codeQualityWarningsAndErrorsCount);
            HandleEntry(diagnosticId, severity, uniqueWarnsAndErrors, configuredCodeStyleIdsBuilder, CodeStylePattern, ref codeStyleWarningsAndErrorsCount);
        }

        var codeQualitySummary = new AnalyzerConfigSummary(codeQualityWarningsAndErrorsCount, configuredCodeQualityIdsBuilder.ToImmutableHashSet());
        var codeStyleSummary = new AnalyzerConfigSummary(codeStyleWarningsAndErrorsCount, configuredCodeStyleIdsBuilder.ToImmutableHashSet());
        return new FirstPartyAnalyzerConfigSummary(codeQualitySummary, codeStyleSummary);

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

    private static bool ShouldShowSuggestions(
        FirstPartyAnalyzerConfigSummary configSummary,
        bool codeQuality,
        IGlobalOptionService globalOptions,
        out ImmutableHashSet<string> configuredDiagnosticIds)
    {
        var warningsAndErrorsCount = codeQuality
            ? configSummary.CodeQualitySummary.WarningsAndErrorsCount
            : configSummary.CodeStyleSummary.WarningsAndErrorsCount;
        configuredDiagnosticIds = codeQuality
            ? configSummary.CodeQualitySummary.ConfiguredDiagnosticIds
            : configSummary.CodeStyleSummary.ConfiguredDiagnosticIds;

        if (warningsAndErrorsCount >= 3)
            return true;

        var isCandidateOption = codeQuality
                ? CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeQuality
                : CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeStyle;
        return globalOptions.GetOption(isCandidateOption);
    }
}
