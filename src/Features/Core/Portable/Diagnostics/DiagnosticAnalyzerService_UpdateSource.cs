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

        // use eventMap and taskQueue to serialize events
        private readonly EventMap _eventMap;
        private readonly SimpleTaskQueue _eventQueue;

        private DiagnosticAnalyzerService(IDiagnosticUpdateSourceRegistrationService registrationService) : this()
        {
            _eventMap = new EventMap();

            // use diagnostic event task scheduler so that we never flood async events queue with million of events.
            // queue itself can handle huge number of events but we are seeing OOM due to captured data in pending events.
            _eventQueue = new SimpleTaskQueue(new DiagnosticEventTaskScheduler(blockingUpperBound: 100));

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

        internal void RaiseDiagnosticsUpdated(object sender, DiagnosticsUpdatedArgs args)
        {
            // raise serialized events
            var ev = _eventMap.GetEventHandlers<EventHandler<DiagnosticsUpdatedArgs>>(DiagnosticsUpdatedEventName);
            if (ev.HasHandlers)
            {
                var asyncToken = Listener.BeginAsyncOperation(nameof(RaiseDiagnosticsUpdated));
                _eventQueue.ScheduleTask(() =>
                {
                    ev.RaiseEvent(handler => handler(sender, args));
                }).CompletesAsyncOperation(asyncToken);
            }
        }

        bool IDiagnosticUpdateSource.SupportGetDiagnostics { get { return true; } }

        ImmutableArray<DiagnosticData> IDiagnosticUpdateSource.GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return GetSpecificCachedDiagnosticsAsync(workspace, id, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult(cancellationToken);
            }

            return GetCachedDiagnosticsAsync(workspace, projectId, documentId, includeSuppressedDiagnostics, cancellationToken).WaitAndGetResult(cancellationToken);
        }
    }
}
