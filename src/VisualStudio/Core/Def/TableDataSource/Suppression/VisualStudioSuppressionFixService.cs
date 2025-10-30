// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Progress;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Suppression;

/// <summary>
/// Service to compute and apply bulk suppression fixes.
/// </summary>
[Export(typeof(IVisualStudioSuppressionFixService))]
[Export(typeof(VisualStudioSuppressionFixService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioSuppressionFixService(
    IThreadingContext threadingContext,
    VisualStudioWorkspaceImpl workspace,
    ICodeFixService codeFixService,
    ICodeActionEditHandlerService editHandlerService,
    VisualStudioDiagnosticListSuppressionStateService suppressionStateService,
    IUIThreadOperationExecutor uiThreadOperationExecutor,
    IVsHierarchyItemManager vsHierarchyItemManager,
    IAsynchronousOperationListenerProvider listenerProvider) : IVisualStudioSuppressionFixService
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly VisualStudioWorkspaceImpl _workspace = workspace;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.ErrorList);
    private readonly ICodeFixService _codeFixService = codeFixService;
    private readonly IFixMultipleOccurrencesService _fixMultipleOccurencesService = workspace.Services.GetRequiredService<IFixMultipleOccurrencesService>();
    private readonly ICodeActionEditHandlerService _editHandlerService = editHandlerService;
    private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService = suppressionStateService;
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor = uiThreadOperationExecutor;
    private readonly IVsHierarchyItemManager _vsHierarchyItemManager = vsHierarchyItemManager;
    private readonly IHierarchyItemToProjectIdMap _projectMap = workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();

    private IWpfTableControl? _tableControl;

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var errorList = await serviceProvider.GetServiceAsync<SVsErrorList, IErrorList>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        _tableControl = errorList?.TableControl;
    }

    public bool AddSuppressions(IVsHierarchy? projectHierarchy)
    {
        return _threadingContext.JoinableTaskFactory.Run(async () =>
        {
            if (_tableControl == null)
                return false;

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);

            // Apply suppressions fix in global suppressions file for non-compiler diagnostics and
            // in source only for compiler diagnostics.
            var diagnosticsToFix = await GetDiagnosticsToFixAsync(selectedEntriesOnly: false, isAddSuppression: true).ConfigureAwait(true);
            if (!ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics: false, isAddSuppression: true, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: false))
                return false;

            return ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics: false, isAddSuppression: true, isSuppressionInSource: true, onlyCompilerDiagnostics: true, showPreviewChangesDialog: false);
        });
    }

    public bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy? projectHierarchy)
    {
        return _threadingContext.JoinableTaskFactory.Run(async () =>
        {
            if (_tableControl == null)
                return false;

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);
            return await ApplySuppressionFixAsync(
                shouldFixInProject,
                selectedErrorListEntriesOnly,
                isAddSuppression: true,
                isSuppressionInSource: suppressInSource,
                onlyCompilerDiagnostics: false,
                showPreviewChangesDialog: true).ConfigureAwait(true);
        });
    }

    public bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy? projectHierarchy)
    {
        return _threadingContext.JoinableTaskFactory.Run(async () =>
        {
            if (_tableControl == null)
                return false;

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);
            return await ApplySuppressionFixAsync(
                shouldFixInProject,
                selectedErrorListEntriesOnly,
                isAddSuppression: false,
                isSuppressionInSource: false,
                onlyCompilerDiagnostics: false,
                showPreviewChangesDialog: true).ConfigureAwait(true);
        });
    }

    private static Func<Project, bool> GetShouldFixInProjectDelegate(IVsHierarchyItemManager vsHierarchyItemManager, IHierarchyItemToProjectIdMap projectMap, IVsHierarchy? projectHierarchy)
    {
        ProjectId? projectIdToMatch = null;
        if (projectHierarchy != null)
        {
            var projectHierarchyItem = vsHierarchyItemManager.GetHierarchyItem(projectHierarchy, VSConstants.VSITEMID_ROOT);
            if (projectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out var projectId))
            {
                projectIdToMatch = projectId;
            }
        }

        return p => projectHierarchy == null || p.Id == projectIdToMatch;
    }

    private static string GetFixTitle(bool isAddSuppression)
        => isAddSuppression ? ServicesVSResources.Suppress_diagnostics : ServicesVSResources.Remove_suppressions;

    private static string GetWaitDialogMessage(bool isAddSuppression)
        => isAddSuppression ? ServicesVSResources.Computing_suppressions_fix : ServicesVSResources.Computing_remove_suppressions_fix;

    private async Task<ImmutableHashSet<DiagnosticData>?> GetDiagnosticsToFixAsync(
        bool selectedEntriesOnly,
        bool isAddSuppression)
    {
        var diagnosticsToFix = ImmutableHashSet<DiagnosticData>.Empty;

        var result = await InvokeWithWaitDialogAsync(async cancellationToken =>
        {
            // If we are fixing selected diagnostics in error list, then get the diagnostics from error list entry
            // snapshots. Otherwise, get all diagnostics from the diagnostic service.
            var diagnosticsToFixArray = selectedEntriesOnly
                ? await _suppressionStateService.GetSelectedItemsAsync(isAddSuppression, cancellationToken).ConfigureAwait(true)
                : [];

            diagnosticsToFix = [.. diagnosticsToFixArray];

        }, GetFixTitle(isAddSuppression), GetWaitDialogMessage(isAddSuppression)).ConfigureAwait(true);

        if (result == UIThreadOperationStatus.Canceled)
            return null;

        return diagnosticsToFix;
    }

    private async Task<bool> ApplySuppressionFixAsync(
        Func<Project, bool> shouldFixInProject,
        bool selectedEntriesOnly,
        bool isAddSuppression,
        bool isSuppressionInSource,
        bool onlyCompilerDiagnostics,
        bool showPreviewChangesDialog)
    {
        var diagnosticsToFix = await GetDiagnosticsToFixAsync(selectedEntriesOnly, isAddSuppression).ConfigureAwait(true);
        return ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, selectedEntriesOnly, isAddSuppression, isSuppressionInSource, onlyCompilerDiagnostics, showPreviewChangesDialog);
    }

    private bool ApplySuppressionFix(IEnumerable<DiagnosticData>? diagnosticsToFix, Func<Project, bool> shouldFixInProject, bool filterStaleDiagnostics, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
    {
        _ = ApplySuppressionFixAsync(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics, isAddSuppression, isSuppressionInSource, onlyCompilerDiagnostics, showPreviewChangesDialog);
        return true;
    }

    private async Task ApplySuppressionFixAsync(
        IEnumerable<DiagnosticData>? diagnosticsToFix,
        Func<Project, bool> shouldFixInProject,
        bool filterStaleDiagnostics,
        bool isAddSuppression,
        bool isSuppressionInSource,
        bool onlyCompilerDiagnostics,
        bool showPreviewChangesDialog)
    {
        try
        {
            using var token = _listener.BeginAsyncOperation(nameof(ApplySuppressionFix));

            var originalSolution = _workspace.CurrentSolution;
            var title = GetFixTitle(isAddSuppression);
            var waitDialogMessage = GetWaitDialogMessage(isAddSuppression);

            using var context = _uiThreadOperationExecutor.BeginExecute(
                title,
                waitDialogMessage,
                allowCancellation: true,
                showProgress: true);

            if (diagnosticsToFix == null)
                return;

            diagnosticsToFix = FilterDiagnostics(diagnosticsToFix, isAddSuppression, isSuppressionInSource, onlyCompilerDiagnostics);
            if (diagnosticsToFix.IsEmpty())
                return;

            var newSolution = _workspace.CurrentSolution;

            var cancellationToken = context.UserCancellationToken;
            cancellationToken.ThrowIfCancellationRequested();

            var documentDiagnosticsToFixMap = await GetDocumentDiagnosticsToFixAsync(
                diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            var projectDiagnosticsToFixMap = isSuppressionInSource
                ? ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty
                : await GetProjectDiagnosticsToFixAsync(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics, cancellationToken).ConfigureAwait(false);

            if (documentDiagnosticsToFixMap == null ||
                projectDiagnosticsToFixMap == null ||
                (documentDiagnosticsToFixMap.IsEmpty && projectDiagnosticsToFixMap.IsEmpty))
            {
                // Nothing to fix.
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Equivalence key determines what fix will be applied.
            // Make sure we don't include any specific diagnostic ID, as we want all of the given diagnostics (which can have varied ID) to be fixed.
            var equivalenceKey = isAddSuppression
                ? (isSuppressionInSource ? FeaturesResources.in_Source : FeaturesResources.in_Suppression_File)
                : FeaturesResources.Remove_Suppression;

            // We have different suppression fixers for every language.
            // So we need to group diagnostics by the containing project language and apply fixes separately.
            var languageServices = projectDiagnosticsToFixMap.Select(p => p.Key.Services).Concat(documentDiagnosticsToFixMap.Select(kvp => kvp.Key.Project.Services)).ToHashSet();

            var progress = context.GetCodeAnalysisProgress();
            foreach (var languageService in languageServices)
            {
                // Use the Fix multiple occurrences service to compute a bulk suppression fix for the specified document and project diagnostics,
                // show a preview changes dialog and then apply the fix to the workspace.

                cancellationToken.ThrowIfCancellationRequested();

                var language = languageService.Language;

                var documentDiagnosticsPerLanguage = GetDocumentDiagnosticsMappedToNewSolution(documentDiagnosticsToFixMap, newSolution, language);
                if (!documentDiagnosticsPerLanguage.IsEmpty)
                {
                    var suppressionFixer = GetSuppressionFixer(documentDiagnosticsPerLanguage.SelectMany(kvp => kvp.Value), language, _codeFixService);
                    var suppressionFixAllProvider = suppressionFixer?.GetFixAllProvider();
                    if (suppressionFixer != null && suppressionFixAllProvider != null)
                    {
                        newSolution = await _fixMultipleOccurencesService.GetFixAsync(
                            documentDiagnosticsPerLanguage,
                            _workspace,
                            suppressionFixer,
                            suppressionFixAllProvider,
                            equivalenceKey,
                            title,
                            waitDialogMessage,
                            progress,
                            cancellationToken).ConfigureAwait(false);
                        if (newSolution == null)
                        {
                            // User cancelled or fixer threw an exception, so we just bail out.
                            return;
                        }
                    }
                }

                var projectDiagnosticsPerLanguage = GetProjectDiagnosticsMappedToNewSolution(projectDiagnosticsToFixMap, newSolution, language);
                if (!projectDiagnosticsPerLanguage.IsEmpty)
                {
                    var suppressionFixer = GetSuppressionFixer(projectDiagnosticsPerLanguage.SelectMany(kvp => kvp.Value), language, _codeFixService);
                    var suppressionFixAllProvider = suppressionFixer?.GetFixAllProvider();
                    if (suppressionFixer != null && suppressionFixAllProvider != null)
                    {
                        newSolution = await _fixMultipleOccurencesService.GetFixAsync(
                             projectDiagnosticsPerLanguage,
                             _workspace,
                             suppressionFixer,
                             suppressionFixAllProvider,
                             equivalenceKey,
                             title,
                             waitDialogMessage,
                             progress,
                             cancellationToken).ConfigureAwait(false);
                        if (newSolution == null)
                        {
                            return;
                        }
                    }
                }

                if (newSolution == _workspace.CurrentSolution)
                {
                    // No changes.
                    return;
                }

                if (showPreviewChangesDialog)
                {
                    var fixAllService = newSolution.Services.GetRequiredService<IFixAllGetFixesService>();
                    newSolution = fixAllService.PreviewChanges(
                        _workspace,
                        _workspace.CurrentSolution,
                        newSolution,
                        fixAllKind: FixAllKind.CodeFix,
                        previewChangesTitle: title,
                        topLevelHeader: title,
                        language: languageServices?.Count == 1 ? languageServices.Single().Language : null,
                        correlationId: null,
                        cancellationToken);
                    if (newSolution == null)
                    {
                        return;
                    }
                }

                waitDialogMessage = isAddSuppression ? ServicesVSResources.Applying_suppressions_fix : ServicesVSResources.Applying_remove_suppressions_fix;
                var operations = ImmutableArray.Create<CodeActionOperation>(new ApplyChangesOperation(newSolution));
                using var scope = context.AddScope(allowCancellation: true, description: waitDialogMessage);
                await _editHandlerService.ApplyAsync(
                    _workspace,
                    originalSolution,
                    fromDocument: null,
                    operations,
                    title,
                    scope.GetCodeAnalysisProgress(),
                    cancellationToken).ConfigureAwait(false);

                // Kick off diagnostic re-analysis for affected projects so that diagnostics gets refreshed.
                _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>().RequestDiagnosticRefresh();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
        {
        }
    }

    private static IEnumerable<DiagnosticData> FilterDiagnostics(IEnumerable<DiagnosticData> diagnostics, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics)
    {
        foreach (var diagnostic in diagnostics)
        {
            var isCompilerDiagnostic = SuppressionHelpers.IsCompilerDiagnostic(diagnostic);
            if (onlyCompilerDiagnostics && !isCompilerDiagnostic)
            {
                continue;
            }

            if (isAddSuppression)
            {
                // Compiler diagnostics can only be suppressed in source.
                if (!diagnostic.IsSuppressed &&
                    (isSuppressionInSource || !isCompilerDiagnostic))
                {
                    yield return diagnostic;
                }
            }
            else if (diagnostic.IsSuppressed)
            {
                yield return diagnostic;
            }
        }
    }

    private async Task<UIThreadOperationStatus> InvokeWithWaitDialogAsync(
        Func<CancellationToken, Task> action, string waitDialogTitle, string waitDialogMessage)
    {
        using var waitContext = _uiThreadOperationExecutor.BeginExecute(waitDialogTitle, waitDialogMessage, allowCancellation: true, showProgress: true);

        var cancelled = false;
        try
        {
            await action(waitContext.UserCancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        return cancelled ? UIThreadOperationStatus.Canceled : UIThreadOperationStatus.Completed;
    }

    private static ImmutableDictionary<Document, ImmutableArray<Diagnostic>> GetDocumentDiagnosticsMappedToNewSolution(ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentDiagnosticsToFixMap, Solution newSolution, string language)
    {
        ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Builder? builder = null;
        foreach (var (oldDocument, diagnostics) in documentDiagnosticsToFixMap)
        {
            if (oldDocument.Project.Language != language)
                continue;

            var document = newSolution.GetDocument(oldDocument.Id);
            if (document != null)
            {
                builder ??= ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
                builder.Add(document, diagnostics);
            }
        }

        return builder != null ? builder.ToImmutable() : ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
    }

    private static ImmutableDictionary<Project, ImmutableArray<Diagnostic>> GetProjectDiagnosticsMappedToNewSolution(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> projectDiagnosticsToFixMap, Solution newSolution, string language)
    {
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Builder? projectDiagsBuilder = null;
        foreach (var (oldProject, diagnostics) in projectDiagnosticsToFixMap)
        {
            if (oldProject.Language != language)
                continue;

            var project = newSolution.GetProject(oldProject.Id);
            if (project != null)
            {
                projectDiagsBuilder ??= ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();
                projectDiagsBuilder.Add(project, diagnostics);
            }
        }

        return projectDiagsBuilder != null ? projectDiagsBuilder.ToImmutable() : ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
    }

    private static CodeFixProvider? GetSuppressionFixer(IEnumerable<Diagnostic> diagnostics, string language, ICodeFixService codeFixService)
    {
        // Fetch the suppression fixer to apply the fix.
        return codeFixService.GetSuppressionFixer(language, diagnostics.Select(d => d.Id));
    }

    private async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(IEnumerable<DiagnosticData> diagnosticsToFix, Func<Project, bool> shouldFixInProject, bool filterStaleDiagnostics, CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<DocumentId, List<DiagnosticData>>();
        foreach (var diagnosticData in diagnosticsToFix.Where(IsDocumentDiagnostic))
        {
            RoslynDebug.AssertNotNull(diagnosticData.DocumentId);

            if (!builder.TryGetValue(diagnosticData.DocumentId, out var diagnosticsPerDocument))
            {
                diagnosticsPerDocument = [];
                builder[diagnosticData.DocumentId] = diagnosticsPerDocument;
            }

            diagnosticsPerDocument.Add(diagnosticData);
        }

        if (builder.Count == 0)
        {
            return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
        }

        var finalBuilder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
        var latestDocumentDiagnosticsMap = filterStaleDiagnostics ? new Dictionary<DocumentId, ImmutableHashSet<DiagnosticData>>() : null;
        foreach (var group in builder.GroupBy(kvp => kvp.Key.ProjectId))
        {
            var projectId = group.Key;
            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project == null || !shouldFixInProject(project))
            {
                continue;
            }

            if (filterStaleDiagnostics)
            {
                RoslynDebug.AssertNotNull(latestDocumentDiagnosticsMap);

                var uniqueDiagnosticIds = group.SelectMany(kvp => kvp.Value.Select(d => d.Id)).ToImmutableHashSet();
                var diagnosticService = _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var latestProjectDiagnostics = (await diagnosticService.GetDiagnosticsForIdsAsync(
                    project, documentIds: default, diagnosticIds: uniqueDiagnosticIds, AnalyzerFilter.All, includeLocalDocumentDiagnostics: true, cancellationToken)
                    .ConfigureAwait(false)).Where(IsDocumentDiagnostic);

                latestDocumentDiagnosticsMap.Clear();
                foreach (var kvp in latestProjectDiagnostics.Where(d => d.DocumentId != null).GroupBy(d => d.DocumentId!))
                {
                    latestDocumentDiagnosticsMap.Add(kvp.Key, [.. kvp]);
                }
            }

            foreach (var documentDiagnostics in group)
            {
                var document = project.GetDocument(documentDiagnostics.Key);
                if (document == null)
                {
                    continue;
                }

                IEnumerable<DiagnosticData> documentDiagnosticsToFix;
                if (filterStaleDiagnostics)
                {
                    RoslynDebug.AssertNotNull(latestDocumentDiagnosticsMap);

                    if (!latestDocumentDiagnosticsMap.TryGetValue(document.Id, out var latestDocumentDiagnostics))
                    {
                        // Ignore stale diagnostics in error list.
                        latestDocumentDiagnostics = [];
                    }

                    // Filter out stale diagnostics in error list.
                    documentDiagnosticsToFix = documentDiagnostics.Value
                        .Where(d => latestDocumentDiagnostics.Contains(d) ||
                                    d.IsBuildDiagnostic() ||
                                    SuppressionHelpers.IsSynthesizedExternalSourceDiagnostic(d));
                }
                else
                {
                    documentDiagnosticsToFix = documentDiagnostics.Value;
                }

                if (documentDiagnosticsToFix.Any())
                {
                    var diagnostics = await documentDiagnosticsToFix.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
                    finalBuilder.Add(document, diagnostics);
                }
            }
        }

        return finalBuilder.ToImmutableDictionary();

        // Local functions
        static bool IsDocumentDiagnostic(DiagnosticData d) => d.DocumentId != null;
    }

    private async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(IEnumerable<DiagnosticData> diagnosticsToFix, Func<Project, bool> shouldFixInProject, bool filterStaleDiagnostics, CancellationToken cancellationToken)
    {
        using var _ = CodeAnalysis.PooledObjects.PooledDictionary<ProjectId, CodeAnalysis.PooledObjects.ArrayBuilder<DiagnosticData>>.GetInstance(out var builder);
        foreach (var diagnosticData in diagnosticsToFix.Where(IsProjectDiagnostic))
            builder.MultiAdd(diagnosticData.ProjectId, diagnosticData);

        if (builder.Count == 0)
            return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;

        var finalBuilder = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();
        var latestDiagnosticsToFix = filterStaleDiagnostics ? new HashSet<DiagnosticData>() : null;
        foreach (var (projectId, diagnostics) in builder)
        {
            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project == null || !shouldFixInProject(project))
                continue;

            IEnumerable<DiagnosticData> projectDiagnosticsToFix;
            if (filterStaleDiagnostics)
            {
                RoslynDebug.AssertNotNull(latestDiagnosticsToFix);

                var uniqueDiagnosticIds = diagnostics.Select(d => d.Id).ToImmutableHashSet();
                var diagnosticService = _workspace.Services.GetRequiredService<IDiagnosticAnalyzerService>();
                var latestDiagnosticsFromDiagnosticService = (await diagnosticService.GetDiagnosticsForIdsAsync(
                    project, documentIds: default, diagnosticIds: uniqueDiagnosticIds, AnalyzerFilter.All, includeLocalDocumentDiagnostics: true, cancellationToken)
                    .ConfigureAwait(false));

                latestDiagnosticsToFix.Clear();
                latestDiagnosticsToFix.AddRange(latestDiagnosticsFromDiagnosticService.Where(IsProjectDiagnostic));

                // Filter out stale diagnostics in error list.
                projectDiagnosticsToFix = diagnostics.Where(d => latestDiagnosticsFromDiagnosticService.Contains(d) || SuppressionHelpers.IsSynthesizedExternalSourceDiagnostic(d));
            }
            else
            {
                projectDiagnosticsToFix = diagnostics;
            }

            if (projectDiagnosticsToFix.Any())
            {
                var projectDiagnostics = await projectDiagnosticsToFix.ToDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
                finalBuilder.Add(project, projectDiagnostics);
            }
        }

        return finalBuilder.ToImmutableDictionary();

        // Local functions
        static bool IsProjectDiagnostic(DiagnosticData d) => d.DataLocation == null;
    }
}
