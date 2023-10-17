// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeAnalysisSuggestions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
        IThreadingContext threadingContext,
        IGlobalOptionService globalOptions,
        IDiagnosticAnalyzerService diagnosticAnalyzerService)
    {
        _workspace = workspace;
        _threadingContext = threadingContext;
        _globalOptions = globalOptions;
        _diagnosticAnalyzerService = diagnosticAnalyzerService;
        _documentTrackingService = workspace.Services.GetRequiredService<IDocumentTrackingService>();

        workspace.WorkspaceChanged += Workspace_WorkspaceChanged;
    }

    private readonly ConcurrentDictionary<IReadOnlyList<AnalyzerReference>, Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>>> _diagnosticDescriptorCache = new();

    private readonly VisualStudioWorkspace _workspace;
    private readonly IThreadingContext _threadingContext;
    private readonly IGlobalOptionService _globalOptions;
    private readonly IDocumentTrackingService _documentTrackingService;
    private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        foreach (var project in _workspace.CurrentSolution.Projects)
            KickOffBackgroundComputation(project, cancellationToken);

        return Task.CompletedTask;
    }

    private void Workspace_WorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
    {
        switch (e.Kind)
        {
            case WorkspaceChangeKind.SolutionAdded:
            case WorkspaceChangeKind.SolutionChanged:
            case WorkspaceChangeKind.SolutionReloaded:
                foreach (var project in e.NewSolution.Projects)
                    KickOffBackgroundComputation(project, CancellationToken.None);
                break;

            case WorkspaceChangeKind.AdditionalDocumentAdded:
            case WorkspaceChangeKind.AdditionalDocumentReloaded:
            case WorkspaceChangeKind.AdditionalDocumentChanged:
            case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
            case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                KickOffBackgroundComputation(e.NewSolution.GetProject(e.ProjectId), CancellationToken.None);
                break;
        }
    }

    private void KickOffBackgroundComputation(Project? project, CancellationToken cancellationToken)
    {
        if (project == null)
            return;

        Task.Run(async () =>
        {
            // Add tasks for config data computation for all features here.
            await GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: true, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    public Task<ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>> TryGetCodeAnalysisSuggestionsConfigDataAsync(Project project, CancellationToken cancellationToken)
        => GetCodeAnalysisSuggestionsConfigDataAsync(project, forceCompute: false, cancellationToken);

    private async Task<ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>> GetCodeAnalysisSuggestionsConfigDataAsync(Project project, bool forceCompute, CancellationToken cancellationToken)
    {
        var summary = AnalyzerConfigSummaryHelper.GetAnalyzerConfigSummary(project, _globalOptions);
        if (summary == null)
            return ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>.Empty;

        var descriptorsByCategory = await GetAvailableDiagnosticDescriptorsByCategoryAsync(project, forceCompute).ConfigureAwait(false);
        if (descriptorsByCategory.IsEmpty)
            return ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>.Empty;

        var computedDiagnostics = await GetAvailableDiagnosticsAsync(project, forceCompute, cancellationToken).ConfigureAwait(false);
        return GetCodeAnalysisSuggestionsByCategory(summary, project, descriptorsByCategory, computedDiagnostics, _globalOptions);
    }

    private static ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)> GetCodeAnalysisSuggestionsByCategory(
        FirstPartyAnalyzerConfigSummary configSummary,
        Project project,
        ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> descriptorsByCategory,
        ImmutableArray<DiagnosticData> computedDiagnostics,
        IGlobalOptionService globalOptions)
    {
        const int MaxIdsToSuggestPerCategoryWhenNoComputedDiagnostics = 5;

        var enableCodeQuality = ShouldShowSuggestions(configSummary, codeQuality: true, globalOptions, out var codeQualityConfiguredDiagnosticIds);
        var enableCodeStyle = ShouldShowSuggestions(configSummary, codeQuality: true, globalOptions, out var codeStyleConfiguredDiagnosticIds);
        if (!enableCodeQuality && !enableCodeStyle)
            return ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>.Empty;

        using var _1 = ArrayBuilder<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)>.GetInstance(out var builder);
        using var _2 = PooledDictionary<string, ImmutableArray<DiagnosticData>>.GetInstance(out var diagnosticsByIdBuilder);
        using var _3 = PooledHashSet<string>.GetInstance(out var uniqueIds);

        if (!computedDiagnostics.IsEmpty)
        {
            foreach (var (category, diagnosticsForCategory) in computedDiagnostics.GroupBy(d => d.Category).OrderBy(g => g.Key))
            {
                var orderedDiagnostics = diagnosticsForCategory.GroupBy(d => d.Id).OrderByDescending(group => group.Count());
                foreach (var (id, diagnostics) in orderedDiagnostics)
                {
                    if (!IsIdAlreadyProcessedOrExplicitlyConfigured(id))
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
        }
        else
        {
            foreach (var (category, descriptorsForCategory) in descriptorsByCategory)
            {
                foreach (var descriptor in descriptorsForCategory)
                {
                    if (!IsIdAlreadyProcessedOrExplicitlyConfigured(descriptor.Id))
                    {
                        var diagnostic = Diagnostic.Create(descriptor, Location.None);
                        var diagnosticData = DiagnosticData.Create(project.Solution, diagnostic, project);
                        diagnosticsByIdBuilder.Add(descriptor.Id, ImmutableArray.Create(diagnosticData));

                        if (diagnosticsByIdBuilder.Count == MaxIdsToSuggestPerCategoryWhenNoComputedDiagnostics)
                            break;
                    }
                }

                if (diagnosticsByIdBuilder.Count > 0)
                {
                    builder.Add((category, diagnosticsByIdBuilder.ToImmutableDictionary()));
                    diagnosticsByIdBuilder.Clear();
                }
            }
        }

        return builder.ToImmutable();

        static bool ShouldShowSuggestions(
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

        bool IsIdAlreadyProcessedOrExplicitlyConfigured(string id)
        {
            if (!uniqueIds.Add(id))
            {
                return true;
            }

            // Only suggest diagnostics that are not explicitly configured in analyzer config files
            if (enableCodeQuality
                && Regex.IsMatch(id, configSummary.CodeQualitySummary.DiagnosticIdPattern)
                && !configSummary.CodeQualitySummary.ConfiguredDiagnosticIds.Contains(id))
            {
                return false;
            }

            if (enableCodeStyle
                && Regex.IsMatch(id, configSummary.CodeStyleSummary.DiagnosticIdPattern)
                && !configSummary.CodeStyleSummary.ConfiguredDiagnosticIds.Contains(id))
            {
                return false;
            }

            return true;
        }
    }

    private async Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>> GetAvailableDiagnosticDescriptorsByCategoryAsync(Project project, bool forceCompute)
    {
        if (!_diagnosticDescriptorCache.TryGetValue(project.AnalyzerReferences, out var task))
        {
            task = new Task<ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>>(() => GetAllAvailableAnalyzerDescriptors(project), _threadingContext.DisposalToken);

            var returnedTask = _diagnosticDescriptorCache.GetOrAdd(project.AnalyzerReferences, task);
            if (returnedTask == task)
                task.Start();

            task = returnedTask;
        }

        if (task.IsCompleted || forceCompute)
            return await task.ConfigureAwait(false);

        return ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>>.Empty;

        ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> GetAllAvailableAnalyzerDescriptors(Project project)
        {
            // include host analyzers
            var refToAnalyzersMap = project.Solution.State.Analyzers.CreateDiagnosticAnalyzersPerReference(project);
            var analyzers = refToAnalyzersMap.SelectMany(kvp => kvp.Value);
            var descriptors = analyzers.SelectMany(analyzer => analyzer.IsCompilerAnalyzer()
                ? ImmutableArray<DiagnosticDescriptor>.Empty
                : _diagnosticAnalyzerService.AnalyzerInfoCache.GetDiagnosticDescriptors(analyzer));

            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<DiagnosticDescriptor>>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptorGroup in descriptors.GroupBy(d => d.Category))
            {
                builder.Add(descriptorGroup.Key, descriptorGroup.OrderByDescending(d => d.Id).ToImmutableArray());
            }

            return builder.ToImmutable();
            ;

        }
    }

    private async Task<ImmutableArray<DiagnosticData>> GetAvailableDiagnosticsAsync(Project project, bool forceCompute, CancellationToken cancellationToken)
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

        return ImmutableArray<DiagnosticData>.Empty;

        static bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer) => !analyzer.IsCompilerAnalyzer();
    }

    public void Dispose()
    {
        _workspace.WorkspaceChanged -= Workspace_WorkspaceChanged;
    }
}
