// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    using ProjectErrorMap = ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>>;

    internal sealed class ExternalErrorDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IGlobalOperationNotificationService _notificationService;

        private readonly TaskQueue _taskQueue;

        private readonly object _gate = new object();
        private InProgressState? _stateDoNotAccessDirectly;
        private ImmutableArray<DiagnosticData> _lastBuiltResult = ImmutableArray<DiagnosticData>.Empty;

        public ExternalErrorDiagnosticUpdateSource(
            VisualStudioWorkspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListenerProvider listenerProvider)
            : this(workspace, diagnosticService, listenerProvider.GetListener(FeatureAttribute.ErrorList))
        {
            registrationService.Register(this);
        }

        /// <summary>
        /// internal for testing
        /// </summary>
        internal ExternalErrorDiagnosticUpdateSource(
            Workspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IAsynchronousOperationListener listener)
        {
            // use queue to serialize work. no lock needed
            _taskQueue = new TaskQueue(listener, TaskScheduler.Default);

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _diagnosticService = diagnosticService;

            _notificationService = _workspace.Services.GetRequiredService<IGlobalOperationNotificationService>();
        }

        public event EventHandler<BuildProgress>? BuildProgressChanged;
        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        public bool IsInProgress => BuildInprogressState != null;

        public ImmutableArray<DiagnosticData> GetBuildErrors()
            => _lastBuiltResult;

        public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
            => BuildInprogressState?.IsSupportedDiagnosticId(projectId, id) ?? false;

        private void OnBuildProgressChanged(InProgressState? state, BuildProgress buildProgress)
        {
            if (state != null)
            {
                _lastBuiltResult = state.GetBuildErrors();
            }

            RaiseBuildProgressChanged(buildProgress);
        }

        public void ClearErrors(ProjectId projectId)
        {
            // capture state if it exists
            var state = BuildInprogressState;

            _taskQueue.ScheduleTask(nameof(ClearErrors), async () =>
            {
                if (state == null)
                {
                    await ClearErrorsCoreAsync(projectId, _workspace.CurrentSolution, state).ConfigureAwait(false);
                }
                else
                {
                    if (state.WereProjectErrorsCleared(projectId))
                    {
                        return;
                    }

                    var solution = state.Solution;

                    // Clear errors for the given project.
                    await ClearErrorsCoreAsync(projectId, solution, state).ConfigureAwait(false);

                    // Additionally, clear errors for all projects that transitively depend on this project.
                    // Otherwise, fixing errors in core projects in dependency chain will leave back stale diagnostics in dependent projects.
                    var transitiveProjectIds = solution.GetProjectDependencyGraph().GetProjectsThatTransitivelyDependOnThisProject(projectId);
                    foreach (var projectId in transitiveProjectIds)
                    {
                        if (state.WereProjectErrorsCleared(projectId))
                        {
                            continue;
                        }

                        await ClearErrorsCoreAsync(projectId, solution, state).ConfigureAwait(false);
                    }
                }
            }, CancellationToken.None);

            return;

            async Task ClearErrorsCoreAsync(ProjectId projectId, Solution solution, InProgressState? state)
            {
                Debug.Assert(state == null || !state.WereProjectErrorsCleared(projectId));

                // Clear build-only errors for project
                ClearBuildOnlyProjectErrors(solution, projectId);

                // Clear live errors for project
                await SetLiveErrorsForProjectAsync(projectId, ImmutableArray<DiagnosticData>.Empty).ConfigureAwait(false);

                // Mark projects as having its error cleared.
                state?.MarkErrorsCleared(projectId);

                // Update build progress to refresh error list
                OnBuildProgressChanged(state, BuildProgress.Updated);
            }
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                    _taskQueue.ScheduleTask("OnSolutionChanged", () => e.OldSolution.ProjectIds.Do(p => ClearBuildOnlyProjectErrors(e.OldSolution, p)), CancellationToken.None);
                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectReloaded:
                    _taskQueue.ScheduleTask("OnProjectChanged", () => ClearBuildOnlyProjectErrors(e.OldSolution, e.ProjectId), CancellationToken.None);
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                    _taskQueue.ScheduleTask("OnDocumentRemoved", () => ClearBuildOnlyDocumentErrors(e.OldSolution, e.ProjectId, e.DocumentId), CancellationToken.None);
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(e.Kind);
            }
        }

        internal void OnSolutionBuildStarted()
        {
            // build just started, create the state and fire build in progress event.
            // build just started, create the state and fire build in progress event.
            _ = GetOrCreateInProgressState();
        }

        internal void OnSolutionBuildCompleted()
        {
            // building is done. reset the state
            // and get local copy of in-progress state
            var inProgressState = ClearInProgressState();

            // enqueue build/live sync in the queue.
            _taskQueue.ScheduleTask("OnSolutionBuild", async () =>
            {
                // nothing to do
                if (inProgressState == null)
                {
                    return;
                }

                // explicitly start solution crawler if it didn't start yet. since solution crawler is lazy, 
                // user might have built solution before workspace fires its first event yet (which is when solution crawler is initialized)
                // here we give initializeLazily: false so that solution crawler is fully initialized when we do de-dup live and build errors,
                // otherwise, we will think none of error we have here belong to live errors since diagnostic service is not initialized yet.
                var registrationService = (SolutionCrawlerRegistrationService)_workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                registrationService.EnsureRegistration(_workspace, initializeLazily: false);

                // Mark the status as updated to refresh error list before we invoke 'SyncBuildErrorsAndReportAsync', which can take some time to complete.
                OnBuildProgressChanged(inProgressState, BuildProgress.Updated);

                // we are about to update live analyzer data using one from build.
                // pause live analyzer
                using var operation = _notificationService.Start("BuildDone");
                if (_diagnosticService is DiagnosticAnalyzerService diagnosticService)
                {
                    await SyncBuildErrorsAndReportOnBuildCompletedAsync(diagnosticService, inProgressState).ConfigureAwait(false);
                }

                // Mark build as complete.
                OnBuildProgressChanged(inProgressState, BuildProgress.Done);
            }, CancellationToken.None);
        }

        private async Task SyncBuildErrorsAndReportOnBuildCompletedAsync(DiagnosticAnalyzerService diagnosticService, InProgressState inProgressState)
        {
            var solution = inProgressState.Solution;
            var (allLiveErrors, pendingLiveErrorsToSync) = await inProgressState.GetLiveErrorsAsync().ConfigureAwait(false);

            // make those errors live errors
            await diagnosticService.SynchronizeWithBuildAsync(_workspace, pendingLiveErrorsToSync, onBuildCompleted: true).ConfigureAwait(false);

            // raise events for ones left-out
            var buildErrors = GetBuildErrors().Except(allLiveErrors).GroupBy(k => k.DocumentId);
            foreach (var group in buildErrors)
            {
                if (group.Key == null)
                {
                    foreach (var projectGroup in group.GroupBy(g => g.ProjectId))
                    {
                        Contract.ThrowIfNull(projectGroup.Key);
                        ReportBuildErrors(projectGroup.Key, solution, projectGroup.ToImmutableArray());
                    }

                    continue;
                }

                ReportBuildErrors(group.Key, solution, group.ToImmutableArray());
            }
        }

        private void ReportBuildErrors<T>(T item, Solution solution, ImmutableArray<DiagnosticData> buildErrors)
        {
            if (item is ProjectId projectId)
            {
                RaiseDiagnosticsCreated(projectId, solution, projectId, null, buildErrors);
                return;
            }

            // must be not null
            var documentId = item as DocumentId;
            RaiseDiagnosticsCreated(documentId, solution, documentId!.ProjectId, documentId, buildErrors);
        }

        private void ClearBuildOnlyProjectErrors(Solution solution, ProjectId? projectId)
        {
            // remove all project errors
            RaiseDiagnosticsRemoved(projectId, solution, projectId, documentId: null);

            var project = solution.GetProject(projectId);
            if (project == null)
            {
                return;
            }

            // remove all document errors
            foreach (var documentId in project.DocumentIds)
            {
                ClearBuildOnlyDocumentErrors(solution, projectId, documentId);
            }
        }

        private void ClearBuildOnlyDocumentErrors(Solution solution, ProjectId? projectId, DocumentId? documentId)
            => RaiseDiagnosticsRemoved(documentId, solution, projectId, documentId);

        public void AddNewErrors(ProjectId projectId, DiagnosticData diagnostic)
        {
            // capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Project New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(projectId, state).ConfigureAwait(false);
                state.AddError(projectId, diagnostic);
            }, CancellationToken.None);
        }

        public void AddNewErrors(DocumentId documentId, DiagnosticData diagnostic)
        {
            // capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Document New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(documentId.ProjectId, state).ConfigureAwait(false);
                state.AddError(documentId, diagnostic);
            }, CancellationToken.None);
        }

        public void AddNewErrors(
            ProjectId projectId, HashSet<DiagnosticData> projectErrors, Dictionary<DocumentId, HashSet<DiagnosticData>> documentErrorMap)
        {
            // capture state that will be processed in background thread
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Project New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(projectId, state).ConfigureAwait(false);

                foreach (var kv in documentErrorMap)
                {
                    state.AddErrors(kv.Key, kv.Value);
                }

                state.AddErrors(projectId, projectErrors);
            }, CancellationToken.None);
        }

        private async Task ReportPreviousProjectErrorsIfRequiredAsync(ProjectId projectId, InProgressState state)
        {
            if (state.TryGetLastProjectWithReportedErrors() is ProjectId lastProjectId &&
                lastProjectId != projectId)
            {
                // We received errors for a different project.
                // Reports errors for lastProjectId as its live errors.
                await SetLiveErrorsForProjectAsync(lastProjectId, state).ConfigureAwait(false);
            }
        }

        private async Task SetLiveErrorsForProjectAsync(ProjectId projectId, InProgressState state)
        {
            var diagnostics = await state.GetLiveErrorsForProjectAsync(projectId).ConfigureAwait(false);
            await SetLiveErrorsForProjectAsync(projectId, diagnostics).ConfigureAwait(false);
            state.MarkLiveErrorsReported(projectId);
        }

        private async Task SetLiveErrorsForProjectAsync(ProjectId projectId, ImmutableArray<DiagnosticData> diagnostics)
        {
            if (_diagnosticService is DiagnosticAnalyzerService diagnosticAnalyzerService)
            {
                // make those errors live errors
                var map = ProjectErrorMap.Empty.Add(projectId, diagnostics);
                await diagnosticAnalyzerService.SynchronizeWithBuildAsync(_workspace, map, onBuildCompleted: false).ConfigureAwait(false);
            }
        }

        private InProgressState? BuildInprogressState
        {
            get
            {
                lock (_gate)
                {
                    return _stateDoNotAccessDirectly;
                }
            }
        }

        private InProgressState? ClearInProgressState()
        {
            lock (_gate)
            {
                var state = _stateDoNotAccessDirectly;

                _stateDoNotAccessDirectly = null;
                return state;
            }
        }

        private InProgressState GetOrCreateInProgressState()
        {
            lock (_gate)
            {
                if (_stateDoNotAccessDirectly == null)
                {
                    // here, we take current snapshot of solution when the state is first created. and through out this code, we use this snapshot.
                    // since we have no idea what actual snapshot of solution the out of proc build has picked up, it doesn't remove the race we can have
                    // between build and diagnostic service, but this at least make us to consistent inside of our code.
                    _stateDoNotAccessDirectly = new InProgressState(this, _workspace.CurrentSolution);
                    OnBuildProgressChanged(_stateDoNotAccessDirectly, BuildProgress.Started);
                }

                return _stateDoNotAccessDirectly;
            }
        }

        private void RaiseDiagnosticsCreated(object? id, Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableArray<DiagnosticData> items)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId, items));
        }

        private void RaiseDiagnosticsRemoved(object? id, Solution solution, ProjectId? projectId, DocumentId? documentId)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId));
        }

        private static ArgumentKey CreateArgumentKey(object? id) => new ArgumentKey(id);

        private void RaiseBuildProgressChanged(BuildProgress progress)
            => BuildProgressChanged?.Invoke(this, progress);

        #region not supported
        public bool SupportGetDiagnostics { get { return false; } }

        public ImmutableArray<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }
        #endregion

        internal enum BuildProgress
        {
            Started,
            Updated,
            Done
        }

        private sealed class InProgressState
        {
            private readonly ExternalErrorDiagnosticUpdateSource _owner;

            private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>> _allDiagnosticIdMap = new ConcurrentDictionary<ProjectId, ImmutableHashSet<string>>();
            private readonly ConcurrentDictionary<ProjectId, ImmutableHashSet<string>> _liveDiagnosticIdMap = new ConcurrentDictionary<ProjectId, ImmutableHashSet<string>>();

            // Fields that are used only from APIs invoked from serialized task queue, hence don't need to be thread safe.
            private readonly Dictionary<ProjectId, Dictionary<DiagnosticData, int>> _projectMap = new Dictionary<ProjectId, Dictionary<DiagnosticData, int>>();
            private readonly Dictionary<DocumentId, Dictionary<DiagnosticData, int>> _documentMap = new Dictionary<DocumentId, Dictionary<DiagnosticData, int>>();
            private readonly HashSet<ProjectId> _projectsWithErrorsCleared = new HashSet<ProjectId>();
            private readonly HashSet<ProjectId> _projectsWithAllLiveErrorsReported = new HashSet<ProjectId>();
            private readonly HashSet<ProjectId> _projectsWithErrors = new HashSet<ProjectId>();
            private ProjectId? _lastProjectWithReportedErrors;
            private int _incrementDoNotAccessDirectly;

            public InProgressState(ExternalErrorDiagnosticUpdateSource owner, Solution solution)
            {
                _owner = owner;
                Solution = solution;
            }

            public Solution Solution { get; }

            public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
                => GetOrCreateSupportedDiagnosticIds(projectId).Contains(id);

            private ImmutableHashSet<string> GetOrCreateSupportedDiagnosticIds(ProjectId projectId)
            {
                if (_allDiagnosticIdMap.TryGetValue(projectId, out var ids))
                {
                    return ids;
                }

                var computedIds = ComputeSupportedDiagnosticIds(projectId, Solution, _owner._diagnosticService);
                return _allDiagnosticIdMap.GetOrAdd(projectId, computedIds);

                static ImmutableHashSet<string> ComputeSupportedDiagnosticIds(ProjectId projectId, Solution solution, IDiagnosticAnalyzerService diagnosticService)
                {
                    var project = solution.GetProject(projectId);
                    if (project == null)
                    {
                        // projectId no longer exist
                        return ImmutableHashSet<string>.Empty;
                    }

                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var descriptorMap = solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(diagnosticService.AnalyzerInfoCache, project);
                    builder.UnionWith(descriptorMap.Values.SelectMany(v => v.Select(d => d.Id)));

                    return builder.ToImmutable();
                }
            }

            public ImmutableArray<DiagnosticData> GetBuildErrors()
            {
                // return errors in the order that is reported
                return ImmutableArray.CreateRange(
                    _projectMap.Values.SelectMany(d => d).Concat(_documentMap.Values.SelectMany(d => d)).OrderBy(kv => kv.Value).Select(kv => kv.Key));
            }

            public void MarkErrorsCleared(ProjectId projectId)
            {
                var added = _projectsWithErrorsCleared.Add(projectId);
                Debug.Assert(added);
            }

            public bool WereProjectErrorsCleared(ProjectId projectId)
                => _projectsWithErrorsCleared.Contains(projectId);

            public void MarkLiveErrorsReported(ProjectId projectId)
                => _projectsWithAllLiveErrorsReported.Add(projectId);

            public ProjectId? TryGetLastProjectWithReportedErrors()
                => _lastProjectWithReportedErrors;

            private IEnumerable<ProjectId> GetProjectsWithErrors()
            {
                // filter out project that is no longer exist in IDE
                // this can happen if user started a "build" and then remove a project from IDE
                // before build finishes
                return _projectsWithErrors.Where(p => Solution.GetProject(p) != null);
            }

            public async Task<(ImmutableArray<DiagnosticData> allLiveErrors, ProjectErrorMap pendingLiveErrorsToSync)> GetLiveErrorsAsync()
            {
                var allLiveErrorsBuilder = ImmutableArray.CreateBuilder<DiagnosticData>();
                var pendingLiveErrorsToSyncBuilder = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableArray<DiagnosticData>>();
                foreach (var projectId in GetProjectsWithErrors())
                {
                    var errors = await GetLiveErrorsForProjectAsync(projectId).ConfigureAwait(false);
                    allLiveErrorsBuilder.AddRange(errors);

                    if (!_projectsWithAllLiveErrorsReported.Contains(projectId))
                    {
                        pendingLiveErrorsToSyncBuilder.Add(projectId, errors);
                    }
                }

                return (allLiveErrorsBuilder.ToImmutable(), pendingLiveErrorsToSyncBuilder.ToImmutable());
            }

            public async Task<ImmutableArray<DiagnosticData>> GetLiveErrorsForProjectAsync(ProjectId projectId)
            {
                var project = Solution.GetRequiredProject(projectId);

                var diagnostics = _projectMap.Where(kv => kv.Key == projectId).SelectMany(kv => kv.Value).Concat(
                        _documentMap.Where(kv => kv.Key.ProjectId == projectId).SelectMany(kv => kv.Value));
                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);
                foreach (var (diagnostic, _) in diagnostics)
                {
                    if (await IsLiveAsync(project, diagnostic).ConfigureAwait(false))
                    {
                        builder.Add(diagnostic);
                    }
                }

                return builder.ToImmutable();
            }

            public void AddErrors(DocumentId key, HashSet<DiagnosticData> diagnostics)
                => AddErrors(_documentMap, key, diagnostics);

            public void AddErrors(ProjectId key, HashSet<DiagnosticData> diagnostics)
                => AddErrors(_projectMap, key, diagnostics);

            public void AddError(DocumentId key, DiagnosticData diagnostic)
                => AddError(_documentMap, key, diagnostic);

            public void AddError(ProjectId key, DiagnosticData diagnostic)
                => AddError(_projectMap, key, diagnostic);

            private async Task<bool> IsLiveAsync(Project project, DiagnosticData diagnosticData)
            {
                // REVIEW: current design is that we special case compiler analyzer case and we accept only document level
                //         diagnostic as live. otherwise, we let them be build errors. we changed compiler analyzer accordingly as well
                //         so that it doesn't report project level diagnostic as live errors.
                if (!IsDocumentLevelDiagnostic(diagnosticData) &&
                    diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Compiler))
                {
                    // compiler error but project level error
                    return false;
                }

                if (await IsSupportedLiveDiagnosticIdAsync(project, diagnosticData.Id).ConfigureAwait(false))
                {
                    return true;
                }

                return false;

                static bool IsDocumentLevelDiagnostic(DiagnosticData diagnosticData)
                {
                    if (diagnosticData.DocumentId != null)
                    {
                        return true;
                    }

                    // due to mapped file such as
                    //
                    // A.cs having
                    // #line 2 RandomeFile.txt
                    //       ErrorHere
                    // #line default
                    //
                    // we can't simply say it is not document level diagnostic since
                    // file path is not part of solution. build output will just tell us 
                    // mapped span not original span, so any code like above will not
                    // part of solution.
                    // 
                    // but also we can't simply say it is a document level error because it has file path
                    // since project level error can have a file path pointing to a file such as dll
                    // , pdb, embedded files and etc.
                    // 
                    // unfortunately, there is no 100% correct way to do this.
                    // so we will use a heuristic that will most likely work for most of common cases.
                    return diagnosticData.DataLocation != null &&
                        !string.IsNullOrEmpty(diagnosticData.DataLocation.OriginalFilePath) &&
                        (diagnosticData.DataLocation.OriginalStartLine > 0 ||
                         diagnosticData.DataLocation.OriginalStartColumn > 0);
                }
            }

            private async Task<bool> IsSupportedLiveDiagnosticIdAsync(Project project, string id)
                => (await GetOrCreateSupportedLiveDiagnosticsAsync(project).ConfigureAwait(false)).Contains(id);

            private async Task<ImmutableHashSet<string>> GetOrCreateSupportedLiveDiagnosticsAsync(Project project)
            {
                if (_liveDiagnosticIdMap.TryGetValue(project.Id, out var ids))
                {
                    return ids;
                }

                var computedIds = await ComputeSupportedLiveDiagnosticIdsAsync().ConfigureAwait(false);
                return _liveDiagnosticIdMap.GetOrAdd(project.Id, computedIds);

                async Task<ImmutableHashSet<string>> ComputeSupportedLiveDiagnosticIdsAsync()
                {
                    var fullSolutionAnalysis = SolutionCrawlerOptions.GetBackgroundAnalysisScope(project) == BackgroundAnalysisScope.FullSolution;
                    if (!project.SupportsCompilation || fullSolutionAnalysis)
                    {
                        return GetOrCreateSupportedDiagnosticIds(project.Id);
                    }

                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var diagnosticService = _owner._diagnosticService;
                    var infoCache = diagnosticService.AnalyzerInfoCache;

                    foreach (var analyzersPerReference in project.Solution.State.Analyzers.CreateDiagnosticAnalyzersPerReference(project))
                    {
                        foreach (var analyzer in analyzersPerReference.Value)
                        {
                            if (await diagnosticService.IsCompilationEndAnalyzerAsync(analyzer, project, CancellationToken.None).ConfigureAwait(false))
                            {
                                continue;
                            }

                            var diagnosticIds = infoCache.GetDiagnosticDescriptors(analyzer).Select(d => d.Id);
                            builder.UnionWith(diagnosticIds);
                        }
                    }

                    return builder.ToImmutable();
                }
            }

            private void AddErrors<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key, HashSet<DiagnosticData> diagnostics)
                where T : notnull
            {
                var errors = GetErrorSet(map, key);
                foreach (var diagnostic in diagnostics)
                {
                    AddError(errors, diagnostic, key);
                }
            }

            private void AddError<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key, DiagnosticData diagnostic)
                where T : notnull
            {
                var errors = GetErrorSet(map, key);
                AddError(errors, diagnostic, key);
            }

            private void AddError<T>(Dictionary<DiagnosticData, int> errors, DiagnosticData diagnostic, T key)
                where T : notnull
            {
                RecordProjectContainsErrors();

                // add only new errors
                if (!errors.TryGetValue(diagnostic, out _))
                {
                    Logger.Log(FunctionId.ExternalErrorDiagnosticUpdateSource_AddError, d => d.ToString(), diagnostic);

                    errors.Add(diagnostic, _incrementDoNotAccessDirectly++);
                }

                return;

                void RecordProjectContainsErrors()
                {
                    RoslynDebug.Assert(key is DocumentId || key is ProjectId);
                    var projectId = (key is DocumentId documentId) ? documentId.ProjectId : (ProjectId)(object)key;

                    // New errors reported for project, need to refresh live errors.
                    _projectsWithAllLiveErrorsReported.Remove(projectId);

                    if (!_projectsWithErrors.Add(projectId))
                    {
                        return;
                    }

                    // this will make build only error list to be updated per project rather than per solution.
                    // basically this will make errors up to last project to show up in error list
                    _lastProjectWithReportedErrors = projectId;
                    _owner.OnBuildProgressChanged(this, BuildProgress.Updated);
                }
            }

            private static Dictionary<DiagnosticData, int> GetErrorSet<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key)
                where T : notnull
                => map.GetOrAdd(key, _ => new Dictionary<DiagnosticData, int>(DiagnosticDataComparer.Instance));
        }

        private sealed class ArgumentKey : BuildToolId.Base<object>
        {
            public ArgumentKey(object? key) : base(key)
            {
            }

            public override string BuildTool
            {
                get { return PredefinedBuildTools.Build; }
            }

            public override bool Equals(object? obj)
                => obj is ArgumentKey &&
                   base.Equals(obj);

            public override int GetHashCode()
                => base.GetHashCode();
        }

        private sealed class DiagnosticDataComparer : IEqualityComparer<DiagnosticData>
        {
            public static readonly DiagnosticDataComparer Instance = new DiagnosticDataComparer();

            public bool Equals(DiagnosticData item1, DiagnosticData item2)
            {
                if ((item1.DocumentId == null) != (item2.DocumentId == null) ||
                    item1.Id != item2.Id ||
                    item1.ProjectId != item2.ProjectId ||
                    item1.Severity != item2.Severity ||
                    item1.Message != item2.Message ||
                    (item1.DataLocation?.MappedStartLine ?? 0) != (item2.DataLocation?.MappedStartLine ?? 0) ||
                    (item1.DataLocation?.MappedStartColumn ?? 0) != (item2.DataLocation?.MappedStartColumn ?? 0) ||
                    (item1.DataLocation?.OriginalStartLine ?? 0) != (item2.DataLocation?.OriginalStartLine ?? 0) ||
                    (item1.DataLocation?.OriginalStartColumn ?? 0) != (item2.DataLocation?.OriginalStartColumn ?? 0))
                {
                    return false;
                }

                return (item1.DocumentId != null) ?
                    item1.DocumentId == item2.DocumentId :
                    item1.DataLocation?.OriginalFilePath == item2.DataLocation?.OriginalFilePath;
            }

            public int GetHashCode(DiagnosticData obj)
            {
                var result =
                    Hash.Combine(obj.Id,
                    Hash.Combine(obj.Message,
                    Hash.Combine(obj.ProjectId,
                    Hash.Combine(obj.DataLocation?.MappedStartLine ?? 0,
                    Hash.Combine(obj.DataLocation?.MappedStartColumn ?? 0,
                    Hash.Combine(obj.DataLocation?.OriginalStartLine ?? 0,
                    Hash.Combine(obj.DataLocation?.OriginalStartColumn ?? 0, (int)obj.Severity)))))));

                return obj.DocumentId != null ?
                    Hash.Combine(obj.DocumentId, result) :
                    Hash.Combine(obj.DataLocation?.OriginalFilePath?.GetHashCode() ?? 0, result);
            }
        }
    }
}
