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
        private const string DiagnosticsUpdatedEventName = nameof(DiagnosticsUpdated);
        private static readonly DiagnosticEventTaskScheduler s_eventScheduler = new DiagnosticEventTaskScheduler(blockingUpperBound: 100);

        private readonly IAsynchronousOperationListener _listener;
        private readonly EventMap _eventMap;
        private readonly SimpleTaskQueue _eventQueue;

        private readonly object _gate;
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>> _map;
        private readonly Dictionary<Workspace, Dictionary<DocumentId, ImmutableArray<Action<DiagnosticsUpdatedArgs>>>> _documentSubscriptions;

        [ImportingConstructor]
        public DiagnosticService([ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) : this()
        {
            // queue to serialize events.
            _eventMap = new EventMap();

            // use diagnostic event task scheduler so that we never flood async events queue with million of events.
            // queue itself can handle huge number of events but we are seeing OOM due to captured data in pending events.
            _eventQueue = new SimpleTaskQueue(s_eventScheduler);

            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.DiagnosticService);

            _gate = new object();
            _map = new Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>>();
            _documentSubscriptions = new Dictionary<Workspace, Dictionary<DocumentId, ImmutableArray<Action<DiagnosticsUpdatedArgs>>>>();
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

        public IDisposable Subscribe(Workspace workspace, DocumentId documentId, Action<DiagnosticsUpdatedArgs> action)
        {
            // diagnostic service's events are asynchronous events, so any actions against it should be serialized through
            // the events queue
            var eventToken = _listener.BeginAsyncOperation("Subscribe");
            _eventQueue.ScheduleTask(() =>
            {
                lock (_gate)
                {
                    var subscriptions = _documentSubscriptions.GetOrAdd(workspace, _ => new Dictionary<DocumentId, ImmutableArray<Action<DiagnosticsUpdatedArgs>>>());
                    var actions = subscriptions.GetOrAdd(documentId, _ => ImmutableArray<Action<DiagnosticsUpdatedArgs>>.Empty);
                    subscriptions[documentId] = actions.Add(action);
                }

            }).CompletesAsyncOperation(eventToken);

            return new Subscription(this, workspace, documentId, action);
        }

        private void Unsubscribe(Workspace workspace, DocumentId documentId, Action<DiagnosticsUpdatedArgs> action)
        {
            // diagnostic service's events are asynchronous events, so any actions against it should be serialized through
            // the events queue
            var eventToken = _listener.BeginAsyncOperation("Unsubscribe");
            _eventQueue.ScheduleTask(() =>
            {
                lock (_gate)
                {
                    if (!_documentSubscriptions.TryGetValue(workspace, out var subscriptions) ||
                        !subscriptions.TryGetValue(documentId, out var actions))
                    {
                        return;
                    }

                    subscriptions[documentId] = actions.Remove(action);
                    if (subscriptions[documentId].Length > 0)
                    {
                        return;
                    }

                    subscriptions.Remove(documentId);
                    if (subscriptions.Count > 0)
                    {
                        return;
                    }

                    _documentSubscriptions.Remove(workspace);
                }
            }).CompletesAsyncOperation(eventToken);
        }

        private void RaiseDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs args)
        {
            Contract.ThrowIfNull(sender);
            var source = (IDiagnosticUpdateSource)sender;

            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);

            var eventToken = _listener.BeginAsyncOperation(DiagnosticsUpdatedEventName);
            _eventQueue.ScheduleTask(() =>
            {
                if (!UpdateDataMap(source, args))
                {
                    // there is no change, nothing to raise events for.
                    return;
                }

                // first raise global events
                ev.RaiseEvent(handler => handler(sender, args));

                // and then raise document specific events
                RaiseDocumentEvents(args);

            }).CompletesAsyncOperation(eventToken);
        }

        private void RaiseDocumentEvents(DiagnosticsUpdatedArgs args)
        {
            ImmutableArray<Action<DiagnosticsUpdatedArgs>> actions;
            lock (_gate)
            {
                // not document specific diagnostics
                if (args.DocumentId == null)
                {
                    return;
                }

                if (!_documentSubscriptions.TryGetValue(args.Workspace, out var subscriptions) ||
                    !subscriptions.TryGetValue(args.DocumentId, out actions))
                {
                    return;
                }
            }

            foreach (var action in actions)
            {
                action(args);
            }
        }

        private bool UpdateDataMap(IDiagnosticUpdateSource source, DiagnosticsUpdatedArgs args)
        {
            // we expect source who uses this ability to have small number of diagnostics.
            lock (_gate)
            {
                Contract.Requires(_updateSources.Contains(source));

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

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            AssertIfNull(e.Diagnostics);
            RaiseDiagnosticsUpdated(sender, e);
        }

        public IEnumerable<DiagnosticData> GetCachedDiagnostics(
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
                        Contract.Requires(pool.Object.Count == 0 || pool.Object.Count == 1);

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
                Contract.Requires(false, "who returns invalid data?");
            }
        }

        private struct Data
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
                this.Workspace = args.Workspace;
                this.ProjectId = args.ProjectId;
                this.DocumentId = args.DocumentId;
                this.Id = args.Id;
                this.Diagnostics = diagnostics;
            }
        }

        private class Subscription : IDisposable
        {
            private readonly DiagnosticService _service;
            private readonly Workspace _workspace;
            private readonly DocumentId _documentId;
            private readonly Action<DiagnosticsUpdatedArgs> _action;

            public Subscription(
                DiagnosticService service,
                Workspace workspace,
                DocumentId documentId,
                Action<DiagnosticsUpdatedArgs> action)
            {
                _service = service;
                _workspace = workspace;
                _documentId = documentId;
                _action = action;
            }

            public void Dispose()
            {
                _service.Unsubscribe(_workspace, _documentId, _action);
            }
        }
    }
}
