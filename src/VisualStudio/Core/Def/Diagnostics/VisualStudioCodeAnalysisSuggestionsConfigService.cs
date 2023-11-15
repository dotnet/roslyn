// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeAnalysisSuggestions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeAnalysisSuggestions;

[ExportWorkspaceService(typeof(ICodeAnalysisSuggestionsConfigService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class VisualStudioCodeAnalysisSuggestionsConfigService(
    IGlobalOptionService globalOptions,
    IDiagnosticAnalyzerService diagnosticAnalyzerService) : ICodeAnalysisSuggestionsConfigService
{
    private readonly IGlobalOptionService _globalOptions = globalOptions;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService = diagnosticAnalyzerService;

    public async Task<ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool isExplicitlyInvoked, CancellationToken cancellationToken)
    {
        var summary = AnalyzerConfigSummaryHelper.GetAnalyzerConfigSummary(project, _globalOptions);
        if (summary == null)
            return ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>.Empty;

        var enableCodeQuality = ShouldShowSuggestions(summary, codeQuality: true, _globalOptions);
        var enableCodeStyle = ShouldShowSuggestions(summary, codeQuality: false, _globalOptions);
        if (!enableCodeQuality && !enableCodeStyle)
            return ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>.Empty;

        var computedDiagnostics = await GetAvailableDiagnosticsAsync(project, summary, enableCodeQuality, enableCodeStyle, forceCompute: isExplicitlyInvoked, cancellationToken).ConfigureAwait(false);
        return GetCodeAnalysisSuggestionsByCategory(summary, computedDiagnostics, enableCodeQuality, enableCodeStyle);

        static bool ShouldShowSuggestions(FirstPartyAnalyzerConfigSummary configSummary, bool codeQuality, IGlobalOptionService globalOptions)
        {
            // We should show code analysis suggestions if either of the below conditions are met:
            //      1. Current solution has at least '3' editorconfig entries escalating to Warning or Error severity.
            //         Note that '3' is an arbitrarily chosen count which can be adjusted in future.
            //      2. Current user has met the candidacy requirements related to invoking code fixes for
            //         code quality or code style diagnostics.

            var warningsAndErrorsCount = codeQuality
                ? configSummary.CodeQualitySummary.WarningsAndErrorsCount
                : configSummary.CodeStyleSummary.WarningsAndErrorsCount;
            if (warningsAndErrorsCount >= 3)
                return true;

            var isCandidateOption = codeQuality
                ? CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeQuality
                : CodeAnalysisSuggestionsOptionsStorage.HasMetCandidacyRequirementsForCodeStyle;
            return globalOptions.GetOption(isCandidateOption);
        }
    }

    private async Task<ImmutableArray<DiagnosticData>> GetAvailableDiagnosticsAsync(
        Project project,
        FirstPartyAnalyzerConfigSummary summary,
        bool enableCodeQuality,
        bool enableCodeStyle,
        bool forceCompute,
        CancellationToken cancellationToken)
    {
        // Get up-to-date project wide diagnostics if we are force computing.
        // Otherwise, return cached diagnostics from background analysis.

        ImmutableArray<DiagnosticData> diagnostics;
        if (forceCompute)
        {
            diagnostics = await _diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null,
                diagnosticIds: null, ShouldIncludeAnalyzer, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true,
                includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            diagnostics = await _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(project.Solution.Workspace, project.Id,
                documentId: null, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        }

        // Filter down to diagnostics with a source location in the current project.
        return diagnostics.WhereAsArray(d => d.DocumentId != null && d.ProjectId == project.Id);

        bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
        {
            if (analyzer.IsCompilerAnalyzer())
                return false;

            foreach (var descriptor in _diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(analyzer))
            {
                if (ShouldIncludeId(descriptor.Id, summary, enableCodeQuality, enableCodeStyle))
                    return true;
            }

            return false;
        }
    }

    private static bool ShouldIncludeId(string id, FirstPartyAnalyzerConfigSummary summary, bool enableCodeQuality, bool enableCodeStyle)
    {
        // Only suggest diagnostics that are not explicitly configured in analyzer config files
        if (enableCodeQuality
            && ShouldIncludeIdForSummary(id, summary.CodeQualitySummary))
        {
            return true;
        }

        if (enableCodeStyle
            && ShouldIncludeIdForSummary(id, summary.CodeStyleSummary))
        {
            return true;
        }

        return false;

        static bool ShouldIncludeIdForSummary(string id, AnalyzerConfigSummary summary)
            => Regex.IsMatch(id, summary.DiagnosticIdPattern)
               && !summary.ConfiguredDiagnosticIds.Contains(id, StringComparer.OrdinalIgnoreCase);
    }

    private static ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)> GetCodeAnalysisSuggestionsByCategory(
        FirstPartyAnalyzerConfigSummary configSummary,
        ImmutableArray<DiagnosticData> computedDiagnostics,
        bool enableCodeQuality,
        bool enableCodeStyle)
    {
        if (computedDiagnostics.IsEmpty)
            return ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>.Empty;

        using var _1 = ArrayBuilder<(string Category, ImmutableArray<DiagnosticData> Diagnostics)>.GetInstance(out var builder);
        using var _2 = ArrayBuilder<DiagnosticData>.GetInstance(out var diagnosticsBuilder);
        using var _3 = PooledHashSet<string>.GetInstance(out var uniqueIds);

        foreach (var (category, diagnosticsForCategory) in computedDiagnostics.GroupBy(d => d.Category).OrderBy(g => g.Key))
        {
            foreach (var (id, diagnostics) in diagnosticsForCategory.GroupBy(d => d.Id))
            {
                if (ShouldIncludeId(id, configSummary, enableCodeQuality, enableCodeStyle))
                {
                    diagnosticsBuilder.AddRange(diagnostics);
                }
            }

            if (diagnosticsBuilder.Count > 0)
            {
                builder.Add((category, diagnosticsBuilder.ToImmutableAndClear()));
            }
        }

        return builder.ToImmutable();
    }
}
