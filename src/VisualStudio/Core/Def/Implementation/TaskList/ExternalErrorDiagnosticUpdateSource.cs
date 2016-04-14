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
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(ExternalErrorDiagnosticUpdateSource))]
    internal class ExternalErrorDiagnosticUpdateSource : ForegroundThreadAffinitizedObject, IDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IGlobalOperationNotificationService _notificationService;

        private readonly SimpleTaskQueue _taskQueue;
        private readonly IAsynchronousOperationListener _listener;

        private InprogressState _state = null;
        private ImmutableArray<DiagnosticData> _lastBuiltResult = ImmutableArray<DiagnosticData>.Empty;

        [ImportingConstructor]
        public ExternalErrorDiagnosticUpdateSource(
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
                this(workspace, diagnosticService, registrationService, new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ErrorList))
        {
            Contract.Requires(!KnownUIContexts.SolutionBuildingContext.IsActive);
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += OnSolutionBuild;
        }

        /// <summary>
        /// internal for testing
        /// </summary>
        internal ExternalErrorDiagnosticUpdateSource(
            Workspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IDiagnosticUpdateSourceRegistrationService registrationService,
            IAsynchronousOperationListener listener)
        {
            // use queue to serialize work. no lock needed
            _taskQueue = new SimpleTaskQueue(TaskScheduler.Default);
            _listener = listener;

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _diagnosticService = diagnosticService;

            _notificationService = _workspace.Services.GetService<IGlobalOperationNotificationService>();

            registrationService.Register(this);
        }

        public event EventHandler<bool> BuildStarted;
        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public bool IsInProgress => _state != null;

        public ImmutableArray<DiagnosticData> GetBuildErrors()
        {
            return _lastBuiltResult;
        }

        public void ClearErrors(ProjectId projectId)
        {
            AssertIsForeground();

            var asyncToken = _listener.BeginAsyncOperation("ClearErrors");
            _taskQueue.ScheduleTask(() =>
            {
                // record the project as built only if we are in build.
                // otherwise (such as closing solution or removing project), no need to record it
                _state?.Built(projectId);

                ClearProjectErrors(projectId);
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
                        _taskQueue.ScheduleTask(() => e.OldSolution.ProjectIds.Do(p => ClearProjectErrors(p, e.OldSolution))).CompletesAsyncOperation(asyncToken);
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnProjectChanged");
                        _taskQueue.ScheduleTask(() => ClearProjectErrors(e.ProjectId, e.OldSolution)).CompletesAsyncOperation(asyncToken);
                        break;
                    }

                case WorkspaceChangeKind.DocumentRemoved:
                case WorkspaceChangeKind.DocumentReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnDocumentRemoved");
                        _taskQueue.ScheduleTask(() => ClearDocumentErrors(e.ProjectId, e.DocumentId)).CompletesAsyncOperation(asyncToken);
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
                    break;
                default:
                    Contract.Fail("Unknown workspace events");
                    break;
            }
        }

        internal void OnSolutionBuild(object sender, UIContextChangedEventArgs e)
        {
            AssertIsForeground();

            if (e.Activated)
            {
                // build just started, create the state and fire build in progress event.
                var state = GetOrCreateInprogressState();
                return;
            }

            // get local copy of inprogress state
            var inprogressState = _state;

            // building is done. reset the state.
            _state = null;

            // enqueue build/live sync in the queue.
            var asyncToken = _listener.BeginAsyncOperation("OnSolutionBuild");
            _taskQueue.ScheduleTask(async () =>
            {
                // nothing to do
                if (inprogressState == null)
                {
                    return;
                }

                _lastBuiltResult = inprogressState.GetBuildDiagnostics();

                // we are about to update live analyzer data using one from build.
                // pause live analyzer
                using (var operation = _notificationService.Start("BuildDone"))
                {
                    // we will have a race here since we can't track version of solution the out of proc build actually used.
                    // result of the race will be us dropping some diagnostics from the build to the floor.
                    var solution = _workspace.CurrentSolution;

                    var supportedIdMap = GetSupportedLiveDiagnosticId(solution, inprogressState);
                    Func<DiagnosticData, bool> liveDiagnosticChecker = d =>
                    {
                        // REVIEW: we probably need a better design on de-duplicating live and build errors. or don't de-dup at all.
                        //         for now, we are special casing compiler error case.
                        var project = solution.GetProject(d.ProjectId);
                        if (project == null)
                        {
                            // project doesn't exist
                            return false;
                        }

                        // REVIEW: current design is that we special case compiler analyzer case and we accept only document level
                        //         diagnostic as live. otherwise, we let them be build errors. we changed compiler analyzer accordingly as well
                        //         so that it doesn't report project level diagnostic as live errors.
                        if (_diagnosticService.IsCompilerDiagnostic(project.Language, d) && d.DocumentId == null)
                        {
                            // compiler error but project level error
                            return false;
                        }

                        HashSet<string> set;
                        if (supportedIdMap.TryGetValue(d.ProjectId, out set) && set.Contains(d.Id))
                        {
                            return true;
                        }

                        return false;
                    };

                    var diagnosticService = _diagnosticService as DiagnosticAnalyzerService;
                    if (diagnosticService != null)
                    {
                        await CleanupAllLiveErrorsIfNeededAsync(diagnosticService, solution, inprogressState).ConfigureAwait(false);
                        await SyncBuildErrorsAndReportAsync(diagnosticService, inprogressState.GetLiveDiagnosticsPerProject(liveDiagnosticChecker)).ConfigureAwait(false);
                    }

                    inprogressState.Done();
                }
            }).CompletesAsyncOperation(asyncToken);
        }

        private async System.Threading.Tasks.Task CleanupAllLiveErrorsIfNeededAsync(DiagnosticAnalyzerService diagnosticService, Solution solution, InprogressState state)
        {
            if (_workspace.Options.GetOption(InternalDiagnosticsOptions.BuildErrorIsTheGod))
            {
                await CleanupAllLiveErrors(diagnosticService, solution.ProjectIds).ConfigureAwait(false);
                return;
            }

            if (_workspace.Options.GetOption(InternalDiagnosticsOptions.ClearLiveErrorsForProjectBuilt))
            {
                await CleanupAllLiveErrors(diagnosticService, state.GetProjectsBuilt(solution)).ConfigureAwait(false);
                return;
            }

            await CleanupAllLiveErrors(diagnosticService, state.GetProjectsWithoutErrors(solution)).ConfigureAwait(false);
            return;
        }

        private System.Threading.Tasks.Task CleanupAllLiveErrors(DiagnosticAnalyzerService diagnosticService, IEnumerable<ProjectId> projects)
        {
            var map = projects.ToImmutableDictionary(p => p, _ => ImmutableArray<DiagnosticData>.Empty);
            return diagnosticService.SynchronizeWithBuildAsync(_workspace, map);
        }

        private async System.Threading.Tasks.Task SyncBuildErrorsAndReportAsync(DiagnosticAnalyzerService diagnosticService, ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> map)
        {
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
                        ReportBuildErrors(projectGroup.Key, projectGroup.ToImmutableArray());
                    }

                    continue;
                }

                ReportBuildErrors(group.Key, group.ToImmutableArray());
            }
        }

        private void ReportBuildErrors<T>(T item, ImmutableArray<DiagnosticData> buildErrors)
        {
            var projectId = item as ProjectId;
            if (projectId != null)
            {
                RaiseDiagnosticsCreated(projectId, projectId, null, buildErrors);
                return;
            }

            // must be not null
            var documentId = item as DocumentId;
            RaiseDiagnosticsCreated(documentId, documentId.ProjectId, documentId, buildErrors);
        }

        private Dictionary<ProjectId, HashSet<string>> GetSupportedLiveDiagnosticId(Solution solution, InprogressState state)
        {
            var map = new Dictionary<ProjectId, HashSet<string>>();

            // here, we don't care about perf that much since build is already expensive work
            foreach (var projectId in state.GetProjectsWithErrors(solution))
            {
                var project = solution.GetProject(projectId);
                if (project == null)
                {
                    continue;
                }

                var descriptorMap = _diagnosticService.GetDiagnosticDescriptors(project);
                map.Add(project.Id, new HashSet<string>(descriptorMap.Values.SelectMany(v => v.Select(d => d.Id))));
            }

            return map;
        }

        private void ClearProjectErrors(ProjectId projectId, Solution solution = null)
        {
            // remove all project errors
            RaiseDiagnosticsRemoved(projectId, projectId, documentId: null);

            var project = (solution ?? _workspace.CurrentSolution).GetProject(projectId);
            if (project == null)
            {
                return;
            }

            // remove all document errors
            foreach (var documentId in project.DocumentIds)
            {
                ClearDocumentErrors(projectId, documentId);
            }
        }

        private void ClearDocumentErrors(ProjectId projectId, DocumentId documentId)
        {
            RaiseDiagnosticsRemoved(documentId, projectId, documentId);
        }

        public void AddNewErrors(DocumentId documentId, DiagnosticData diagnostic)
        {
            AssertIsForeground();

            var asyncToken = _listener.BeginAsyncOperation("Document New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                GetOrCreateInprogressState().AddError(documentId, diagnostic);
            }).CompletesAsyncOperation(asyncToken);
        }

        public void AddNewErrors(
            ProjectId projectId, HashSet<DiagnosticData> projectErrors, Dictionary<DocumentId, HashSet<DiagnosticData>> documentErrorMap)
        {
            AssertIsForeground();

            var asyncToken = _listener.BeginAsyncOperation("Project New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                var state = GetOrCreateInprogressState();
                foreach (var kv in documentErrorMap)
                {
                    state.AddErrors(kv.Key, kv.Value);
                }

                state.AddErrors(projectId, projectErrors);
            }).CompletesAsyncOperation(asyncToken);
        }

        private InprogressState GetOrCreateInprogressState()
        {
            if (_state == null)
            {
                _state = new InprogressState(this);
            }

            return _state;
        }

        private void RaiseDiagnosticsCreated(object id, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> items)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                   CreateArgumentKey(id), _workspace, _workspace.CurrentSolution, projectId, documentId, items));
        }

        private void RaiseDiagnosticsRemoved(object id, ProjectId projectId, DocumentId documentId)
        {
            DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsRemoved(
                   CreateArgumentKey(id), _workspace, _workspace.CurrentSolution, projectId, documentId));
        }

        private static ArgumentKey CreateArgumentKey(object id) => new ArgumentKey(id);

        private void RaiseBuildStarted(bool started)
        {
            BuildStarted?.Invoke(this, started);
        }

        #region not supported
        public bool SupportGetDiagnostics { get { return false; } }

        public ImmutableArray<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return ImmutableArray<DiagnosticData>.Empty;
        }
        #endregion

        private class InprogressState
        {
            private readonly ExternalErrorDiagnosticUpdateSource _owner;

            private readonly HashSet<ProjectId> _builtProjects = new HashSet<ProjectId>();
            private readonly Dictionary<ProjectId, HashSet<DiagnosticData>> _projectMap = new Dictionary<ProjectId, HashSet<DiagnosticData>>();
            private readonly Dictionary<DocumentId, HashSet<DiagnosticData>> _documentMap = new Dictionary<DocumentId, HashSet<DiagnosticData>>();

            public InprogressState(ExternalErrorDiagnosticUpdateSource owner)
            {
                _owner = owner;

                // let people know build has started
                // TODO: to be more accurate, it probably needs to be counted. but for now,
                //       I think the way it is doing probably enough.
                _owner.RaiseBuildStarted(started: true);
            }

            public void Done()
            {
                _owner.RaiseBuildStarted(started: false);
            }

            public ImmutableArray<DiagnosticData> GetBuildDiagnostics()
            {
                return ImmutableArray.CreateRange(_projectMap.Values.SelectMany(d => d).Concat(_documentMap.Values.SelectMany(d => d)));
            }

            public void Built(ProjectId projectId)
            {
                _builtProjects.Add(projectId);
            }

            public IEnumerable<ProjectId> GetProjectsBuilt(Solution solution)
            {
                return solution.ProjectIds.Where(p => _builtProjects.Contains(p));
            }

            public IEnumerable<ProjectId> GetProjectsWithErrors(Solution solution)
            {
                return GetProjectIds().Where(p => solution.GetProject(p) != null);
            }

            public IEnumerable<ProjectId> GetProjectsWithoutErrors(Solution solution)
            {
                return GetProjectsBuilt(solution).Except(GetProjectsWithErrors(solution));
            }

            public ImmutableDictionary<ProjectId, ImmutableArray<DiagnosticData>> GetLiveDiagnosticsPerProject(Func<DiagnosticData, bool> liveDiagnosticChecker)
            {
                var builder = ImmutableDictionary.CreateBuilder<ProjectId, ImmutableArray<DiagnosticData>>();
                foreach (var projectId in GetProjectIds())
                {
                    var diagnostics = ImmutableArray.CreateRange(
                        _projectMap.Where(kv => kv.Key == projectId).SelectMany(kv => kv.Value).Concat(
                            _documentMap.Where(kv => kv.Key.ProjectId == projectId).SelectMany(kv => kv.Value)).Where(liveDiagnosticChecker));

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

            private void AddErrors<T>(Dictionary<T, HashSet<DiagnosticData>> map, T key, HashSet<DiagnosticData> diagnostics)
            {
                var errors = GetErrorSet(map, key);
                errors.UnionWith(diagnostics);
            }

            private void AddError<T>(Dictionary<T, HashSet<DiagnosticData>> map, T key, DiagnosticData diagnostic)
            {
                var errors = GetErrorSet(map, key);
                errors.Add(diagnostic);
            }

            private IEnumerable<ProjectId> GetProjectIds()
            {
                return _documentMap.Keys.Select(k => k.ProjectId).Concat(_projectMap.Keys).Distinct();
            }

            private HashSet<DiagnosticData> GetErrorSet<T>(Dictionary<T, HashSet<DiagnosticData>> map, T key)
            {
                return map.GetOrAdd(key, _ => new HashSet<DiagnosticData>(DiagnosticDataComparer.Instance));
            }
        }

        private class ArgumentKey : BuildToolId.Base<object>
        {
            public ArgumentKey(object key) : base(key)
            {
            }

            public override string BuildTool
            {
                get { return PredefinedBuildTools.Build; }
            }

            public override bool Equals(object obj)
            {
                var other = obj as ArgumentKey;
                if (other == null)
                {
                    return false;
                }

                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        private class DiagnosticDataComparer : IEqualityComparer<DiagnosticData>
        {
            public static readonly DiagnosticDataComparer Instance = new DiagnosticDataComparer();

            public bool Equals(DiagnosticData item1, DiagnosticData item2)
            {
                // crash if any one of them is NULL
                if ((IsNull(item1.DocumentId) ^ IsNull(item2.DocumentId)) ||
                    (IsNull(item1.ProjectId) ^ IsNull(item2.ProjectId)))
                {
                    return false;
                }

                if (item1.DocumentId != null && item2.DocumentId != null)
                {
                    var lineColumn1 = GetOriginalOrMappedLineColumn(item1);
                    var lineColumn2 = GetOriginalOrMappedLineColumn(item2);

                    return item1.Id == item2.Id &&
                           item1.Message == item2.Message &&
                           item1.ProjectId == item2.ProjectId &&
                           item1.DocumentId == item2.DocumentId &&
                           lineColumn1.Item1 == lineColumn2.Item1 &&
                           lineColumn1.Item2 == lineColumn2.Item2 &&
                           item1.Severity == item2.Severity;
                }

                return item1.Id == item2.Id &&
                       item1.Message == item2.Message &&
                       item1.ProjectId == item2.ProjectId &&
                       item1.Severity == item2.Severity;
            }

            public int GetHashCode(DiagnosticData obj)
            {
                if (obj.DocumentId != null)
                {
                    var lineColumn = GetOriginalOrMappedLineColumn(obj);

                    return Hash.Combine(obj.Id,
                           Hash.Combine(obj.Message,
                           Hash.Combine(obj.ProjectId,
                           Hash.Combine(obj.DocumentId,
                           Hash.Combine(lineColumn.Item1,
                           Hash.Combine(lineColumn.Item2, (int)obj.Severity))))));
                }

                return Hash.Combine(obj.Id,
                       Hash.Combine(obj.Message,
                       Hash.Combine(obj.ProjectId, (int)obj.Severity)));
            }

            private static ValueTuple<int, int> GetOriginalOrMappedLineColumn(DiagnosticData data)
            {
                var workspace = data.Workspace as VisualStudioWorkspaceImpl;
                if (workspace == null)
                {
                    return ValueTuple.Create(data.DataLocation?.MappedStartLine ?? 0, data.DataLocation?.MappedStartColumn ?? 0);
                }

                var containedDocument = workspace.GetHostDocument(data.DocumentId) as ContainedDocument;
                if (containedDocument == null)
                {
                    return ValueTuple.Create(data.DataLocation?.MappedStartLine ?? 0, data.DataLocation?.MappedStartColumn ?? 0);
                }

                return ValueTuple.Create(data.DataLocation?.OriginalStartLine ?? 0, data.DataLocation?.OriginalStartColumn ?? 0);
            }

            private bool IsNull<T>(T item) where T : class
            {
                return item == null;
            }
        }
    }
}
