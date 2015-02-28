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
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(IDiagnosticUpdateSource))]
    [Export(typeof(ExternalErrorDiagnosticUpdateSource))]
    internal class ExternalErrorDiagnosticUpdateSource : IDiagnosticUpdateSource
    {
        private readonly Workspace _workspace;
        private readonly IDiagnosticUpdateSource _diagnosticService;

        private readonly SimpleTaskQueue _taskQueue;
        private readonly IAsynchronousOperationListener _listener;

        private readonly object _gate = new object();

        // errors reported by build that is not reported by live errors
        private readonly Dictionary<ProjectId, HashSet<DiagnosticData>> _projectToDiagnosticsMap = new Dictionary<ProjectId, HashSet<DiagnosticData>>();
        private readonly Dictionary<DocumentId, HashSet<DiagnosticData>> _documentToDiagnosticsMap = new Dictionary<DocumentId, HashSet<DiagnosticData>>();

        [ImportingConstructor]
        public ExternalErrorDiagnosticUpdateSource(
            VisualStudioWorkspaceImpl workspace,
            IDiagnosticAnalyzerService diagnosticService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
                this(workspace, diagnosticService, new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.ErrorList))
        {
            Contract.Requires(!KnownUIContexts.SolutionBuildingContext.IsActive);
            KnownUIContexts.SolutionBuildingContext.UIContextChanged += OnSolutionBuild;
        }

        /// <summary>
        /// Test Only
        /// </summary>
        public ExternalErrorDiagnosticUpdateSource(
            Workspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IAsynchronousOperationListener listener)
        {
            // use queue to serialize work. no lock needed
            _taskQueue = new SimpleTaskQueue(TaskScheduler.Default);
            _listener = listener;

            _workspace = workspace;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            _diagnosticService = (IDiagnosticUpdateSource)diagnosticService;
            _diagnosticService.DiagnosticsUpdated += OnDiagnosticUpdated;
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        public bool SupportGetDiagnostics { get { return true; } }

        public ImmutableArray<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, CancellationToken cancellationToken)
        {
            if (workspace != _workspace)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            if (id != null)
            {
                return GetSpecificDiagnostics(projectId, documentId, id);
            }

            if (documentId != null)
            {
                return GetSpecificDiagnostics(_documentToDiagnosticsMap, documentId);
            }

            if (projectId != null)
            {
                return GetProjectDiagnostics(projectId, cancellationToken);
            }

            return GetSolutionDiagnostics(workspace.CurrentSolution, cancellationToken);
        }

        private ImmutableArray<DiagnosticData> GetSolutionDiagnostics(Solution solution, CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<DiagnosticData>();
            foreach (var projectId in solution.ProjectIds)
            {
                builder.AddRange(GetProjectDiagnostics(projectId, cancellationToken));
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<DiagnosticData> GetProjectDiagnostics(ProjectId projectId, CancellationToken cancellationToken)
        {
            List<DocumentId> documentIds;
            lock (_gate)
            {
                documentIds = _documentToDiagnosticsMap.Keys.Where(d => d.ProjectId == projectId).ToList();
            }

            var builder = ImmutableArray.CreateBuilder<DiagnosticData>(documentIds.Count + 1);
            builder.AddRange(GetSpecificDiagnostics(_projectToDiagnosticsMap, projectId));

            foreach (var documentId in documentIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                builder.AddRange(GetSpecificDiagnostics(_documentToDiagnosticsMap, documentId));
            }

            return builder.ToImmutable();
        }

        private ImmutableArray<DiagnosticData> GetSpecificDiagnostics(ProjectId projectId, DocumentId documentId, object id)
        {
            var key = id as ArgumentKey;
            if (key == null)
            {
                return ImmutableArray<DiagnosticData>.Empty;
            }

            Contract.Requires(documentId == key.DocumentId);
            if (documentId != null)
            {
                return GetSpecificDiagnostics(_documentToDiagnosticsMap, documentId);
            }

            Contract.Requires(projectId == key.ProjectId);
            if (projectId != null)
            {
                return GetSpecificDiagnostics(_projectToDiagnosticsMap, projectId);
            }

            return Contract.FailWithReturn<ImmutableArray<DiagnosticData>>("shouldn't reach here");
        }

        private ImmutableArray<DiagnosticData> GetSpecificDiagnostics<T>(Dictionary<T, HashSet<DiagnosticData>> map, T key)
        {
            lock (_gate)
            {
                HashSet<DiagnosticData> data;
                if (map.TryGetValue(key, out data))
                {
                    return data.ToImmutableArrayOrEmpty();
                }

                return ImmutableArray<DiagnosticData>.Empty;
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
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnSolutionChanged");
                        _taskQueue.ScheduleTask(() => e.OldSolution.ProjectIds.Do(p => ClearProjectErrors(p))).CompletesAsyncOperation(asyncToken);
                        break;
                    }

                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectReloaded:
                    {
                        var asyncToken = _listener.BeginAsyncOperation("OnProjectChanged");
                        _taskQueue.ScheduleTask(() => ClearProjectErrors(e.ProjectId)).CompletesAsyncOperation(asyncToken);
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
            if (e.Activated)
            {
                return;
            }

            var asyncToken = _listener.BeginAsyncOperation("OnSolutionBuild");
            _taskQueue.ScheduleTask(() =>
            {
                // build is done, remove live error and report errors
                IDictionary<ProjectId, IList<DiagnosticData>> liveProjectErrors;
                IDictionary<DocumentId, IList<DiagnosticData>> liveDocumentErrors;
                GetLiveProjectAndDocumentErrors(out liveProjectErrors, out liveDocumentErrors);

                using (var documentIds = SharedPools.Default<List<DocumentId>>().GetPooledObject())
                using (var projectIds = SharedPools.Default<List<ProjectId>>().GetPooledObject())
                {
                    lock (_gate)
                    {
                        documentIds.Object.AddRange(_documentToDiagnosticsMap.Keys);
                        projectIds.Object.AddRange(_projectToDiagnosticsMap.Keys);
                    }

                    foreach (var documentId in documentIds.Object)
                    {
                        var errors = liveDocumentErrors.GetValueOrDefault(documentId);
                        RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(documentId.ProjectId, documentId, errors, reportOnlyIfChanged: false);
                    }

                    foreach (var projectId in projectIds.Object)
                    {
                        var errors = liveProjectErrors.GetValueOrDefault(projectId);
                        RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(projectId, null, errors, reportOnlyIfChanged: false);
                    }
                }
            }).CompletesAsyncOperation(asyncToken);
        }

        private void OnDiagnosticUpdated(object sender, DiagnosticsUpdatedArgs args)
        {
            if (args.Diagnostics.Length == 0 || _workspace != args.Workspace)
            {
                return;
            }

            // live errors win over build errors
            var asyncToken = _listener.BeginAsyncOperation("OnDiagnosticUpdated");
            _taskQueue.ScheduleTask(
                () => RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(args.ProjectId, args.DocumentId, args.Diagnostics, reportOnlyIfChanged: true)).CompletesAsyncOperation(asyncToken);
        }

        private void RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(ProjectId projectId, DocumentId documentId, IEnumerable<DiagnosticData> liveErrors, bool reportOnlyIfChanged)
        {
            if (documentId != null)
            {
                RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(_documentToDiagnosticsMap, documentId, projectId, documentId, liveErrors, reportOnlyIfChanged);
                return;
            }

            if (projectId != null)
            {
                RemoveBuildErrorsDuplicatedByLiveErrorsAndReport(_projectToDiagnosticsMap, projectId, projectId, null, liveErrors, reportOnlyIfChanged);
                return;
            }

            // diagnostic errors without any associated workspace project/document?
            Contract.Requires(false, "how can this happen?");
        }

        private void RemoveBuildErrorsDuplicatedByLiveErrorsAndReport<T>(
            Dictionary<T, HashSet<DiagnosticData>> buildErrorMap, T key,
            ProjectId projectId, DocumentId documentId, IEnumerable<DiagnosticData> liveErrors, bool reportOnlyIfChanged)
        {
            ImmutableArray<DiagnosticData> items;

            lock (_gate)
            {
                HashSet<DiagnosticData> buildErrors;
                if (!buildErrorMap.TryGetValue(key, out buildErrors))
                {
                    return;
                }

                var originalBuildErrorCount = buildErrors.Count;
                if (liveErrors != null)
                {
                    buildErrors.ExceptWith(liveErrors);
                }

                if (buildErrors.Count == 0)
                {
                    buildErrorMap.Remove(key);
                }

                // nothing to refresh.
                if (originalBuildErrorCount == 0)
                {
                    return;
                }

                // if nothing has changed and we are asked to report only when something has changed
                if (reportOnlyIfChanged && originalBuildErrorCount == buildErrors.Count)
                {
                    return;
                }

                items = buildErrors.ToImmutableArrayOrEmpty();
            }

            RaiseDiagnosticsUpdated(key, projectId, documentId, items);
        }

        public void ClearErrors(ProjectId projectId)
        {
            var asyncToken = _listener.BeginAsyncOperation("ClearErrors");
            _taskQueue.ScheduleTask(() => ClearProjectErrors(projectId)).CompletesAsyncOperation(asyncToken);
        }

        private void ClearProjectErrors(ProjectId projectId)
        {
            var clearProjectError = false;
            using (var pool = SharedPools.Default<List<DocumentId>>().GetPooledObject())
            {
                lock (_gate)
                {
                    clearProjectError = _projectToDiagnosticsMap.Remove(projectId);
                    pool.Object.AddRange(_documentToDiagnosticsMap.Keys.Where(k => k.ProjectId == projectId));
                }

                if (clearProjectError)
                {
                    // remove all project errors
                    RaiseDiagnosticsUpdated(projectId, projectId, null, ImmutableArray<DiagnosticData>.Empty);
                }

                // remove all document errors
                foreach (var documentId in pool.Object)
                {
                    ClearDocumentErrors(projectId, documentId);
                }
            }
        }

        private void ClearDocumentErrors(ProjectId projectId, DocumentId documentId)
        {
            lock (_gate)
            {
                _documentToDiagnosticsMap.Remove(documentId);
            }

            RaiseDiagnosticsUpdated(documentId, projectId, documentId, ImmutableArray<DiagnosticData>.Empty);
        }

        public void AddNewErrors(DocumentId documentId, DiagnosticData diagnostic)
        {
            var asyncToken = _listener.BeginAsyncOperation("Document New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                lock (_gate)
                {
                    var errors = _documentToDiagnosticsMap.GetOrAdd(documentId, _ => new HashSet<DiagnosticData>(DiagnosticDataComparer.Instance));
                    errors.Add(diagnostic);
                }
            }).CompletesAsyncOperation(asyncToken);
        }

        public void AddNewErrors(
            ProjectId projectId, HashSet<DiagnosticData> projectErrorSet, Dictionary<DocumentId, HashSet<DiagnosticData>> documentErrorMap)
        {
            var asyncToken = _listener.BeginAsyncOperation("Project New Errors");
            _taskQueue.ScheduleTask(() =>
            {
                lock (_gate)
                {
                    foreach (var kv in documentErrorMap)
                    {
                        var documentErrors = _documentToDiagnosticsMap.GetOrAdd(kv.Key, _ => new HashSet<DiagnosticData>(DiagnosticDataComparer.Instance));
                        documentErrors.UnionWith(kv.Value);
                    }

                    var projectErrors = _projectToDiagnosticsMap.GetOrAdd(projectId, _ => new HashSet<DiagnosticData>(DiagnosticDataComparer.Instance));
                    projectErrors.UnionWith(projectErrorSet);
                }
            }).CompletesAsyncOperation(asyncToken);
        }

        private void GetLiveProjectAndDocumentErrors(
            out IDictionary<ProjectId, IList<DiagnosticData>> projectErrors,
            out IDictionary<DocumentId, IList<DiagnosticData>> documentErrors)
        {
            projectErrors = null;
            documentErrors = null;

            foreach (var diagnostic in _diagnosticService.GetDiagnostics(_workspace, id: null, projectId: null, documentId: null, cancellationToken: CancellationToken.None))
            {
                if (diagnostic.DocumentId != null)
                {
                    documentErrors = documentErrors ?? new Dictionary<DocumentId, IList<DiagnosticData>>();

                    var errors = documentErrors.GetOrAdd(diagnostic.DocumentId, _ => new List<DiagnosticData>());
                    errors.Add(diagnostic);
                    continue;
                }

                if (diagnostic.ProjectId != null)
                {
                    projectErrors = projectErrors ?? new Dictionary<ProjectId, IList<DiagnosticData>>();

                    var errors = projectErrors.GetOrAdd(diagnostic.ProjectId, _ => new List<DiagnosticData>());
                    errors.Add(diagnostic);
                    continue;
                }

                Contract.Requires(false, "shouldn't happen");
            }

            projectErrors = projectErrors ?? SpecializedCollections.EmptyDictionary<ProjectId, IList<DiagnosticData>>();
            documentErrors = documentErrors ?? SpecializedCollections.EmptyDictionary<DocumentId, IList<DiagnosticData>>();
        }

        private void RaiseDiagnosticsUpdated(object id, ProjectId projectId, DocumentId documentId, ImmutableArray<DiagnosticData> items)
        {
            var diagnosticsUpdated = DiagnosticsUpdated;
            if (diagnosticsUpdated != null)
            {
                diagnosticsUpdated(this, new DiagnosticsUpdatedArgs(
                   new ArgumentKey(id), _workspace, _workspace.CurrentSolution, projectId, documentId, items));
            }
        }

        private class ArgumentKey
        {
            public object Key;

            public ArgumentKey(object key)
            {
                this.Key = key;
            }

            public DocumentId DocumentId
            {
                get { return this.Key as DocumentId; }
            }

            public ProjectId ProjectId
            {
                get { return this.Key as ProjectId; }
            }

            public override bool Equals(object obj)
            {
                var other = obj as ArgumentKey;
                if (other == null)
                {
                    return false;
                }

                return Key == other.Key;
            }

            public override int GetHashCode()
            {
                return this.Key.GetHashCode();
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
                    return ValueTuple.Create(data.MappedStartLine, data.MappedStartColumn);
                }

                var containedDocument = workspace.GetHostDocument(data.DocumentId) as ContainedDocument;
                if (containedDocument == null)
                {
                    return ValueTuple.Create(data.MappedStartLine, data.MappedStartColumn);
                }

                return ValueTuple.Create(data.OriginalStartLine, data.OriginalStartColumn);
            }

            private bool IsNull<T>(T item) where T : class
            {
                return item == null;
            }
        }
    }
}
