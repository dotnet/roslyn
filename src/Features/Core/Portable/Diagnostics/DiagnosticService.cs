﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared]
    internal class DiagnosticService : IDiagnosticService
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        private readonly IAsynchronousOperationListener _listener;
        private readonly EventMap _eventMap;
        private readonly SimpleTaskQueue _eventQueue;
        private readonly ImmutableArray<IDiagnosticUpdateSource> _updateSources;

        private readonly object _gate;
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<object, Data>> _map;

        [ImportingConstructor]
        public DiagnosticService(
            [ImportMany] IEnumerable<IDiagnosticUpdateSource> diagnosticUpdateSource,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            // queue to serialize events.
            _eventMap = new EventMap();
            _eventQueue = new SimpleTaskQueue(TaskScheduler.Default);

            _updateSources = diagnosticUpdateSource.AsImmutable();
            _listener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.DiagnosticService);

            _gate = new object();
            _map = new Dictionary<IDiagnosticUpdateSource, Dictionary<object, Data>>();

            // connect each diagnostic update source to events
            ConnectDiagnosticsUpdatedEvents();
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
            if (updateSource == null || updateSource.SupportGetDiagnostics)
            {
                return;
            }

            Contract.Requires(_updateSources.IndexOf(updateSource) >= 0);

            // we expect someone who uses this ability to small.
            lock (_gate)
            {
                var list = _map.GetOrAdd(updateSource, _ => new Dictionary<object, Data>());
                var data = new Data(args);

                list.Remove(data.Id);
                if (list.Count == 0 && args.Diagnostics.Length == 0)
                {
                    _map.Remove(updateSource);
                    return;
                }

                list.Add(args.Id, data);
            }
        }

        private void ConnectDiagnosticsUpdatedEvents()
        {
            foreach (var source in _updateSources)
            {
                source.DiagnosticsUpdated += OnDiagnosticsUpdated;
            }
        }

        private void OnDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs e)
        {
            RaiseDiagnosticsUpdated(sender, e);
        }

        public IEnumerable<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, object id, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                // get specific one
                return GetSpecificDiagnostics(workspace, projectId, documentId, id, cancellationToken);
            }

            // get aggregated ones
            return GetDiagnostics(workspace, projectId, documentId, cancellationToken);
        }

        private IEnumerable<DiagnosticData> GetSpecificDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, CancellationToken cancellationToken)
        {
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (source.SupportGetDiagnostics)
                {
                    var diagnostics = source.GetDiagnostics(workspace, projectId, documentId, id, cancellationToken);
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
                            return pool.Object[0].Diagnostics;
                        }
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
        }

        private IEnumerable<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
        {
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (source.SupportGetDiagnostics)
                {
                    foreach (var diagnostic in source.GetDiagnostics(workspace, projectId, documentId, null, cancellationToken))
                    {
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
                                yield return diagnostic;
                            }
                        }
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

        private struct Data : IEquatable<Data>
        {
            public readonly Workspace Workspace;
            public readonly ProjectId ProjectId;
            public readonly DocumentId DocumentId;
            public readonly object Id;
            public readonly ImmutableArray<DiagnosticData> Diagnostics;

            public Data(DiagnosticsUpdatedArgs args)
            {
                this.Workspace = args.Workspace;
                this.ProjectId = args.ProjectId;
                this.DocumentId = args.DocumentId;
                this.Id = args.Id;
                this.Diagnostics = args.Diagnostics;
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
