// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

#pragma warning disable CA1200 // Avoid using cref tags with a prefix

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    using ProjectErrorMap = ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>>;

    /// <summary>
    /// Diagnostic source for warnings and errors reported from explicit build command invocations in Visual Studio.
    /// VS workspaces calls into us when a build is invoked or completed in Visual Studio.
    /// <see cref="ProjectExternalErrorReporter"/> calls into us to clear reported diagnostics or to report new diagnostics during the build.
    /// For each of these callbacks, we create/capture the current <see cref="GetBuildInProgressState()"/> and
    /// schedule updating/processing this state on a serialized <see cref="_taskQueue"/> in the background.
    /// The processing phase de-dupes the diagnostics reported from build and intellisense to ensure that the error list does not contain duplicate diagnostics.
    /// It raises events about diagnostic updates, which eventually trigger the "Build + Intellisense" and "Build only" error list diagnostic
    /// sources to update the reported diagnostics.
    /// </summary>
    internal sealed class ExternalErrorDiagnosticUpdateSource : IDiagnosticUpdateSource, IDisposable
    {
        private readonly Workspace _workspace;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IBuildOnlyDiagnosticsService _buildOnlyDiagnosticsService;
        private readonly IGlobalOperationNotificationService _notificationService;
        private readonly CancellationToken _disposalToken;

        /// <summary>
        /// Task queue to serialize all the work for errors reported by build.
        /// <see cref="_stateDoNotAccessDirectly"/> represents the state from build errors,
        /// which is built up and processed in serialized fashion on this task queue.
        /// </summary>
        private readonly TaskQueue _taskQueue;

        /// <summary>
        /// Task queue to serialize all the post-build and post error list refresh tasks.
        /// Error list refresh requires build/live diagnostics de-duping to complete, which happens during
        /// <see cref="SyncBuildErrorsAndReportOnBuildCompletedAsync(DiagnosticAnalyzerService, InProgressState)"/>.
        /// Computationally expensive tasks such as writing build errors into persistent storage,
        /// invoking background analysis on open files/solution after build completes, etc.
        /// are added to this task queue to help ensure faster error list refresh.
        /// </summary>
        private readonly TaskQueue _postBuildAndErrorListRefreshTaskQueue;

        // Gate for concurrent access and fields guarded with this gate.
        private readonly object _gate = new();
        private InProgressState? _stateDoNotAccessDirectly;
        private readonly CancellationSeries _activeCancellationSeriesDoNotAccessDirectly = new();

        /// <summary>
        /// Latest diagnostics reported during current or last build.
        /// These are not the de-duped build/live diagnostics, but the actual diagnostics from build.
        /// They are directly used by the "Build only" error list setting.
        /// </summary>
        private ImmutableArray<DiagnosticData> _lastBuiltResult = ImmutableArray<DiagnosticData>.Empty;

        public ExternalErrorDiagnosticUpdateSource(
            VisualStudioWorkspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IGlobalOperationNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
            : this(workspace, diagnosticService, notificationService, listenerProvider.GetListener(FeatureAttribute.ErrorList), threadingContext.DisposalToken)
        {
            registrationService.Register(this);
        }

        /// <summary>
        /// internal for testing
        /// </summary>
        internal ExternalErrorDiagnosticUpdateSource(
            Workspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IGlobalOperationNotificationService notificationService,
            IAsynchronousOperationListener listener,
            CancellationToken disposalToken)
        {
            // use queue to serialize work. no lock needed
            _taskQueue = new TaskQueue(listener, TaskScheduler.Default);
            _postBuildAndErrorListRefreshTaskQueue = new TaskQueue(listener, TaskScheduler.Default);
            _disposalToken = disposalToken;

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _diagnosticService = diagnosticService;
            _buildOnlyDiagnosticsService = _workspace.Services.GetRequiredService<IBuildOnlyDiagnosticsService>();

            _notificationService = notificationService;
        }

        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache => _diagnosticService.AnalyzerInfoCache;

        /// <summary>
        /// Event generated from the serialized <see cref="_taskQueue"/> whenever the build progress in Visual Studio changes.
        /// Events are guaranteed to be generated in a serial fashion, but may be invoked on any thread.
        /// </summary>
        public event EventHandler<BuildProgress>? BuildProgressChanged;

        /// <summary>
        /// Event generated from the serialized <see cref="_taskQueue"/> whenever build-only diagnostics are reported during a build in Visual Studio.
        /// These diagnostics are not supported from intellisense and only get refreshed during actual build.
        /// </summary>
        public event EventHandler<DiagnosticsUpdatedArgs>? DiagnosticsUpdated;

        /// <summary>
        /// Event generated from the serialized <see cref="_taskQueue"/> whenever build-only diagnostics are cleared during a build in Visual Studio.
        /// These diagnostics are not supported from intellisense and only get refreshed during actual build.
        /// </summary>
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        /// <summary>
        /// Indicates if a build is currently in progress inside Visual Studio.
        /// </summary>
        public bool IsInProgress => GetBuildInProgressState() != null;

        public void Dispose()
        {
            lock (_gate)
            {
                _activeCancellationSeriesDoNotAccessDirectly.Dispose();
            }
        }

        /// <summary>
        /// Get the latest diagnostics reported during current or last build.
        /// These are not the de-duped build/live diagnostics, but the actual diagnostics from build.
        /// They are directly used by the "Build only" error list setting.
        /// </summary>
        public ImmutableArray<DiagnosticData> GetBuildErrors()
            => _lastBuiltResult;

        /// <summary>
        /// Returns true if the given <paramref name="id"/> represents an analyzer diagnostic ID that could be reported
        /// for the given <paramref name="projectId"/> during the current build in progress.
        /// This API is only intended to be invoked from <see cref="ProjectExternalErrorReporter"/> while a build is in progress.
        /// </summary>
        public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
            => GetBuildInProgressState()?.IsSupportedDiagnosticId(projectId, id) ?? false;

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
            // Capture state if it exists
            var state = GetBuildInProgressState();

            // Update the state to clear diagnostics and raise corresponding diagnostic updated events
            // on a serialized task queue.
            _taskQueue.ScheduleTask(nameof(ClearErrors), async () =>
            {
                if (state == null)
                {
                    // TODO: Is it possible that ClearErrors can be invoked while the build is not in progress?
                    // We fallback to current solution in the workspace and clear errors for the project.
                    await ClearErrorsCoreAsync(projectId, _workspace.CurrentSolution, state).ConfigureAwait(false);
                }
                else
                {
                    // We are going to clear the diagnostics for the current project.
                    // Additionally, we clear errors for all projects that transitively depend on this project.
                    // Otherwise, fixing errors in core projects in dependency chain will leave back stale diagnostics in dependent projects.

                    // First check if we already cleared the diagnostics for this project when processing a referenced project.
                    // If so, we don't need to clear diagnostics for it again.
                    if (state.WereProjectErrorsCleared(projectId))
                    {
                        return;
                    }

                    var solution = state.Solution;

                    await ClearErrorsCoreAsync(projectId, solution, state).ConfigureAwait(false);

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
            }, GetApplicableCancellationToken(state));

            return;

            async ValueTask ClearErrorsCoreAsync(ProjectId projectId, Solution solution, InProgressState? state)
            {
                Debug.Assert(state == null || !state.WereProjectErrorsCleared(projectId));

                // Here, we clear the build and live errors for the project.
                // Additionally, we mark projects as having its errors cleared.
                // This ensures that we do not attempt to clear the diagnostics again for the same project
                // when 'ClearErrors' is invoked for multiple dependent projects.
                // Finally, we update build progress state so error list gets refreshed.

                ClearBuildOnlyProjectErrors(solution, projectId);

                await SetLiveErrorsForProjectAsync(projectId, ImmutableArray<DiagnosticData>.Empty, GetApplicableCancellationToken(state)).ConfigureAwait(false);

                state?.MarkErrorsCleared(projectId);

                OnBuildProgressChanged(state, BuildProgress.Updated);
            }
        }

        // internal for testing purposes only.
        internal void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            // Clear relevant build-only errors on workspace events such as solution added/removed/reloaded,
            // project added/removed/reloaded, etc.
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                    _taskQueue.ScheduleTask("OnSolutionAdded", () => e.OldSolution.ProjectIds.Do(p => ClearBuildOnlyProjectErrors(e.OldSolution, p)), _disposalToken);
                    break;

                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                    _taskQueue.ScheduleTask("OnSolutionChanged", () => e.OldSolution.ProjectIds.Do(p => ClearBuildOnlyProjectErrors(e.OldSolution, p)), _disposalToken);
                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectReloaded:
                    _taskQueue.ScheduleTask("OnProjectChanged", () => ClearBuildOnlyProjectErrors(e.OldSolution, e.ProjectId), _disposalToken);
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                case WorkspaceChangeKind.AdditionalDocumentRemoved:
                case WorkspaceChangeKind.AdditionalDocumentReloaded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    _taskQueue.ScheduleTask("OnDocumentRemoved", () => ClearBuildOnlyDocumentErrors(e.OldSolution, e.ProjectId, e.DocumentId), _disposalToken);
                    break;

                case WorkspaceChangeKind.DocumentChanged:
                case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                case WorkspaceChangeKind.AdditionalDocumentChanged:
                    // We clear build-only errors for the document on document edits.
                    // This is done to address multiple customer reports of stale build-only diagnostics
                    // after they fix/remove the code flagged from build-only diagnostics, but the diagnostics
                    // do not get automatically removed/refreshed while typing.
                    // See https://github.com/dotnet/docs/issues/26708 and https://github.com/dotnet/roslyn/issues/64659
                    // for additional details.
                    _taskQueue.ScheduleTask("OnDocumentChanged", () => ClearBuildOnlyDocumentErrors(e.OldSolution, e.ProjectId, e.DocumentId), _disposalToken);
                    break;

                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.DocumentAdded:
                case WorkspaceChangeKind.ProjectChanged:
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.AdditionalDocumentAdded:
                case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(e.Kind);
            }
        }

        internal void OnSolutionBuildStarted()
        {
            // Build just started, create the state and fire build in progress event.
            _ = GetOrCreateInProgressState();
        }

        internal void OnSolutionBuildCompleted()
        {
            // Building is done, so reset the state
            // and get local copy of in-progress state.
            var inProgressState = ClearInProgressState();

            // Enqueue build/live sync in the queue.
            _taskQueue.ScheduleTask("OnSolutionBuild", async () =>
            {
                try
                {
                    // nothing to do
                    if (inProgressState == null)
                    {
                        return;
                    }

                    // Explicitly start solution crawler if it didn't start yet. since solution crawler is lazy, 
                    // user might have built solution before workspace fires its first event yet (which is when solution crawler is initialized)
                    // here we give initializeLazily: false so that solution crawler is fully initialized when we do de-dup live and build errors,
                    // otherwise, we will think none of error we have here belong to live errors since diagnostic service is not initialized yet.
                    if (_diagnosticService.GlobalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
                    {
                        var registrationService = (SolutionCrawlerRegistrationService)_workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                        registrationService.EnsureRegistration(_workspace, initializeLazily: false);
                    }

                    // Mark the status as updated to refresh error list before we invoke 'SyncBuildErrorsAndReportAsync', which can take some time to complete.
                    OnBuildProgressChanged(inProgressState, BuildProgress.Updated);

                    // We are about to update live analyzer data using one from build.
                    // pause live analyzer
                    using var operation = _notificationService.Start("BuildDone");
                    if (_diagnosticService is DiagnosticAnalyzerService diagnosticService)
                        await SyncBuildErrorsAndReportOnBuildCompletedAsync(diagnosticService, inProgressState).ConfigureAwait(false);

                    // Mark build as complete.
                    OnBuildProgressChanged(inProgressState, BuildProgress.Done);
                }
                finally
                {
                    await _postBuildAndErrorListRefreshTaskQueue.LastScheduledTask.ConfigureAwait(false);
                }
            }, GetApplicableCancellationToken(inProgressState));
        }

        /// <summary>
        /// Core method that de-dupes live and build diagnostics at the completion of build.
        /// It raises diagnostic update events for both the Build-only diagnostics and Build + Intellisense diagnostics
        /// in the error list.
        /// </summary>
        private ValueTask SyncBuildErrorsAndReportOnBuildCompletedAsync(DiagnosticAnalyzerService diagnosticService, InProgressState inProgressState)
        {
            var solution = inProgressState.Solution;
            var cancellationToken = inProgressState.CancellationToken;
            var (allLiveErrors, pendingLiveErrorsToSync) = inProgressState.GetLiveErrors();

            // Raise events for build only errors
            var buildErrors = GetBuildErrors().Except(allLiveErrors).GroupBy(k => k.DocumentId);
            foreach (var group in buildErrors)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

            // Report pending live errors
            return diagnosticService.SynchronizeWithBuildAsync(_workspace, pendingLiveErrorsToSync, _postBuildAndErrorListRefreshTaskQueue, onBuildCompleted: true, cancellationToken);
        }

        private void ReportBuildErrors<T>(T item, Solution solution, ImmutableArray<DiagnosticData> buildErrors)
        {
            if (item is ProjectId projectId)
            {
                RaiseDiagnosticsCreated(projectId, solution, projectId, null, buildErrors);
                return;
            }

            RoslynDebug.Assert(item is DocumentId);
            var documentId = (DocumentId)(object)item;
            RaiseDiagnosticsCreated(documentId, solution, documentId.ProjectId, documentId, buildErrors);
        }

        private void ClearBuildOnlyProjectErrors(Solution solution, ProjectId? projectId)
        {
            // Remove all project errors
            RaiseDiagnosticsRemoved(projectId, solution, projectId, documentId: null);

            var project = solution.GetProject(projectId);
            if (project == null)
            {
                return;
            }

            // Remove all document errors
            foreach (var documentId in project.DocumentIds.Concat(project.AdditionalDocumentIds).Concat(project.AnalyzerConfigDocumentIds))
            {
                ClearBuildOnlyDocumentErrors(solution, projectId, documentId);
            }
        }

        private void ClearBuildOnlyDocumentErrors(Solution solution, ProjectId? projectId, DocumentId? documentId)
            => RaiseDiagnosticsRemoved(documentId, solution, projectId, documentId);

        public void AddNewErrors(ProjectId projectId, DiagnosticData diagnostic)
        {
            Debug.Assert(diagnostic.IsBuildDiagnostic());

            // Capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Project New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(projectId, state).ConfigureAwait(false);
                state.AddError(projectId, diagnostic);
            }, state.CancellationToken);
        }

        public void AddNewErrors(DocumentId documentId, DiagnosticData diagnostic)
        {
            Debug.Assert(diagnostic.IsBuildDiagnostic());

            // Capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Document New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(documentId.ProjectId, state).ConfigureAwait(false);
                state.AddError(documentId, diagnostic);
            }, state.CancellationToken);
        }

        public void AddNewErrors(
            ProjectId projectId, HashSet<DiagnosticData> projectErrors, Dictionary<DocumentId, HashSet<DiagnosticData>> documentErrorMap)
        {
            Debug.Assert(projectErrors.All(d => d.IsBuildDiagnostic()));
            Debug.Assert(documentErrorMap.SelectMany(kvp => kvp.Value).All(d => d.IsBuildDiagnostic()));

            // Capture state that will be processed in background thread
            var state = GetOrCreateInProgressState();

            _taskQueue.ScheduleTask("Project New Errors", async () =>
            {
                await ReportPreviousProjectErrorsIfRequiredAsync(projectId, state).ConfigureAwait(false);

                foreach (var kv in documentErrorMap)
                    state.AddErrors(kv.Key, kv.Value);

                state.AddErrors(projectId, projectErrors);
            }, state.CancellationToken);
        }

        /// <summary>
        /// This method is invoked from all <see cref="M:AddNewErrors"/> overloads before it adds the new errors to the in progress state.
        /// It checks if build reported errors for a different project then the previous callback to report errors.
        /// This provides a good checkpoint to de-dupe build and live errors for lastProjectId and
        /// raise diagnostic updated events for that project.
        /// This ensures that error list keeps getting refreshed while a build is in progress, as opposed to doing all the work
        /// and a single refresh when the build completes.
        /// </summary>
        private ValueTask ReportPreviousProjectErrorsIfRequiredAsync(ProjectId projectId, InProgressState state)
        {
            if (state.TryGetLastProjectWithReportedErrors() is ProjectId lastProjectId &&
                lastProjectId != projectId)
            {
                return SetLiveErrorsForProjectAsync(lastProjectId, state);
            }

            return default;
        }

        private async ValueTask SetLiveErrorsForProjectAsync(ProjectId projectId, InProgressState state)
        {
            var diagnostics = state.GetLiveErrorsForProject(projectId);
            await SetLiveErrorsForProjectAsync(projectId, diagnostics, state.CancellationToken).ConfigureAwait(false);
            state.MarkLiveErrorsReported(projectId);
        }

        private ValueTask SetLiveErrorsForProjectAsync(ProjectId projectId, ImmutableArray<DiagnosticData> diagnostics, CancellationToken cancellationToken)
        {
            if (_diagnosticService is DiagnosticAnalyzerService diagnosticAnalyzerService)
            {
                // make those errors live errors
                var map = ProjectErrorMap.Empty.Add(projectId, diagnostics);
                return diagnosticAnalyzerService.SynchronizeWithBuildAsync(_workspace, map, _postBuildAndErrorListRefreshTaskQueue, onBuildCompleted: false, cancellationToken);
            }

            return default;
        }

        private CancellationToken GetApplicableCancellationToken(InProgressState? state)
            => state?.CancellationToken ?? _disposalToken;

        private InProgressState? GetBuildInProgressState()
        {
            lock (_gate)
            {
                return _stateDoNotAccessDirectly;
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
                    // We take current snapshot of solution when the state is first created. and through out this code, we use this snapshot.
                    // Since we have no idea what actual snapshot of solution the out of proc build has picked up, it doesn't remove the race we can have
                    // between build and diagnostic service, but this at least make us to consistent inside of our code.
                    _stateDoNotAccessDirectly = new InProgressState(this, _workspace.CurrentSolution, _activeCancellationSeriesDoNotAccessDirectly.CreateNext(_disposalToken));
                    OnBuildProgressChanged(_stateDoNotAccessDirectly, BuildProgress.Started);
                }

                return _stateDoNotAccessDirectly;
            }
        }

        private void RaiseDiagnosticsCreated(object? id, Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableArray<DiagnosticData> items)
        {
            _buildOnlyDiagnosticsService.AddBuildOnlyDiagnostics(solution, projectId, documentId, items);
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId, items));
        }

        private void RaiseDiagnosticsRemoved(object? id, Solution solution, ProjectId? projectId, DocumentId? documentId)
        {
            _buildOnlyDiagnosticsService.ClearBuildOnlyDiagnostics(solution, projectId, documentId);
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId));
        }

        private static ArgumentKey CreateArgumentKey(object? id) => new(id);

        private void RaiseBuildProgressChanged(BuildProgress progress)
            => BuildProgressChanged?.Invoke(this, progress);

        #region not supported
        public bool SupportGetDiagnostics { get { return false; } }

        public ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default)
        {
            return new ValueTask<ImmutableArray<DiagnosticData>>(ImmutableArray<DiagnosticData>.Empty);
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

            /// <summary>
            /// Map from project ID to all the possible analyzer diagnostic IDs that can be reported in the project.
            /// </summary>
            /// <remarks>
            /// This map may be accessed concurrently, so needs to ensure thread safety by using locks.
            /// </remarks>
            private readonly Dictionary<ProjectId, ImmutableHashSet<string>> _allDiagnosticIdMap = new();

            /// <summary>
            /// Map from project ID to all the possible intellisense analyzer diagnostic IDs that can be reported in the project.
            /// A diagnostic is considered to be an intellise analyzer diagnostic if is reported from a non-compilation end action in an analyzer,
            /// i.e. we do not require to analyze the entire compilation to compute these diagnostics.
            /// Compilation end diagnostics are considered build-only diagnostics.
            /// </summary>
            /// <remarks>
            /// This map may be accessed concurrently, so needs to ensure thread safety by using locks.
            /// </remarks>
            private readonly Dictionary<ProjectId, ImmutableHashSet<string>> _liveDiagnosticIdMap = new();

            // Fields that are used only from APIs invoked from serialized task queue, hence don't need to be thread safe.
            #region Serialized fields

            /// <summary>
            /// Map from project ID to a dictionary of reported project level diagnostics to an integral counter.
            /// Project level diagnostics are diagnostics that have no document location, i.e. reported with <see cref="Location.None"/>.
            /// Integral counter value for each diagnostic is used to order the reported diagnostics in error list
            /// based on the order in which they were reported during build.
            /// </summary>
            private readonly Dictionary<ProjectId, Dictionary<DiagnosticData, int>> _projectMap = new();

            /// <summary>
            /// Map from document ID to a dictionary of reported document level diagnostics to an integral counter.
            /// Project level diagnostics are diagnostics that have a valid document location, i.e. reported with a location within a syntax tree.
            /// Integral counter value for each diagnostic is used to order the reported diagnostics in error list
            /// based on the order in which they were reported during build.
            /// </summary>
            private readonly Dictionary<DocumentId, Dictionary<DiagnosticData, int>> _documentMap = new();

            /// <summary>
            /// Set of projects for which we have already cleared the build and intellisense diagnostics in the error list.
            /// </summary>
            private readonly HashSet<ProjectId> _projectsWithErrorsCleared = new();

            /// <summary>
            /// Set of projects for which we have reported all intellisense/live diagnostics.
            /// </summary>
            private readonly HashSet<ProjectId> _projectsWithAllLiveErrorsReported = new();

            /// <summary>
            /// Set of projects which have at least one project or document diagnostic in
            /// <see cref="_projectMap"/> and/or <see cref="_documentMap"/>.
            /// </summary>
            private readonly HashSet<ProjectId> _projectsWithErrors = new();

            /// <summary>
            /// Last project for which build reported an error through one of the <see cref="M:AddError"/> methods.
            /// </summary>
            private ProjectId? _lastProjectWithReportedErrors;

            /// <summary>
            /// Counter to help order the diagnostics in error list based on the order in which they were reported during build.
            /// </summary>
            private int _incrementDoNotAccessDirectly;

            #endregion

            public InProgressState(ExternalErrorDiagnosticUpdateSource owner, Solution solution, CancellationToken cancellationToken)
            {
                _owner = owner;
                Solution = solution;
                CancellationToken = cancellationToken;
            }

            public Solution Solution { get; }

            public CancellationToken CancellationToken { get; }

            private static ImmutableHashSet<string> GetOrCreateDiagnosticIds(
                ProjectId projectId,
                Dictionary<ProjectId, ImmutableHashSet<string>> diagnosticIdMap,
                Func<ImmutableHashSet<string>> computeDiagosticIds)
            {
                lock (diagnosticIdMap)
                {
                    if (diagnosticIdMap.TryGetValue(projectId, out var ids))
                    {
                        return ids;
                    }
                }

                var computedIds = computeDiagosticIds();

                lock (diagnosticIdMap)
                {
                    diagnosticIdMap[projectId] = computedIds;
                    return computedIds;
                }
            }

            public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
                => GetOrCreateSupportedDiagnosticIds(projectId).Contains(id);

            private ImmutableHashSet<string> GetOrCreateSupportedDiagnosticIds(ProjectId projectId)
            {
                return GetOrCreateDiagnosticIds(projectId, _allDiagnosticIdMap, ComputeSupportedDiagnosticIds);

                ImmutableHashSet<string> ComputeSupportedDiagnosticIds()
                {
                    var project = Solution.GetProject(projectId);
                    if (project == null)
                    {
                        // projectId no longer exist
                        return ImmutableHashSet<string>.Empty;
                    }

                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var descriptorMap = Solution.State.Analyzers.GetDiagnosticDescriptorsPerReference(_owner._diagnosticService.AnalyzerInfoCache, project);
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

            public (ImmutableArray<DiagnosticData> allLiveErrors, ProjectErrorMap pendingLiveErrorsToSync) GetLiveErrors()
            {
                var allLiveErrorsBuilder = ImmutableArray.CreateBuilder<DiagnosticData>();
                var pendingLiveErrorsToSyncBuilder = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableArray<DiagnosticData>>();
                foreach (var projectId in GetProjectsWithErrors())
                {
                    CancellationToken.ThrowIfCancellationRequested();

                    var errors = GetLiveErrorsForProject(projectId);
                    allLiveErrorsBuilder.AddRange(errors);

                    if (!_projectsWithAllLiveErrorsReported.Contains(projectId))
                    {
                        pendingLiveErrorsToSyncBuilder.Add(projectId, errors);
                    }
                }

                return (allLiveErrorsBuilder.ToImmutable(), pendingLiveErrorsToSyncBuilder.ToImmutable());

                // Local functions.
                IEnumerable<ProjectId> GetProjectsWithErrors()
                {
                    // Filter out project that is no longer exist in IDE
                    // this can happen if user started a "build" and then remove a project from IDE
                    // before build finishes
                    return _projectsWithErrors.Where(p => Solution.GetProject(p) != null);
                }
            }

            public ImmutableArray<DiagnosticData> GetLiveErrorsForProject(ProjectId projectId)
            {
                var project = Solution.GetProject(projectId);
                if (project == null)
                {
                    return ImmutableArray<DiagnosticData>.Empty;
                }

                var diagnostics = _projectMap.Where(kv => kv.Key == projectId).SelectMany(kv => kv.Value).Concat(
                        _documentMap.Where(kv => kv.Key.ProjectId == projectId).SelectMany(kv => kv.Value));
                using var _ = ArrayBuilder<DiagnosticData>.GetInstance(out var builder);
                foreach (var (diagnostic, _) in diagnostics)
                {
                    if (IsLive(project, diagnostic))
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

            private bool IsLive(Project project, DiagnosticData diagnosticData)
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

                // Compiler diagnostics reported on additional documents indicate mapped diagnostics, such as compiler diagnostics
                // in razor files which are actually reported on generated source files but mapped to razor files during build.
                // These are not reported on additional files during live analysis, and can be considered to be build-only diagnostics.
                if (IsAdditionalDocumentDiagnostic(project, diagnosticData) &&
                    diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Compiler))
                {
                    return false;
                }

                if (IsSupportedLiveDiagnosticId(project, diagnosticData.Id))
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
                    return
                        !string.IsNullOrEmpty(diagnosticData.DataLocation.UnmappedFileSpan.Path) &&
                        (diagnosticData.DataLocation.UnmappedFileSpan.StartLinePosition.Line > 0 ||
                         diagnosticData.DataLocation.UnmappedFileSpan.StartLinePosition.Character > 0);
                }

                static bool IsAdditionalDocumentDiagnostic(Project project, DiagnosticData diagnosticData)
                    => diagnosticData.DocumentId != null && project.ContainsAdditionalDocument(diagnosticData.DocumentId);
            }

            private bool IsSupportedLiveDiagnosticId(Project project, string id)
                => GetOrCreateSupportedLiveDiagnostics(project).Contains(id);

            private ImmutableHashSet<string> GetOrCreateSupportedLiveDiagnostics(Project project)
            {
                var fullSolutionAnalysis = _owner._diagnosticService.GlobalOptions.IsFullSolutionAnalysisEnabled(project.Language);
                if (!project.SupportsCompilation || fullSolutionAnalysis)
                {
                    // Defer to _allDiagnosticIdMap so we avoid placing FSA diagnostics in _liveDiagnosticIdMap
                    return GetOrCreateSupportedDiagnosticIds(project.Id);
                }

                return GetOrCreateDiagnosticIds(project.Id, _liveDiagnosticIdMap, ComputeSupportedLiveDiagnosticIds);

                ImmutableHashSet<string> ComputeSupportedLiveDiagnosticIds()
                {
                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var infoCache = _owner._diagnosticService.AnalyzerInfoCache;

                    foreach (var analyzersPerReference in project.Solution.State.Analyzers.CreateDiagnosticAnalyzersPerReference(project))
                    {
                        foreach (var analyzer in analyzersPerReference.Value)
                        {
                            var diagnosticIds = infoCache.GetNonCompilationEndDiagnosticDescriptors(analyzer).Select(d => d.Id);
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
                    RoslynDebug.Assert(key is DocumentId or ProjectId);
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
            public static readonly DiagnosticDataComparer Instance = new();

            public bool Equals(DiagnosticData item1, DiagnosticData item2)
            {
                if ((item1.DocumentId == null) != (item2.DocumentId == null) ||
                    item1.Id != item2.Id ||
                    item1.ProjectId != item2.ProjectId ||
                    item1.Severity != item2.Severity ||
                    item1.Message != item2.Message ||
                    item1.DataLocation.MappedFileSpan.Span != item2.DataLocation.MappedFileSpan.Span ||
                    item1.DataLocation.UnmappedFileSpan.Span != item2.DataLocation.UnmappedFileSpan.Span)
                {
                    return false;
                }

                // TODO: unclear why we are comparing the original paths, and not the normalized paths.   This may
                // indicate a bug. If it is correct behavior, this should be documented as to why this is the right span
                // to be considering.
                return (item1.DocumentId != null)
                    ? item1.DocumentId == item2.DocumentId
                    : item1.DataLocation.UnmappedFileSpan.Path == item2.DataLocation.UnmappedFileSpan.Path;
            }

            public int GetHashCode(DiagnosticData obj)
            {
                // TODO: unclear on why we're hashing the start of the data location, whereas .Equals above checks the
                // full span.
                var result =
                    Hash.Combine(obj.Id,
                    Hash.Combine(obj.Message,
                    Hash.Combine(obj.ProjectId,
                    Hash.Combine(obj.DataLocation.MappedFileSpan.Span.Start.GetHashCode(),
                    Hash.Combine(obj.DataLocation.UnmappedFileSpan.Span.Start.GetHashCode(), (int)obj.Severity)))));

                // TODO: unclear why we are hashing the original path, and not the normalized path.   This may
                // indicate a bug. If it is correct behavior, this should be documented as to why this is the right span
                // to be considering.
                return obj.DocumentId != null
                    ? Hash.Combine(obj.DocumentId, result)
                    : Hash.Combine(obj.DataLocation.UnmappedFileSpan.Path, result);
            }
        }
    }
}
