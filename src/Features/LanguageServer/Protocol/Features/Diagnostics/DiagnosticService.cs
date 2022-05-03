// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared]
    internal partial class DiagnosticService : IDiagnosticService
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        private readonly EventMap _eventMap = new();
        private readonly TaskQueue _eventQueue;

        private readonly object _gate = new();
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>> _map = new();

        private readonly EventListenerTracker<IDiagnosticService> _eventListenerTracker;

        public IGlobalOptionService GlobalOptions { get; }

        private ImmutableHashSet<IDiagnosticUpdateSource> _updateSources;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticService(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            GlobalOptions = globalOptions;

            // we use registry service rather than doing MEF import since MEF import method can have race issue where
            // update source gets created before aggregator - diagnostic service - is created and we will lose events fired before
            // the aggregator is created.
            _updateSources = ImmutableHashSet<IDiagnosticUpdateSource>.Empty;

            // queue to serialize events.
            _eventQueue = new TaskQueue(listenerProvider.GetListener(FeatureAttribute.DiagnosticService), TaskScheduler.Default);

            _eventListenerTracker = new EventListenerTracker<IDiagnosticService>(eventListeners, WellKnownEventListeners.DiagnosticService);
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
            _eventListenerTracker.EnsureEventListener(args.Workspace, this);

            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);

            _eventQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
            {
                if (!UpdateDataMap(source, args))
                {
                    // there is no change, nothing to raise events for.
                    return;
                }

                ev.RaiseEvent(handler => handler(source, args));
            }, CancellationToken.None);
        }

        private void RaiseDiagnosticsCleared(IDiagnosticUpdateSource source)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);

            _eventQueue.ScheduleTask(DiagnosticsUpdatedEventName, () =>
            {
                using var pooledObject = SharedPools.Default<List<DiagnosticsUpdatedArgs>>().GetPooledObject();

                var removed = pooledObject.Object;
                if (!ClearDiagnosticsReportedBySource(source, removed))
                {
                    // there is no change, nothing to raise events for.
                    return;
                }

                // don't create event listener if it haven't created yet. if there is a diagnostic to remove
                // listener should have already created since all events are done in the serialized queue
                foreach (var args in removed)
                {
                    ev.RaiseEvent(handler => handler(source, args));
                }
            }, CancellationToken.None);
        }

        private bool UpdateDataMap(IDiagnosticUpdateSource source, DiagnosticsUpdatedArgs args)
        {
            // we expect source who uses this ability to have small number of diagnostics.
            lock (_gate)
            {
                Debug.Assert(_updateSources.Contains(source));

                // The diagnostic service itself caches all diagnostics produced by the IDiagnosticUpdateSource's.  As
                // such, we want to grab all the diagnostics (regardless of push/pull setting) and cache inside
                // ourselves.  Later, when anyone calls GetDiagnostics or GetDiagnosticBuckets we will check if their 
                // push/pull request matches the current user setting and return these if appropriate.
                var diagnostics = args.GetAllDiagnosticsRegardlessOfPushPullSetting();

                // check cheap early bail out
                if (diagnostics.Length == 0 && !_map.ContainsKey(source))
                {
                    // no new diagnostic, and we don't have update source for it.
                    return false;
                }

                // 2 different workspaces (ex, PreviewWorkspaces) can return same Args.Id, we need to
                // distinguish them. so we separate diagnostics per workspace map.
                var workspaceMap = _map.GetOrAdd(source, _ => new Dictionary<Workspace, Dictionary<object, Data>>());

                if (diagnostics.Length == 0 && !workspaceMap.ContainsKey(args.Workspace))
                {
                    // no new diagnostic, and we don't have workspace for it.
                    return false;
                }

                var diagnosticDataMap = workspaceMap.GetOrAdd(args.Workspace, _ => new Dictionary<object, Data>());

                diagnosticDataMap.Remove(args.Id);
                if (diagnosticDataMap.Count == 0 && diagnostics.Length == 0)
                {
                    workspaceMap.Remove(args.Workspace);

                    if (workspaceMap.Count == 0)
                    {
                        _map.Remove(source);
                    }

                    return true;
                }

                if (diagnostics.Length > 0)
                {
                    // save data only if there is a diagnostic
                    var data = source.SupportGetDiagnostics ? new Data(args) : new Data(args, diagnostics);
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
            AssertIfNull(e.GetAllDiagnosticsRegardlessOfPushPullSetting());

            // all events are serialized by async event handler
            RaiseDiagnosticsUpdated((IDiagnosticUpdateSource)sender, e);
        }

        private void OnCleared(object sender, EventArgs e)
        {
            // all events are serialized by async event handler
            RaiseDiagnosticsCleared((IDiagnosticUpdateSource)sender);
        }

        [Obsolete]
        ImmutableArray<DiagnosticData> IDiagnosticService.GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            => GetPushDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, GlobalOptions.GetDiagnosticMode(InternalDiagnosticsOptions.NormalDiagnosticMode), cancellationToken).AsTask().WaitAndGetResult_CanCallOnBackground(cancellationToken);

        public ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            => GetDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, forPullDiagnostics: true, diagnosticMode, cancellationToken);

        public ValueTask<ImmutableArray<DiagnosticData>> GetPushDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            => GetDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, forPullDiagnostics: false, diagnosticMode, cancellationToken);

        private ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            Workspace workspace,
            ProjectId projectId,
            DocumentId documentId,
            object id,
            bool includeSuppressedDiagnostics,
            bool forPullDiagnostics,
            DiagnosticMode diagnosticMode,
            CancellationToken cancellationToken)
        {
            // If this is a pull client, but pull diagnostics is not on, then they get nothing.  Similarly, if this is a
            // push client and pull diagnostics are on, they get nothing.
            if (forPullDiagnostics != (diagnosticMode == DiagnosticMode.Pull))
                return new ValueTask<ImmutableArray<DiagnosticData>>(ImmutableArray<DiagnosticData>.Empty);

            if (id != null)
            {
                // get specific one
                return GetSpecificDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, cancellationToken);
            }

            // get aggregated ones
            return GetDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken);
        }

        private async ValueTask<ImmutableArray<DiagnosticData>> GetSpecificDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Data>.GetInstance(out var buffer);

            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                buffer.Clear();
                if (source.SupportGetDiagnostics)
                {
                    var diagnostics = await source.GetDiagnosticsAsync(workspace, projectId, documentId, id, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false);
                    if (diagnostics.Length > 0)
                        return diagnostics;
                }
                else
                {
                    AppendMatchingData(source, workspace, projectId, documentId, id, buffer);
                    Debug.Assert(buffer.Count is 0 or 1);

                    if (buffer.Count == 1)
                    {
                        var diagnostics = buffer[0].Diagnostics;
                        return includeSuppressedDiagnostics
                            ? diagnostics
                            : diagnostics.NullToEmpty().WhereAsArray(d => !d.IsSuppressed);
                    }
                }
            }

            return ImmutableArray<DiagnosticData>.Empty;
        }

        private async ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            Workspace workspace, ProjectId projectId, DocumentId documentId, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            using var _1 = ArrayBuilder<DiagnosticData>.GetInstance(out var result);
            using var _2 = ArrayBuilder<Data>.GetInstance(out var buffer);
            foreach (var source in _updateSources)
            {
                cancellationToken.ThrowIfCancellationRequested();

                buffer.Clear();
                if (source.SupportGetDiagnostics)
                {
                    result.AddRange(await source.GetDiagnosticsAsync(workspace, projectId, documentId, id: null, includeSuppressedDiagnostics, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    AppendMatchingData(source, workspace, projectId, documentId, id: null, buffer);

                    foreach (var data in buffer)
                    {
                        foreach (var diagnostic in data.Diagnostics)
                        {
                            AssertIfNull(diagnostic);
                            if (includeSuppressedDiagnostics || !diagnostic.IsSuppressed)
                                result.Add(diagnostic);
                        }
                    }
                }
            }

            return result.ToImmutable();
        }

        public ImmutableArray<DiagnosticBucket> GetPullDiagnosticBuckets(Workspace workspace, ProjectId projectId, DocumentId documentId, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            => GetDiagnosticBuckets(workspace, projectId, documentId, forPullDiagnostics: true, diagnosticMode, cancellationToken);

        public ImmutableArray<DiagnosticBucket> GetPushDiagnosticBuckets(Workspace workspace, ProjectId projectId, DocumentId documentId, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            => GetDiagnosticBuckets(workspace, projectId, documentId, forPullDiagnostics: false, diagnosticMode, cancellationToken);

        private ImmutableArray<DiagnosticBucket> GetDiagnosticBuckets(
            Workspace workspace,
            ProjectId projectId,
            DocumentId documentId,
            bool forPullDiagnostics,
            DiagnosticMode diagnosticMode,
            CancellationToken cancellationToken)
        {
            // If this is a pull client, but pull diagnostics is not on, then they get nothing.  Similarly, if this is a
            // push client and pull diagnostics are on, they get nothing.
            if (forPullDiagnostics != (diagnosticMode == DiagnosticMode.Pull))
                return ImmutableArray<DiagnosticBucket>.Empty;

            using var _1 = ArrayBuilder<DiagnosticBucket>.GetInstance(out var result);
            using var _2 = ArrayBuilder<Data>.GetInstance(out var buffer);

            foreach (var source in _updateSources)
            {
                buffer.Clear();
                cancellationToken.ThrowIfCancellationRequested();

                AppendMatchingData(source, workspace, projectId, documentId, id: null, buffer);
                foreach (var data in buffer)
                    result.Add(new DiagnosticBucket(data.Id, data.Workspace, data.ProjectId, data.DocumentId));
            }

            return result.ToImmutable();
        }

        private void AppendMatchingData(
            IDiagnosticUpdateSource source, Workspace workspace, ProjectId projectId, DocumentId documentId, object id, ArrayBuilder<Data> list)
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

        private static bool TryAddData<T>(Workspace workspace, T key, Data data, Func<Data, T> keyGetter, ArrayBuilder<Data> result) where T : class
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
        private static void AssertIfNull(ImmutableArray<DiagnosticData> diagnostics)
        {
            for (var i = 0; i < diagnostics.Length; i++)
            {
                AssertIfNull(diagnostics[i]);
            }
        }

        [Conditional("DEBUG")]
        private static void AssertIfNull<T>(T obj) where T : class
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

            public Data(UpdatedEventArgs args)
                : this(args, ImmutableArray<DiagnosticData>.Empty)
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

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly DiagnosticService _diagnosticService;

            internal TestAccessor(DiagnosticService diagnosticService)
                => _diagnosticService = diagnosticService;

            internal ref readonly EventListenerTracker<IDiagnosticService> EventListenerTracker
                => ref _diagnosticService._eventListenerTracker;
        }
    }
}
