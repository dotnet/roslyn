// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared]
    internal partial class DiagnosticService : IDiagnosticService
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        private static readonly DiagnosticEventTaskScheduler s_eventScheduler = new DiagnosticEventTaskScheduler(blockingUpperBound: 100);

        private readonly IAsynchronousOperationListener _listener;
        private readonly EventMap _eventMap;
        private readonly SimpleTaskQueue _eventQueue;

        private readonly object _gate;
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>> _map;

        [ImportingConstructor]
        public DiagnosticService(IAsynchronousOperationListenerProvider listenerProvider) : this()
        {
            // queue to serialize events.
            _eventMap = new EventMap();

            // use diagnostic event task scheduler so that we never flood async events queue with million of events.
            // queue itself can handle huge number of events but we are seeing OOM due to captured data in pending events.
            _eventQueue = new SimpleTaskQueue(s_eventScheduler);

            _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);

            _gate = new object();
            _map = new Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>>();
        }

        public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated
        {
            add
            {
                _eventMap.AddEventHandler(DiagnosticsUpdatedEventName, value);
            }

            remove
            {
                _eventMap.RemoveEventHandler(DiagnosticsUpdatedEventName, value);
            }
        }

        private void RaiseDiagnosticsUpdated(IDiagnosticUpdateSource source, DiagnosticsUpdatedArgs args)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (!RequireRunningEventTasks(source, ev))
            {
                return;
            }

            var eventToken = _listener.BeginAsyncOperation(DiagnosticsUpdatedEventName);
            _eventQueue.ScheduleTask(() =>
            {
                if (!UpdateDataMap(source, args))
                {
                    // there is no change, nothing to raise events for.
                    return;
                }

                ev.RaiseEvent(handler => handler(source, args));
            }).CompletesAsyncOperation(eventToken);
        }

        private void RaiseDiagnosticsCleared(IDiagnosticUpdateSource source)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (!RequireRunningEventTasks(source, ev))
            {
                return;
            }

            var eventToken = _listener.BeginAsyncOperation(DiagnosticsUpdatedEventName);
            _eventQueue.ScheduleTask(() =>
            {
                using (var pooledObject = SharedPools.Default<List<DiagnosticsUpdatedArgs>>().GetPooledObject())
                {
                    var removed = pooledObject.Object;
                    if (!ClearDiagnosticsReportedBySource(source, removed))
                    {
                        // there is no change, nothing to raise events for.
                        return;
                    }

                    foreach (var args in removed)
                    {
                        ev.RaiseEvent(handler => handler(source, args));
                    }
                }
            }).CompletesAsyncOperation(eventToken);
        }

        private bool RequireRunningEventTasks(
            IDiagnosticUpdateSource source, EventMap.EventHandlerSet<EventHandler<DiagnosticsUpdatedArgs>> ev)
        {
            // basically there are 2 cases when there is no event handler registered. 
            // first case is when diagnostic update source itself provide GetDiagnostics functionality. 
            // in that case, DiagnosticService doesn't need to track diagnostics reported. so, it bail out right away.
            // second case is when diagnostic source doesn't provide GetDiagnostics functionality. 
            // in that case, DiagnosticService needs to track diagnostics reported. so it need to enqueue background 
            // work to process given data regardless whether there is event handler registered or not.
            // this could be separated in 2 tasks, but we already saw cases where there are too many tasks enqueued, 
            // so I merged it to one. 

            // if it doesn't SupportGetDiagnostics, we need to process reported data, so enqueue task.
            if (!source.SupportGetDiagnostics)
            {
                return true;
            }

            return ev.HasHandlers;
        }

        private bool UpdateDataMap(IDiagnosticUpdateSource source, DiagnosticsUpdatedArgs args)
        {
            // we expect source who uses this ability to have small number of diagnostics.
            lock (_gate)
            {
                Debug.Assert(_updateSources.Contains(source));

                // check cheap early bail out
                if (args.Diagnostics.Length == 0 && !_map.ContainsKey(source))
                {
                    // no new diagnostic, and we don't have update source for it.
                    return false;
                }

                // 2 different workspaces (ex, PreviewWorkspaces) can return same Args.Id, we need to
                // distinguish them. so we separate diagnostics per workspace map.
                var workspaceMap = _map.GetOrAdd(source, _ => new Dictionary<Workspace, Dictionary<object, Data>>());

                if (args.Diagnostics.Length == 0 && !workspaceMap.ContainsKey(args.Workspace))
                {
                    // no new diagnostic, and we don't have workspace for it.
                    return false;
                }

                var diagnosticDataMap = workspaceMap.GetOrAdd(args.Workspace, _ => new Dictionary<object, Data>());

                diagnosticDataMap.Remove(args.Id);
                if (diagnosticDataMap.Count == 0 && args.Diagnostics.Length == 0)
                {
                    workspaceMap.Remove(args.Workspace);

                    if (workspaceMap.Count == 0)
                    {
                        _map.Remove(source);
                    }

                    return true;
                }

                if (args.Diagnostics.Length > 0)
                {
                    // save data only if there is a diagnostic
                    var data = source.SupportGetDiagnostics ? new Data(args) : new Data(args, args.Diagnostics);
                    diagnosticDataMap.Add(args.Id, data);
                }

                return true;
            }
        }

        private bool ClearDiagnosticsReportedBySource(IDiagnosticUpdateSource source, List<DiagnosticsUpdatedArgs> removed)
        {
            // we expect source who uses this ability to have small number of diagnostics.
            lock (_gate)
            {
                Debug.Assert(_updateSources.Contains(source));

                // 2 different workspaces (ex, PreviewWorkspaces) can return same Args.Id, we need to
                // distinguish them. so we separate diagnostics per workspace map.
                if (!_map.TryGetValue(source, out var workspaceMap))
                {
                    return false;
                }

                foreach (var (workspace, map) in workspaceMap)
                {
                    foreach (var (id, data) in map)
                    {
                        removed.Add(DiagnosticsUpdatedArgs.DiagnosticsRemoved(id, data.Workspace, solution: null, data.ProjectId, data.DocumentId));
                    }
                }

                // all diagnostics from the source is cleared
                _map.Remove(source);
                return true;
            }
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            AssertIfNull(e.Diagnostics);

            // all events are serialized by async event handler
            RaiseDiagnosticsUpdated((IDiagnosticUpdateSource)sender, e);
        }

        private void OnCleared(object sender, EventArgs e)
        {
            // all events are serialized by async event handler
            RaiseDiagnosticsCleared((IDiagnosticUpdateSource)sender);
        }

        public IEnumerable<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                // get specific one
                return GetSpecificDiagnostics(workspace, projectId, documentId, id, includeSuppressedDiagnostics, cancellationToken);
            }

            // get aggregated ones
            return GetDiagnostics(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
        }

        private IEnumerable<DiagnosticData> GetSpecificDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (source.SupportGetDiagnostics)
                {
                    var diagnostics = source.GetDiagnostics(workspace, projectId, documentId, id, includeSuppressedDiagnostics, cancellationToken);
                    if (diagnostics != null && diagnostics.Length > 0)
                    {
                        return diagnostics;
                    }
                }
                else
                {
                    using (var pool = SharedPools.Default<List<Data>>().GetPooledObject())
                    {
                        AppendMatchingData(source, workspace, projectId, documentId, id, pool.Object);
                        Debug.Assert(pool.Object.Count == 0 || pool.Object.Count == 1);

                        if (pool.Object.Count == 1)
                        {
                            var diagnostics = pool.Object[0].Diagnostics;
                            return !includeSuppressedDiagnostics ? FilterSuppressedDiagnostics(diagnostics) : diagnostics;
                        }
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
        }

        private static IEnumerable<DiagnosticData> FilterSuppressedDiagnostics(ImmutableArray<DiagnosticData> diagnostics)
        {
            if (!diagnostics.IsDefault)
            {
                foreach (var diagnostic in diagnostics)
                {
                    if (!diagnostic.IsSuppressed)
                    {
                        yield return diagnostic;
                    }
                }
            }
        }

        private IEnumerable<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (source.SupportGetDiagnostics)
                {
                    foreach (var diagnostic in source.GetDiagnostics(workspace, projectId, documentId, null, includeSuppressedDiagnostics, cancellationToken))
                    {
                        AssertIfNull(diagnostic);
                        yield return diagnostic;
                    }
                }
                else
                {
                    using (var list = SharedPools.Default<List<Data>>().GetPooledObject())
                    {
                        AppendMatchingData(source, workspace, projectId, documentId, null, list.Object);

                        foreach (var data in list.Object)
                        {
                            foreach (var diagnostic in data.Diagnostics)
                            {
                                AssertIfNull(diagnostic);
                                if (includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                                {
                                    yield return diagnostic;
                                }
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(
            Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using (var list = SharedPools.Default<List<Data>>().GetPooledObject())
                {
                    AppendMatchingData(source, workspace, projectId, documentId, null, list.Object);

                    foreach (var data in list.Object)
                    {
                        yield return new UpdatedEventArgs(data.Id, data.Workspace, data.ProjectId, data.DocumentId);
                    }
                }
            }
        }

        private void AppendMatchingData(
            IDiagnosticUpdateSource source, Workspace workspace, ProjectId projectId, DocumentId documentId, object id, List<Data> list)
        {
            Contract.ThrowIfNull(workspace);

            lock (_gate)
            {
                if (!_map.TryGetValue(source, out var workspaceMap) ||
                    !workspaceMap.TryGetValue(workspace, out var current))
                {
                    return;
                }

                if (id != null)
                {
                    if (current.TryGetValue(id, out var data))
                    {
                        list.Add(data);
                    }

                    return;
                }

                foreach (var data in current.Values)
                {
                    if (TryAddData(workspace, documentId, data, d => d.DocumentId, list) ||
                        TryAddData(workspace, projectId, data, d => d.ProjectId, list) ||
                        TryAddData(workspace, workspace, data, d => d.Workspace, list))
                    {
                        continue;
                    }
                }
            }
        }

        private bool TryAddData<T>(Workspace workspace, T key, Data data, Func<Data, T> keyGetter, List<Data> result) where T : class
        {
            if (key == null)
            {
                return false;
            }

            // make sure data is from same workspace. project/documentId can be shared between 2 different workspace
            if (workspace != data.Workspace)
            {
                return false;
            }

            if (key == keyGetter(data))
            {
                result.Add(data);
            }

            return true;
        }

        [Conditional("DEBUG")]
        private void AssertIfNull(ImmutableArray<DiagnosticData> diagnostics)
        {
            for (var i = 0; i < diagnostics.Length; i++)
            {
                AssertIfNull(diagnostics[i]);
            }
        }

        [Conditional("DEBUG")]
        private void AssertIfNull<T>(T obj) where T : class
        {
            if (obj == null)
            {
                Debug.Assert(false, "who returns invalid data?");
            }
        }

        private readonly struct Data
        {
            public readonly Workspace Workspace;
            public readonly ProjectId ProjectId;
            public readonly DocumentId DocumentId;
            public readonly object Id;
            public readonly ImmutableArray<DiagnosticData> Diagnostics;

            public Data(UpdatedEventArgs args) :
                this(args, ImmutableArray<DiagnosticData>.Empty)
            {
            }

            public Data(UpdatedEventArgs args, ImmutableArray<DiagnosticData> diagnostics)
            {
                Workspace = args.Workspace;
                ProjectId = args.ProjectId;
                DocumentId = args.DocumentId;
                Id = args.Id;
                Diagnostics = diagnostics;
            }
        }
    }
}
