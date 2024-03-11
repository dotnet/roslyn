// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticService)), Shared]
    internal partial class DiagnosticService : IDiagnosticService, IDisposable
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        private readonly EventMap _eventMap = new();
        private readonly CancellationTokenSource _eventQueueCancellation = new();
        private readonly AsyncBatchingWorkQueue<(BatchOperation operation, IDiagnosticUpdateSource source, ImmutableArray<DiagnosticsUpdatedArgs> argsCollection)> _eventQueue;

        private readonly object _gate = new();
        private readonly Dictionary<IDiagnosticUpdateSource, Dictionary<Workspace, Dictionary<object, Data>>> _map = [];

        private ImmutableHashSet<IDiagnosticUpdateSource> _updateSources;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticService(
            IAsynchronousOperationListenerProvider listenerProvider,
            [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            // we use registry service rather than doing MEF import since MEF import method can have race issue where
            // update source gets created before aggregator - diagnostic service - is created and we will lose events
            // fired before the aggregator is created.
            _updateSources = [];

            // queue to serialize events.
            _eventQueue = new AsyncBatchingWorkQueue<(BatchOperation operation, IDiagnosticUpdateSource source, ImmutableArray<DiagnosticsUpdatedArgs> argsCollection)>(
                delay: TimeSpan.Zero,
                ProcessEventsBatchAsync,
                listenerProvider.GetListener(FeatureAttribute.DiagnosticService),
                _eventQueueCancellation.Token);
        }

        private enum BatchOperation
        {
            DiagnosticsUpdated,
            DiagnosticsCleared,
        }

        public event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>> DiagnosticsUpdated
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

        void IDisposable.Dispose()
        {
            _eventQueueCancellation.Cancel();
        }

        private ValueTask ProcessEventsBatchAsync(ImmutableSegmentedList<(BatchOperation operation, IDiagnosticUpdateSource source, ImmutableArray<DiagnosticsUpdatedArgs> argsCollection)> batch, CancellationToken cancellationToken)
        {
            var ev = _eventMap.GetEventHandlers<EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>>(DiagnosticsUpdatedEventName);

            foreach (var (operation, source, argsCollection) in batch)
            {
                if (operation == BatchOperation.DiagnosticsUpdated)
                {
                    var updatedArgsCollection = UpdateDataMap(source, argsCollection);
                    if (updatedArgsCollection.IsEmpty)
                    {
                        // there is no change, nothing to raise events for.
                        continue;
                    }

                    ev.RaiseEvent(static (handler, arg) => handler(arg.source, arg.updatedArgsCollection), (source, updatedArgsCollection));
                }
                else if (operation == BatchOperation.DiagnosticsCleared)
                {
                    using var argsBuilder = TemporaryArray<DiagnosticsUpdatedArgs>.Empty;

                    if (!ClearDiagnosticsReportedBySource(source, ref argsBuilder.AsRef()))
                    {
                        // there is no change, nothing to raise events for.
                        continue;
                    }

                    // don't create event listener if it haven't created yet. if there is a diagnostic to remove
                    // listener should have already created since all events are done in the serialized queue
                    ev.RaiseEvent(static (handler, arg) => handler(arg.source, arg.args), (source, args: argsBuilder.ToImmutableAndClear()));
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(operation);
                }
            }

            return ValueTaskFactory.CompletedTask;
        }

        private void RaiseDiagnosticsUpdated(IDiagnosticUpdateSource source, ImmutableArray<DiagnosticsUpdatedArgs> argsCollection)
        {
            _eventQueue.AddWork((BatchOperation.DiagnosticsUpdated, source, argsCollection));
        }

        private void RaiseDiagnosticsCleared(IDiagnosticUpdateSource source)
        {
            _eventQueue.AddWork((BatchOperation.DiagnosticsCleared, source, ImmutableArray<DiagnosticsUpdatedArgs>.Empty));
        }

        private ImmutableArray<DiagnosticsUpdatedArgs> UpdateDataMap(IDiagnosticUpdateSource source, ImmutableArray<DiagnosticsUpdatedArgs> argsCollection)
        {
            // we expect source who uses this ability to have small number of diagnostics.
            lock (_gate)
            {
                var result = argsCollection.WhereAsArray(args =>
                {
                    Debug.Assert(_updateSources.Contains(source));

                    var diagnostics = args.Diagnostics;

                    // check cheap early bail out
                    if (diagnostics.Length == 0 && !_map.ContainsKey(source))
                    {
                        // no new diagnostic, and we don't have update source for it.
                        return false;
                    }

                    // 2 different workspaces (ex, PreviewWorkspaces) can return same Args.Id, we need to
                    // distinguish them. so we separate diagnostics per workspace map.
                    var workspaceMap = _map.GetOrAdd(source, _ => []);

                    if (diagnostics.Length == 0 && !workspaceMap.ContainsKey(args.Workspace))
                    {
                        // no new diagnostic, and we don't have workspace for it.
                        return false;
                    }

                    var diagnosticDataMap = workspaceMap.GetOrAdd(args.Workspace, _ => []);

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
                        var data = new Data(args, diagnostics);
                        diagnosticDataMap.Add(args.Id, data);
                    }

                    return true;
                });

                return result;
            }
        }

        private bool ClearDiagnosticsReportedBySource(IDiagnosticUpdateSource source, ref TemporaryArray<DiagnosticsUpdatedArgs> removed)
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

        private void OnDiagnosticsUpdated(object? sender, ImmutableArray<DiagnosticsUpdatedArgs> e)
        {
            AssertIfNull(e.SelectManyAsArray(e => e.Diagnostics));

            // all events are serialized by async event handler
            RaiseDiagnosticsUpdated((IDiagnosticUpdateSource)sender!, e);
        }

        private void OnCleared(object? sender, EventArgs e)
        {
            // all events are serialized by async event handler
            RaiseDiagnosticsCleared((IDiagnosticUpdateSource)sender!);
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
        private static void AssertIfNull<T>(T obj)
            where T : class
        {
            if (obj == null)
            {
                Debug.Assert(false, "who returns invalid data?");
            }
        }

        private readonly struct Data
        {
            public readonly Workspace Workspace;
            public readonly ProjectId? ProjectId;
            public readonly DocumentId? DocumentId;
            public readonly object Id;
            public readonly ImmutableArray<DiagnosticData> Diagnostics;

            public Data(UpdatedEventArgs args)
                : this(args, [])
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
