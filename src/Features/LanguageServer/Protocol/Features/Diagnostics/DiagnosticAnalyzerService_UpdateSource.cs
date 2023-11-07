// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class DiagnosticAnalyzerService : IDiagnosticUpdateSource
    {
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

        public event EventHandler DiagnosticsCleared
        {
            add
            {
                // don't do anything. this update source doesn't use cleared event
            }

            remove
            {
                // don't do anything. this update source doesn't use cleared event
            }
        }

        internal void RaiseDiagnosticsUpdated(ImmutableArray<DiagnosticsUpdatedArgs> args)
        {
            if (args.IsEmpty)
                return;

            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => ev.RaiseEvent(static (handler, arg) => handler(arg.self, arg.args), (self: this, args)), CancellationToken.None);
            }
        }

        internal void RaiseBulkDiagnosticsUpdated(Action<Action<ImmutableArray<DiagnosticsUpdatedArgs>>> eventAction)
        {
            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                // we do this bulk update to reduce number of tasks (with captured data) enqueued.
                // we saw some "out of memory" due to us having long list of pending tasks in memory. 
                // this is to reduce for such case to happen.
                void raiseEvents(ImmutableArray<DiagnosticsUpdatedArgs> args)
                {
                    if (args.IsEmpty)
                        return;

                    ev.RaiseEvent(
                        static (handler, arg) => handler(arg.self, arg.args),
                        (self: this, args));
                }

                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => eventAction(raiseEvents), CancellationToken.None);
            }
        }

        internal void RaiseBulkDiagnosticsUpdated(Func<Action<ImmutableArray<DiagnosticsUpdatedArgs>>, Task> eventActionAsync)
        {
            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                // we do this bulk update to reduce number of tasks (with captured data) enqueued.
                // we saw some "out of memory" due to us having long list of pending tasks in memory. 
                // this is to reduce for such case to happen.
                void raiseEvents(ImmutableArray<DiagnosticsUpdatedArgs> args)
                {
                    ev.RaiseEvent(
                        static (handler, arg) =>
                        {
                            if (!arg.args.IsEmpty)
                                handler(arg.self, arg.args);
                        },
                        (self: this, args));
                }

                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => eventActionAsync(raiseEvents), CancellationToken.None);
            }
        }

        bool IDiagnosticUpdateSource.SupportGetDiagnostics => true;

        ValueTask<ImmutableArray<DiagnosticData>> IDiagnosticUpdateSource.GetDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return new ValueTask<ImmutableArray<DiagnosticData>>(GetSpecificCachedDiagnosticsAsync(workspace, id, includeSuppressedDiagnostics, includeNonLocalDocumentDiagnostics: true, cancellationToken));
            }

            return new ValueTask<ImmutableArray<DiagnosticData>>(GetCachedDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, includeLocalDocumentDiagnostics: true, includeNonLocalDocumentDiagnostics: true, cancellationToken));
        }
    }
}
