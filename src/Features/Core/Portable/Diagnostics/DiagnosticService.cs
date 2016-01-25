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
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<object, Data>> _map;

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
            _map = new Dictionary<IDiagnosticUpdateSource, Dictionary<object, Data>>();
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

        private void RaiseDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs args)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                var eventToken = _listener.BeginAsyncOperation(DiagnosticsUpdatedEventName);
                _eventQueue.ScheduleTask(() =>
                {
                    UpdateDataMap(sender, args);
                    ev.RaiseEvent(handler => handler(sender, args));
                }).CompletesAsyncOperation(eventToken);
            }
        }

        private void UpdateDataMap(object sender, DiagnosticsUpdatedArgs args)
        {
            var updateSource = sender as IDiagnosticUpdateSource;
            if (updateSource == null)
            {
                return;
            }

            Contract.Requires(_updateSources.Contains(updateSource));

            // we expect someone who uses this ability to small.
            lock (_gate)
            {
                // check cheap early bail out
                if (args.Diagnostics.Length == 0 && !_map.ContainsKey(updateSource))
                {
                    // no new diagnostic, and we don't have update source for it.
                    return;
                }

                var list = _map.GetOrAdd(updateSource, _ => new Dictionary<object, Data>());
                var data = updateSource.SupportGetDiagnostics ? new Data(args) : new Data(args, args.Diagnostics);

                list.Remove(data.Id);
                if (list.Count == 0 && args.Diagnostics.Length == 0)
                {
                    _map.Remove(updateSource);
                    return;
                }

                list.Add(args.Id, data);
            }
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            AssertIfNull(e.Diagnostics);
            RaiseDiagnosticsUpdated(sender, e);
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

        private static IEnumerable<DiagnosticData> FilterSuppressedDiagnostics(IEnumerable<DiagnosticData> diagnostics)
        {
            if (diagnostics != null)
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

        public IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
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
            lock (_gate)
            {
                Dictionary<object, Data> current;
                if (!_map.TryGetValue(source, out current))
                {
                    return;
                }

                if (id != null)
                {
                    Data data;
                    if (current.TryGetValue(id, out data))
                    {
                        list.Add(data);
                    }

                    return;
                }

                foreach (var data in current.Values)
                {
                    if (TryAddData(documentId, data, d => d.DocumentId, list) ||
                        TryAddData(projectId, data, d => d.ProjectId, list) ||
                        TryAddData(workspace, data, d => d.Workspace, list))
                    {
                        continue;
                    }
                }
            }
        }

        private bool TryAddData<T>(T key, Data data, Func<Data, T> keyGetter, List<Data> result) where T : class
        {
            if (key == null)
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
        private void AssertIfNull(DiagnosticData diagnostic)
        {
            if (diagnostic == null)
            {
                Contract.Requires(false, "who returns invalid data?");
            }
        }

        private struct Data : IEquatable<Data>
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

            public bool Equals(Data other)
            {
                return this.Workspace == other.Workspace &&
                       this.ProjectId == other.ProjectId &&
                       this.DocumentId == other.DocumentId &&
                       this.Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                return (obj is Data) && Equals((Data)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(Workspace,
                       Hash.Combine(ProjectId,
                       Hash.Combine(DocumentId,
                       Hash.Combine(Id, 1))));
            }
        }
    }
}
