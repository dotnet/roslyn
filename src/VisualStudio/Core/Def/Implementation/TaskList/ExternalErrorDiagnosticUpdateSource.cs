// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal sealed class ExternalErrorDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IGlobalOperationNotificationService _notificationService;

        private readonly SimpleTaskQueue _taskQueue;
        private readonly IAsynchronousOperationListener _listener;

        private readonly object _gate = new object();
        private InProgressState _stateDoNotAccessDirectly = null;
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
            _taskQueue = new SimpleTaskQueue(TaskScheduler.Default);
            _listener = listener;

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _diagnosticService = diagnosticService;

            _notificationService = _workspace.Services.GetService<IGlobalOperationNotificationService>();
        }

        public event EventHandler<BuildProgress> BuildProgressChanged;
        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;
        public event EventHandler DiagnosticsCleared { add { } remove { } }

        public bool IsInProgress => BuildInprogressState != null;

        public ImmutableArray<DiagnosticData> GetBuildErrors()
        {
            return _lastBuiltResult;
        }

        public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
        {
            return BuildInprogressState?.IsSupportedDiagnosticId(projectId, id) ?? false;
        }

        public void ClearErrors(ProjectId projectId)
        {
            // capture state if it exists
            var state = BuildInprogressState;

            var asyncToken = _listener.BeginAsyncOperation("ClearErrors");
            _taskQueue.ScheduleTask(() =>
            {
                // this will get called if the project is actually built by "build" command.
                // we track what project has been built, so that later we can clear any stale live errors
                // if build give us no errors for this project
                state?.Built(projectId);

                ClearProjectErrors(state?.Solution ?? _workspace.CurrentSolution, projectId);
            }).CompletesAsyncOperation(asyncToken);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnSolutionChanged");
                        _taskQueue.ScheduleTask(() => e.OldSolution.ProjectIds.Do(p => ClearProjectErrors(e.OldSolution, p))).CompletesAsyncOperation(asyncToken);
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnProjectChanged");
                        _taskQueue.ScheduleTask(() => ClearProjectErrors(e.OldSolution, e.ProjectId)).CompletesAsyncOperation(asyncToken);
                        break;
                    }

                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnDocumentRemoved");
                        _taskQueue.ScheduleTask(() => ClearDocumentErrors(e.OldSolution, e.ProjectId, e.DocumentId)).CompletesAsyncOperation(asyncToken);
                        break;
                    }

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
                    Contract.Fail("Unknown workspace events");
                    break;
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
            var asyncToken = _listener.BeginAsyncOperation("OnSolutionBuild");
            _taskQueue.ScheduleTask(async () =>
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
                var registrationService = (SolutionCrawlerRegistrationService)_workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
                registrationService.EnsureRegistration(_workspace, initializeLazily: false);

                _lastBuiltResult = inProgressState.GetBuildDiagnostics();

                // we are about to update live analyzer data using one from build.
                // pause live analyzer
                using var operation = _notificationService.Start("BuildDone");
                if (_diagnosticService is DiagnosticAnalyzerService diagnosticService)
                {
                    await CleanupAllLiveErrorsAsync(diagnosticService, inProgressState.GetProjectsWithoutErrors()).ConfigureAwait(false);
                    await SyncBuildErrorsAndReportAsync(diagnosticService, inProgressState).ConfigureAwait(false);
                }

                inProgressState.Done();
            }).CompletesAsyncOperation(asyncToken);
        }

        private Task CleanupAllLiveErrorsAsync(DiagnosticAnalyzerService diagnosticService, IEnumerable<ProjectId> projects)
        {
            var map = projects.ToImmutableDictionary(p => p, _ => ImmutableArray<DiagnosticData>.Empty);
            return diagnosticService.SynchronizeWithBuildAsync(_workspace, map);
        }

        private async Task SyncBuildErrorsAndReportAsync(DiagnosticAnalyzerService diagnosticService, InProgressState inProgressState)
        {
            var solution = inProgressState.Solution;
            var map = await inProgressState.GetLiveDiagnosticsPerProjectAsync().ConfigureAwait(false);

            // make those errors live errors
            await diagnosticService.SynchronizeWithBuildAsync(_workspace, map).ConfigureAwait(false);

            // raise events for ones left-out
            var buildErrors = GetBuildErrors().Except(map.Values.SelectMany(v => v)).GroupBy(k => k.DocumentId);
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
            RaiseDiagnosticsCreated(documentId, solution, documentId.ProjectId, documentId, buildErrors);
        }

        private void ClearProjectErrors(Solution solution, ProjectId projectId)
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
                ClearDocumentErrors(solution, projectId, documentId);
            }
        }

        private void ClearDocumentErrors(Solution solution, ProjectId projectId, DocumentId documentId)
        {
            RaiseDiagnosticsRemoved(documentId, solution, projectId, documentId);
        }

        public void AddNewErrors(ProjectId projectId, DiagnosticData diagnostic)
        {
            // capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            var asyncToken = _listener.BeginAsyncOperation("Project New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                state.AddError(projectId, diagnostic);
            }).CompletesAsyncOperation(asyncToken);
        }

        public void AddNewErrors(DocumentId documentId, DiagnosticData diagnostic)
        {
            // capture state that will be processed in background thread.
            var state = GetOrCreateInProgressState();

            var asyncToken = _listener.BeginAsyncOperation("Document New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                state.AddError(documentId, diagnostic);
            }).CompletesAsyncOperation(asyncToken);
        }

        public void AddNewErrors(
            ProjectId projectId, HashSet<DiagnosticData> projectErrors, Dictionary<DocumentId, HashSet<DiagnosticData>> documentErrorMap)
        {
            // capture state that will be processed in background thread
            var state = GetOrCreateInProgressState();

            var asyncToken = _listener.BeginAsyncOperation("Project New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                foreach (var kv in documentErrorMap)
                {
                    state.AddErrors(kv.Key, kv.Value);
                }

                state.AddErrors(projectId, projectErrors);
            }).CompletesAsyncOperation(asyncToken);
        }

        private InProgressState BuildInprogressState
        {
            get
            {
                lock (_gate)
                {
                    return _stateDoNotAccessDirectly;
                }
            }
        }

        private InProgressState ClearInProgressState()
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
                }

                return _stateDoNotAccessDirectly;
            }
        }

        private void RaiseDiagnosticsCreated(object id, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> items)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId, items));
        }

        private void RaiseDiagnosticsRemoved(object id, Solution solution, ProjectId projectId, DocumentId documentId)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                   CreateArgumentKey(id), _workspace, solution, projectId, documentId));
        }

        private static ArgumentKey CreateArgumentKey(object id) => new ArgumentKey(id);

        private void RaiseBuildProgressChanged(BuildProgress progress)
        {
            BuildProgressChanged?.Invoke(this, progress);
        }

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
            private int _incrementDoNotAccessDirectly = 0;

            private readonly ExternalErrorDiagnosticUpdateSource _owner;
            private readonly HashSet<ProjectId> _projectsBuilt = new HashSet<ProjectId>();
            private readonly HashSet<ProjectId> _projectsWithErrors = new HashSet<ProjectId>();

            private readonly Dictionary<ProjectId, ImmutableHashSet<string>> _allDiagnosticIdMap = new Dictionary<ProjectId, ImmutableHashSet<string>>();
            private readonly Dictionary<ProjectId, ImmutableHashSet<string>> _liveDiagnosticIdMap = new Dictionary<ProjectId, ImmutableHashSet<string>>();

            private readonly Dictionary<ProjectId, Dictionary<DiagnosticData, int>> _projectMap = new Dictionary<ProjectId, Dictionary<DiagnosticData, int>>();
            private readonly Dictionary<DocumentId, Dictionary<DiagnosticData, int>> _documentMap = new Dictionary<DocumentId, Dictionary<DiagnosticData, int>>();

            public InProgressState(ExternalErrorDiagnosticUpdateSource owner, Solution solution)
            {
                _owner = owner;
                Solution = solution;

                // let people know build has started
                _owner.RaiseBuildProgressChanged(BuildProgress.Started);
            }

            public Solution Solution { get; }

            public void Done()
            {
                _owner.RaiseBuildProgressChanged(BuildProgress.Done);
            }

            public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
            {
                lock (_allDiagnosticIdMap)
                {
                    if (_allDiagnosticIdMap.TryGetValue(projectId, out var ids))
                    {
                        return ids.Contains(id);
                    }

                    var project = Solution.GetProject(projectId);
                    if (project == null)
                    {
                        // projectId no longer exist, return false;
                        _allDiagnosticIdMap.Add(projectId, ImmutableHashSet<string>.Empty);
                        return false;
                    }

                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var descriptorMap = _owner._diagnosticService.CreateDiagnosticDescriptorsPerReference(project);
                    builder.UnionWith(descriptorMap.Values.SelectMany(v => v.Select(d => d.Id)));

                    var set = builder.ToImmutable();
                    _allDiagnosticIdMap.Add(projectId, set);

                    return set.Contains(id);
                }
            }

            public ImmutableArray<DiagnosticData> GetBuildDiagnostics()
            {
                // return errors in the order that is reported
                return ImmutableArray.CreateRange(
                    _projectMap.Values.SelectMany(d => d).Concat(_documentMap.Values.SelectMany(d => d)).OrderBy(kv => kv.Value).Select(kv => kv.Key));
            }

            public void Built(ProjectId projectId)
            {
                _projectsBuilt.Add(projectId);
            }

            public IEnumerable<ProjectId> GetProjectsWithErrors()
            {
                // filter out project that is no longer exist in IDE
                // this can happen if user started a "build" and then remove a project from IDE
                // before build finishes
                return _projectsWithErrors.Where(p => Solution.GetProject(p) != null);
            }

            public IEnumerable<ProjectId> GetProjectsWithoutErrors()
            {
                return GetProjectsBuilt().Except(GetProjectsWithErrors());

                IEnumerable<ProjectId> GetProjectsBuilt()
                {
                    // filter out project that is no longer exist in IDE
                    // this can happen if user started a "build" and then remove a project from IDE
                    // before build finishes
                    return Solution.ProjectIds.Where(p => _projectsBuilt.Contains(p));
                }
            }

            public async Task<ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>>> GetLiveDiagnosticsPerProjectAsync()
            {
                var builder = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableArray<DiagnosticData>>();
                foreach (var projectId in GetProjectsWithErrors())
                {
                    var project = Solution.GetProject(projectId);
                    var compilation = project.SupportsCompilation ?
                        await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false) : null;

                    var diagnostics = ImmutableArray.CreateRange(
                        _projectMap.Where(kv => kv.Key == projectId).SelectMany(kv => kv.Value).Concat(
                            _documentMap.Where(kv => kv.Key.ProjectId == projectId).SelectMany(kv => kv.Value))
                                .Select(kv => kv.Key).Where(d => IsLive(project, compilation, d)));

                    builder.Add(projectId, diagnostics);
                }

                return builder.ToImmutable();
            }

            public void AddErrors(DocumentId key, HashSet<DiagnosticData> diagnostics)
            {
                AddErrors(_documentMap, key, diagnostics);
            }

            public void AddErrors(ProjectId key, HashSet<DiagnosticData> diagnostics)
            {
                AddErrors(_projectMap, key, diagnostics);
            }

            public void AddError(DocumentId key, DiagnosticData diagnostic)
            {
                AddError(_documentMap, key, diagnostic);
            }

            public void AddError(ProjectId key, DiagnosticData diagnostic)
            {
                AddError(_projectMap, key, diagnostic);
            }

            private bool IsLive(Project project, Compilation compilation, DiagnosticData diagnosticData)
            {
                // REVIEW: current design is that we special case compiler analyzer case and we accept only document level
                //         diagnostic as live. otherwise, we let them be build errors. we changed compiler analyzer accordingly as well
                //         so that it doesn't report project level diagnostic as live errors.
                if (_owner._diagnosticService.IsCompilerDiagnostic(project.Language, diagnosticData) &&
                    !IsDocumentLevelDiagnostic(diagnosticData))
                {
                    // compiler error but project level error
                    return false;
                }

                if (SupportedLiveDiagnosticId(project, compilation, diagnosticData.Id))
                {
                    return true;
                }

                return false;

                static bool IsDocumentLevelDiagnostic(DiagnosticData diagnoaticData)
                {
                    if (diagnoaticData.DocumentId != null)
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
                    return !string.IsNullOrEmpty(diagnoaticData.DataLocation.OriginalFilePath) &&
                            (diagnoaticData.DataLocation.OriginalStartLine > 0 ||
                             diagnoaticData.DataLocation.OriginalStartColumn > 0);
                }
            }

            private bool SupportedLiveDiagnosticId(Project project, Compilation compilation, string id)
            {
                var projectId = project.Id;

                lock (_liveDiagnosticIdMap)
                {
                    if (_liveDiagnosticIdMap.TryGetValue(projectId, out var ids))
                    {
                        return ids.Contains(id);
                    }

                    var fullSolutionAnalysis = ServiceFeatureOnOffOptions.IsClosedFileDiagnosticsEnabled(project);
                    if (!project.SupportsCompilation || fullSolutionAnalysis)
                    {
                        return IsSupportedDiagnosticId(project.Id, id);
                    }

                    // set ids set
                    var builder = ImmutableHashSet.CreateBuilder<string>();
                    var diagnosticService = _owner._diagnosticService;
                    foreach (var analyzer in diagnosticService.GetDiagnosticAnalyzers(project))
                    {
                        if (diagnosticService.IsCompilationEndAnalyzer(analyzer, project, compilation))
                        {
                            continue;
                        }

                        var diagnosticIds = diagnosticService.GetDiagnosticDescriptors(analyzer).Select(d => d.Id);
                        builder.UnionWith(diagnosticIds);
                    }

                    var set = builder.ToImmutable();
                    _liveDiagnosticIdMap.Add(projectId, set);

                    return set.Contains(id);
                }
            }

            private void AddErrors<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key, HashSet<DiagnosticData> diagnostics)
            {
                var errors = GetErrorSet(map, key);
                foreach (var diagnostic in diagnostics)
                {
                    AddError(errors, diagnostic, key);
                }
            }

            private void AddError<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key, DiagnosticData diagnostic)
            {
                var errors = GetErrorSet(map, key);
                AddError(errors, diagnostic, key);
            }

            private void AddError<T>(Dictionary<DiagnosticData, int> errors, DiagnosticData diagnostic, T key)
            {
                RecordProjectContainsErrors();

                // add only new errors
                if (!errors.TryGetValue(diagnostic, out _))
                {
                    Logger.Log(FunctionId.ExternalErrorDiagnosticUpdateSource_AddError, d => d.ToString(), diagnostic);

                    errors.Add(diagnostic, GetNextIncrement());
                }

                int GetNextIncrement()
                {
                    return _incrementDoNotAccessDirectly++;
                }

                void RecordProjectContainsErrors()
                {
                    var projectId = (key is DocumentId documentId) ? documentId.ProjectId : (ProjectId)(object)key;

                    if (!_projectsWithErrors.Add(projectId))
                    {
                        return;
                    }

                    // this will make build only error list to be updated per project rather than per solution.
                    // basically this will make errors up to last project to show up in error list
                    _owner._lastBuiltResult = GetBuildDiagnostics();
                    _owner.RaiseBuildProgressChanged(BuildProgress.Updated);
                }
            }

            private Dictionary<DiagnosticData, int> GetErrorSet<T>(Dictionary<T, Dictionary<DiagnosticData, int>> map, T key)
            {
                return map.GetOrAdd(key, _ => new Dictionary<DiagnosticData, int>(DiagnosticDataComparer.Instance));
            }
        }

        private sealed class ArgumentKey : BuildToolId.Base<object>
        {
            public ArgumentKey(object key) : base(key)
            {
            }

            public override string BuildTool
            {
                get { return PredefinedBuildTools.Build; }
            }

            public override bool Equals(object obj)
                => obj is ArgumentKey &&
                   base.Equals(obj);

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
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
