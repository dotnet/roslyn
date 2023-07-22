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
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation;
using Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Suppression
{
    /// <summary>
    /// Service to compute and apply bulk suppression fixes.
    /// </summary>
    [Export(typeof(IVisualStudioSuppressionFixService))]
    [Export(typeof(VisualStudioSuppressionFixService))]
    internal sealed class VisualStudioSuppressionFixService : IVisualStudioSuppressionFixService
    {
        private readonly IThreadingContext _threadingContext;
        private readonly VisualStudioWorkspaceImpl _workspace;
        private readonly IAsynchronousOperationListener _listener;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly ExternalErrorDiagnosticUpdateSource _buildErrorDiagnosticService;
        private readonly ICodeFixService _codeFixService;
        private readonly IFixMultipleOccurrencesService _fixMultipleOccurencesService;
        private readonly ICodeActionEditHandlerService _editHandlerService;
        private readonly VisualStudioDiagnosticListSuppressionStateService _suppressionStateService;
        private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IHierarchyItemToProjectIdMap _projectMap;
        private readonly IGlobalOptionService _globalOptions;

        private IWpfTableControl? _tableControl;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioSuppressionFixService(
            IThreadingContext threadingContext,
            SVsServiceProvider serviceProvider,
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticAnalyzerService diagnosticService,
            ICodeFixService codeFixService,
            ICodeActionEditHandlerService editHandlerService,
            VisualStudioDiagnosticListSuppressionStateService suppressionStateService,
            IUIThreadOperationExecutor uiThreadOperationExecutor,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IAsynchronousOperationListenerProvider listenerProvider,
            IGlobalOptionService globalOptions)
        {
            _threadingContext = threadingContext;
            _workspace = workspace;
            _diagnosticService = diagnosticService;
            _buildErrorDiagnosticService = workspace.ExternalErrorDiagnosticUpdateSource;
            _codeFixService = codeFixService;
            _suppressionStateService = suppressionStateService;
            _editHandlerService = editHandlerService;
            _uiThreadOperationExecutor = uiThreadOperationExecutor;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _fixMultipleOccurencesService = workspace.Services.GetRequiredService<IFixMultipleOccurrencesService>();
            _projectMap = workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
            _listener = listenerProvider.GetListener(FeatureAttribute.ErrorList);
            _globalOptions = globalOptions;
        }

        public async Task InitializeAsync(IAsyncServiceProvider serviceProvider)
        {
            var errorList = await serviceProvider.GetServiceAsync<SVsErrorList, IErrorList>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false);
            _tableControl = errorList?.TableControl;
        }

        public bool AddSuppressions(IVsHierarchy? projectHierarchy)
        {
            if (_tableControl == null)
            {
                return false;
            }

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);

            // Apply suppressions fix in global suppressions file for non-compiler diagnostics and
            // in source only for compiler diagnostics.
            var diagnosticsToFix = GetDiagnosticsToFix(shouldFixInProject, selectedEntriesOnly: false, isAddSuppression: true);
            if (!ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics: false, isAddSuppression: true, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: false))
            {
                return false;
            }

            return ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics: false, isAddSuppression: true, isSuppressionInSource: true, onlyCompilerDiagnostics: true, showPreviewChangesDialog: false);
        }

        public bool AddSuppressions(bool selectedErrorListEntriesOnly, bool suppressInSource, IVsHierarchy? projectHierarchy)
        {
            if (_tableControl == null)
            {
                return false;
            }

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);
            return ApplySuppressionFix(shouldFixInProject, selectedErrorListEntriesOnly, isAddSuppression: true, isSuppressionInSource: suppressInSource, onlyCompilerDiagnostics: false, showPreviewChangesDialog: true);
        }

        public bool RemoveSuppressions(bool selectedErrorListEntriesOnly, IVsHierarchy? projectHierarchy)
        {
            if (_tableControl == null)
            {
                return false;
            }

            var shouldFixInProject = GetShouldFixInProjectDelegate(_vsHierarchyItemManager, _projectMap, projectHierarchy);
            return ApplySuppressionFix(shouldFixInProject, selectedErrorListEntriesOnly, isAddSuppression: false, isSuppressionInSource: false, onlyCompilerDiagnostics: false, showPreviewChangesDialog: true);
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

        private async Task<ImmutableArray<DiagnosticData>> GetAllBuildDiagnosticsAsync(Func<Project, bool> shouldFixInProject, CancellationToken cancellationToken)
        {
            using var _ = CodeAnalysis.PooledObjects.ArrayBuilder<DiagnosticData>.GetInstance(out var builder);

            var buildDiagnostics = _buildErrorDiagnosticService.GetBuildErrors().Where(d => d.ProjectId != null && d.Severity != DiagnosticSeverity.Hidden);
            var solution = _workspace.CurrentSolution;
            foreach (var diagnosticsByProject in buildDiagnostics.GroupBy(d => d.ProjectId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (diagnosticsByProject.Key == null)
                {
                    // Diagnostics with no projectId cannot be suppressed.
                    continue;
                }

                var project = solution.GetProject(diagnosticsByProject.Key);
                if (project != null && shouldFixInProject(project))
                {
                    var diagnosticsByDocument = diagnosticsByProject.GroupBy(d => d.DocumentId);
                    foreach (var group in diagnosticsByDocument)
                    {
                        var documentId = group.Key;
                        if (documentId == null)
                        {
                            // Project diagnostics, just add all of them.
                            builder.AddRange(group);
                            continue;
                        }

                        // For document diagnostics ensure that whatever was reported is placed at a valid location for
                        // the state the document is currently in.
                        var document = project.GetDocument(documentId);
                        if (document != null)
                        {
                            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                            var text = await tree.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            foreach (var diagnostic in group)
                            {
                                var span = diagnostic.DataLocation.UnmappedFileSpan.GetClampedTextSpan(text);
                                builder.Add(diagnostic.WithLocations(diagnostic.DataLocation.WithSpan(span, tree), additionalLocations: default));
                            }
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static string GetFixTitle(bool isAddSuppression)
            => isAddSuppression ? ServicesVSResources.Suppress_diagnostics : ServicesVSResources.Remove_suppressions;

        private static string GetWaitDialogMessage(bool isAddSuppression)
            => isAddSuppression ? ServicesVSResources.Computing_suppressions_fix : ServicesVSResources.Computing_remove_suppressions_fix;

        private IEnumerable<DiagnosticData>? GetDiagnosticsToFix(Func<Project, bool> shouldFixInProject, bool selectedEntriesOnly, bool isAddSuppression)
        {
            var diagnosticsToFix = ImmutableHashSet<DiagnosticData>.Empty;
            void computeDiagnosticsToFix(IUIThreadOperationContext context)
            {
                var cancellationToken = context.UserCancellationToken;

                // If we are fixing selected diagnostics in error list, then get the diagnostics from error list entry
                // snapshots. Otherwise, get all diagnostics from the diagnostic service.
                var diagnosticsToFixTask = selectedEntriesOnly
                    ? _suppressionStateService.GetSelectedItemsAsync(isAddSuppression, cancellationToken)
                    : GetAllBuildDiagnosticsAsync(shouldFixInProject, cancellationToken);

                diagnosticsToFix = diagnosticsToFixTask.WaitAndGetResult(cancellationToken).ToImmutableHashSet();
            }

            var title = GetFixTitle(isAddSuppression);
            var waitDialogMessage = GetWaitDialogMessage(isAddSuppression);
            var result = InvokeWithWaitDialog(computeDiagnosticsToFix, title, waitDialogMessage);

            // Bail out if the user cancelled.
            if (result == UIThreadOperationStatus.Canceled)
            {
                return null;
            }

            return diagnosticsToFix;
        }

        private bool ApplySuppressionFix(Func<Project, bool> shouldFixInProject, bool selectedEntriesOnly, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
        {
            var diagnosticsToFix = GetDiagnosticsToFix(shouldFixInProject, selectedEntriesOnly, isAddSuppression);
            return ApplySuppressionFix(diagnosticsToFix, shouldFixInProject, selectedEntriesOnly, isAddSuppression, isSuppressionInSource, onlyCompilerDiagnostics, showPreviewChangesDialog);
        }

        private bool ApplySuppressionFix(IEnumerable<DiagnosticData>? diagnosticsToFix, Func<Project, bool> shouldFixInProject, bool filterStaleDiagnostics, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
        {
            _ = ApplySuppressionFixAsync(diagnosticsToFix, shouldFixInProject, filterStaleDiagnostics, isAddSuppression, isSuppressionInSource, onlyCompilerDiagnostics, showPreviewChangesDialog);
            return true;
        }

        private async Task ApplySuppressionFixAsync(IEnumerable<DiagnosticData>? diagnosticsToFix, Func<Project, bool> shouldFixInProject, bool filterStaleDiagnostics, bool isAddSuppression, bool isSuppressionInSource, bool onlyCompilerDiagnostics, bool showPreviewChangesDialog)
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

                foreach (var languageService in languageServices)
                {
                    // Use the Fix multiple occurrences service to compute a bulk suppression fix for the specified document and project diagnostics,
                    // show a preview changes dialog and then apply the fix to the workspace.

                    cancellationToken.ThrowIfCancellationRequested();

                    var language = languageService.Language;
                    var options = _globalOptions.GetCodeActionOptions(languageService);
                    var optionsProvider = options.CreateProvider();

                    var documentDiagnosticsPerLanguage = GetDocumentDiagnosticsMappedToNewSolution(documentDiagnosticsToFixMap, newSolution, language);
                    if (!documentDiagnosticsPerLanguage.IsEmpty)
                    {
                        var suppressionFixer = GetSuppressionFixer(documentDiagnosticsPerLanguage.SelectMany(kvp => kvp.Value), language, _codeFixService);
                        if (suppressionFixer != null)
                        {
                            var suppressionFixAllProvider = suppressionFixer.GetFixAllProvider();
                            newSolution = _fixMultipleOccurencesService.GetFix(
                                documentDiagnosticsPerLanguage,
                                _workspace,
                                suppressionFixer,
                                suppressionFixAllProvider,
                                optionsProvider,
                                equivalenceKey,
                                title,
                                waitDialogMessage,
                                cancellationToken);
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
                        if (suppressionFixer != null)
                        {
                            var suppressionFixAllProvider = suppressionFixer.GetFixAllProvider();
                            newSolution = _fixMultipleOccurencesService.GetFix(
                                 projectDiagnosticsPerLanguage,
                                 _workspace,
                                 suppressionFixer,
                                 suppressionFixAllProvider,
                                 optionsProvider,
                                 equivalenceKey,
                                 title,
                                 waitDialogMessage,
                                 cancellationToken);
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
                        newSolution = FixAllGetFixesService.PreviewChanges(
                            _workspace.CurrentSolution,
                            newSolution,
                            fixAllPreviewChangesTitle: title,
                            fixAllTopLevelHeader: title,
                            fixAllKind: FixAllKind.CodeFix,
                            languageOpt: languageServices?.Count == 1 ? languageServices.Single().Language : null,
                            workspace: _workspace);
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
                        operations: operations,
                        title: title,
                        progressTracker: new UIThreadOperationContextProgressTracker(scope),
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    // Kick off diagnostic re-analysis for affected projects so that diagnostics gets refreshed.
                    _ = Task.Run(() =>
                    {
                        var reanalyzeDocuments = diagnosticsToFix.Select(d => d.DocumentId).WhereNotNull().Distinct();
                        _diagnosticService.Reanalyze(_workspace, projectIds: null, documentIds: reanalyzeDocuments, highPriority: true);
                    });
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

        private UIThreadOperationStatus InvokeWithWaitDialog(
            Action<IUIThreadOperationContext> action, string waitDialogTitle, string waitDialogMessage)
        {
            var cancelled = false;
            var result = _uiThreadOperationExecutor.Execute(
                waitDialogTitle,
                waitDialogMessage,
                allowCancellation: true,
                showProgress: true,
                action: waitContext =>
                {
                    try
                    {
                        action(waitContext);
                    }
                    catch (OperationCanceledException)
                    {
                        cancelled = true;
                    }
                });

            return cancelled ? UIThreadOperationStatus.Canceled : result;
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
                    diagnosticsPerDocument = new List<DiagnosticData>();
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
                    var latestProjectDiagnostics = (await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null,
                        diagnosticIds: uniqueDiagnosticIds, shouldIncludeAnalyzer: null, includeSuppressedDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken)
                        .ConfigureAwait(false)).Where(IsDocumentDiagnostic);

                    latestDocumentDiagnosticsMap.Clear();
                    foreach (var kvp in latestProjectDiagnostics.Where(d => d.DocumentId != null).GroupBy(d => d.DocumentId!))
                    {
                        latestDocumentDiagnosticsMap.Add(kvp.Key, kvp.ToImmutableHashSet());
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
                            latestDocumentDiagnostics = ImmutableHashSet<DiagnosticData>.Empty;
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
            var builder = ImmutableDictionary.CreateBuilder<ProjectId, List<DiagnosticData>>();
            foreach (var diagnosticData in diagnosticsToFix.Where(IsProjectDiagnostic))
            {
                RoslynDebug.AssertNotNull(diagnosticData.ProjectId);

                if (!builder.TryGetValue(diagnosticData.ProjectId, out var diagnosticsPerProject))
                {
                    diagnosticsPerProject = new List<DiagnosticData>();
                    builder[diagnosticData.ProjectId] = diagnosticsPerProject;
                }

                diagnosticsPerProject.Add(diagnosticData);
            }

            if (builder.Count == 0)
            {
                return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
            }

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
                    var latestDiagnosticsFromDiagnosticService = (await _diagnosticService.GetDiagnosticsForIdsAsync(project.Solution, project.Id, documentId: null,
                        diagnosticIds: uniqueDiagnosticIds, shouldIncludeAnalyzer: null, includeSuppressedDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken)
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
            static bool IsProjectDiagnostic(DiagnosticData d) => d.DataLocation == null && d.ProjectId != null;
        }
    }
}
