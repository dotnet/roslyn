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

        internal void RaiseDiagnosticsUpdated(DiagnosticsUpdatedArgs args)
        {
            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => ev.RaiseEvent(handler => handler(this, args)), CancellationToken.None);
            }
        }

        internal void RaiseBulkDiagnosticsUpdated(Action<Action<DiagnosticsUpdatedArgs>> eventAction)
        {
            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                // we do this bulk update to reduce number of tasks (with captured data) enqueued.
                // we saw some "out of memory" due to us having long list of pending tasks in memory. 
                // this is to reduce for such case to happen.
                void raiseEvents(DiagnosticsUpdatedArgs args) => ev.RaiseEvent(handler => handler(this, args));

                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => eventAction(raiseEvents), CancellationToken.None);
            }
        }

        internal void RaiseBulkDiagnosticsUpdated(Func<Action<DiagnosticsUpdatedArgs>, Task> eventActionAsync)
        {
            // all diagnostics events are serialized.
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                // we do this bulk update to reduce number of tasks (with captured data) enqueued.
                // we saw some "out of memory" due to us having long list of pending tasks in memory. 
                // this is to reduce for such case to happen.
                void raiseEvents(DiagnosticsUpdatedArgs args) => ev.RaiseEvent(handler => handler(this, args));

                _eventQueue.ScheduleTask(nameof(RaiseDiagnosticsUpdated), () => eventActionAsync(raiseEvents), CancellationToken.None);
            }
        }

        bool IDiagnosticUpdateSource.SupportGetDiagnostics => true;

        ValueTask<ImmutableArray<DiagnosticData>> IDiagnosticUpdateSource.GetDiagnosticsAsync(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return new ValueTask<ImmutableArray<DiagnosticData>>(GetSpecificCachedDiagnosticsAsync(workspace, id, includeSuppressedDiagnostics, cancellationToken));
            }

            return new ValueTask<ImmutableArray<DiagnosticData>>(GetCachedDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken));
        }
    }
}
