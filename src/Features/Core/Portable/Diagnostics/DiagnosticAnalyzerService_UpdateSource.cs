// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class DiagnosticAnalyzerService : IDiagnosticUpdateSource
    {
        private const string DiagnosticsUpdatedEventName = "DiagnosticsUpdated";

        private static readonly DiagnosticEventTaskScheduler s_eventScheduler = new DiagnosticEventTaskScheduler(blockingUpperBound: 100);

        // use eventMap and taskQueue to serialize events
        private readonly EventMap _eventMap;
        private readonly SimpleTaskQueue _eventQueue;

        private DiagnosticAnalyzerService(IDiagnosticUpdateSourceRegistrationService registrationService) : this()
        {
            _eventMap = new EventMap();

            // use diagnostic event task scheduler so that we never flood async events queue with million of events.
            // queue itself can handle huge number of events but we are seeing OOM due to captured data in pending events.
            _eventQueue = new SimpleTaskQueue(s_eventScheduler);

            registrationService.Register(this);
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
                var asyncToken = Listener.BeginAsyncOperation(nameof(RaiseDiagnosticsUpdated));
                _eventQueue.ScheduleTask(() => ev.RaiseEvent(handler => handler(this, args))).CompletesAsyncOperation(asyncToken);
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

                var asyncToken = Listener.BeginAsyncOperation(nameof(RaiseDiagnosticsUpdated));
                _eventQueue.ScheduleTask(() => eventAction(raiseEvents)).CompletesAsyncOperation(asyncToken);
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

                var asyncToken = Listener.BeginAsyncOperation(nameof(RaiseDiagnosticsUpdated));
                _eventQueue.ScheduleTask(() => eventActionAsync(raiseEvents)).CompletesAsyncOperation(asyncToken);
            }
        }

        bool IDiagnosticUpdateSource.SupportGetDiagnostics => true;

        ImmutableArray<DiagnosticData> IDiagnosticUpdateSource.GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return GetSpecificCachedDiagnosticsAsync(workspace, id, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
            }

            return GetCachedDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult_CanCallOnBackground(cancellationToken);
        }
    }
}
