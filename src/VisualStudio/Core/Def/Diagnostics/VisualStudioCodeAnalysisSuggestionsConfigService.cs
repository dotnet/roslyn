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
[Export(typeof(VisualStudioCodeAnalysisSuggestionsConfigService))]
internal sealed partial class VisualStudioCodeAnalysisSuggestionsConfigService : ICodeAnalysisSuggestionsConfigService, IDisposable
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioCodeAnalysisSuggestionsConfigService(
        VisualStudioWorkspace workspace,
        IGlobalOptionService globalOptions,
        IDiagnosticAnalyzerService diagnosticAnalyzerService)
    {
        _workspace = workspace;
        _globalOptions = globalOptions;
        _diagnosticAnalyzerService = diagnosticAnalyzerService;
        _documentTrackingService = workspace.Services.GetRequiredService<IDocumentTrackingService>();

        workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
    }

    private readonly VisualStudioWorkspace _workspace;
    private readonly IGlobalOptionService _globalOptions;
    private readonly IDocumentTrackingService _documentTrackingService;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        RefreshDiagnostics(_workspace.CurrentSolution, cancellationToken);
        return Task.CompletedTask;
    }

    private void RefreshDiagnostics(Solution solution, CancellationToken cancellationToken)
    {
        foreach (var project in solution.Projects)
            KickOffBackgroundComputation(project, cancellationToken);
    }

    private void KickOffBackgroundComputation(Project project, CancellationToken cancellationToken)
        => Task.Run(() => GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: true, cancellationToken), cancellationToken);

    private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        switch (e.Kind)
        {
            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.SolutionChanged:
            case WorkspaceChangeKind.SolutionReloaded:
                RefreshDiagnostics(e.NewSolution, CancellationToken.None);
                break;

            case WorkspaceChangeKind.AdditionalDocumentAdded:
            case WorkspaceChangeKind.AdditionalDocumentReloaded:
            case WorkspaceChangeKind.AdditionalDocumentChanged:
            case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                if (e.NewSolution.GetProject(e.ProjectId) is { } project)
                    KickOffBackgroundComputation(project, CancellationToken.None);
                break;
        }
    }

    public Task<ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken)
        => GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: false, cancellationToken);

    private async Task<ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>> GetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool forceCompute, CancellationToken cancellationToken)
    {
        var summary = AnalyzerConfigSummaryHelper.GetAnalyzerConfigSummary(project, _globalOptions);
        if (summary == null)
            return ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>.Empty;

        var enableCodeQuality = ShouldShowSuggestions(summary, codeQuality: true, _globalOptions);
        var enableCodeStyle = ShouldShowSuggestions(summary, codeQuality: false, _globalOptions);
        if (!enableCodeQuality && !enableCodeStyle)
            return ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>.Empty;

        var computedDiagnostics = await GetAvailableDiagnosticsAsync(project, summary, enableCodeQuality, enableCodeStyle, forceCompute, cancellationToken).ConfigureAwait(false);
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
        if (forceCompute)
        {
            return await _diagnosticAnalyzerService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null,
                diagnosticIds: null, ShouldIncludeAnalyzer, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true,
                includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        }

        // Attempt to get cached project diagnostics.
        var cachedDiagnostics = await _diagnosticAnalyzerService.GetCachedDiagnosticsAsync(_workspace, project.Id,
            documentId: null, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        
        if (!cachedDiagnostics.IsEmpty)
            return cachedDiagnostics;

        // Otherwise, attempt to get active document diagnostics.
        if (_documentTrackingService.TryGetActiveDocument() is { } documentId &&
            documentId.ProjectId == project.Id &&
            project.GetDocument(documentId) is { } document)
        {
            return await _diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken).ConfigureAwait(false);
        }

        // Otherwise, kick off background computation of diagnostics for current project snapshot,
        // but do not wait for the computation and bail out from the current request with empty diagnostics.
        KickOffBackgroundComputation(project, cancellationToken);
        return ImmutableArray<DiagnosticData>.Empty;

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

    private static ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)> GetCodeAnalysisSuggestionsByCategory(
        FirstPartyAnalyzerConfigSummary configSummary,
        ImmutableArray<DiagnosticData> computedDiagnostics,
        bool enableCodeQuality,
        bool enableCodeStyle)
    {
        if (computedDiagnostics.IsEmpty)
            return ImmutableArray<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>.Empty;

        using var _1 = ArrayBuilder<(string Category, ImmutableDictionary<string, ImmutableArray<DiagnosticData>> DiagnosticsById)>.GetInstance(out var builder);
        using var _2 = PooledDictionary<string, ImmutableArray<DiagnosticData>>.GetInstance(out var diagnosticsByIdBuilder);
        using var _3 = PooledHashSet<string>.GetInstance(out var uniqueIds);

        foreach (var (category, diagnosticsForCategory) in computedDiagnostics.GroupBy(d => d.Category).OrderBy(g => g.Key))
        {
            var orderedDiagnostics = diagnosticsForCategory.GroupBy(d => d.Id).OrderByDescending(group => group.Count());
            foreach (var (id, diagnostics) in orderedDiagnostics)
            {
                if (ShouldIncludeId(id, configSummary, enableCodeQuality, enableCodeStyle))
                {
                    diagnosticsByIdBuilder.Add(id, diagnostics.ToImmutableArray());
                }
            }

            if (diagnosticsByIdBuilder.Count > 0)
            {
                builder.Add((category, diagnosticsByIdBuilder.ToImmutableDictionary()));
                diagnosticsByIdBuilder.Clear();
            }
        }

        return builder.ToImmutable();
    }

    public void Dispose()
    {
        _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
    }
}
