// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal class VSTelemetryLogger : ILogger
    {
        private readonly TelemetrySession _session;
        private readonly ConcurrentDictionary<int, object> _pendingScopes;

        public VSTelemetryLogger(TelemetrySession session)
        {
            _session = session;
            _pendingScopes = new ConcurrentDictionary<int, object>(concurrencyLevel: 2, capacity: 10);
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return true;
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            var kvLogMessage = logMessage as KeyValueLogMessage;
            if (kvLogMessage == null)
            {
                return;
            }

            try
            {
                // guard us from exception thrown by telemetry
                if (!kvLogMessage.ContainsProperty)
                {
                    _session.PostEvent(functionId.GetEventName());
                    return;
                }

                var telemetryEvent = CreateTelemetryEvent(functionId, kvLogMessage);
                _session.PostEvent(telemetryEvent);
            }
            catch
            {
            }
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
        {
            var kvLogMessage = logMessage as KeyValueLogMessage;
            if (kvLogMessage == null)
            {
                return;
            }

            try
            {
                // guard us from exception thrown by telemetry
                _pendingScopes[blockId] = CreateAndStartScope(kvLogMessage.Kind, functionId);
            }
            catch
            {
            }
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int blockId, int delta, CancellationToken cancellationToken)
        {
            var kvLogMessage = logMessage as KeyValueLogMessage;
            if (kvLogMessage == null)
            {
                return;
            }

            try
            {
                // guard us from exception thrown by telemetry
                var kind = kvLogMessage.Kind;
                switch (kind)
                {
                    case LogType.Trace:
                        EndScope<OperationEvent>(functionId, blockId, kvLogMessage, cancellationToken);
                        return;
                    case LogType.UserAction:
                        EndScope<UserTaskEvent>(functionId, blockId, kvLogMessage, cancellationToken);
                        return;
                    default:
                        FatalError.Report(new Exception($"unknown type: {kind}"));
                        break;
                }
            }
            catch
            {
            }
        }

        private void EndScope<T>(FunctionId functionId, int blockId, KeyValueLogMessage kvLogMessage, CancellationToken cancellationToken)
            where T : OperationEvent
        {
            if (!_pendingScopes.TryRemove(blockId, out var value))
            {
                Debug.Assert(false, "when can this happen?");
                return;
            }

            var operation = (TelemetryScope<T>)value;

            AppendProperties(operation.EndEvent, functionId, kvLogMessage);
            operation.End(cancellationToken.IsCancellationRequested ? TelemetryResult.UserCancel : TelemetryResult.Success);
        }

        private object CreateAndStartScope(LogType kind, FunctionId functionId)
        {
            // use object since TelemetryScope<UserTask> and 
            // TelemetryScope<Operation> can't be shared
            var eventName = functionId.GetEventName();

            return kind switch
            {
                LogType.Trace => _session.StartOperation(eventName),
                LogType.UserAction => _session.StartUserTask(eventName),
                _ => (object)FatalError.Report(new Exception($"unknown type: {kind}")),
            };
        }

        private TelemetryEvent CreateTelemetryEvent(FunctionId functionId, KeyValueLogMessage logMessage)
        {
            var eventName = functionId.GetEventName();
            return AppendProperties(new TelemetryEvent(eventName), functionId, logMessage);
        }

        private static T AppendProperties<T>(T @event, FunctionId functionId, KeyValueLogMessage logMessage)
            where T : TelemetryEvent
        {
            if (!logMessage.ContainsProperty)
            {
                return @event;
            }

            foreach (var kv in logMessage.Properties)
            {
                var propertyName = functionId.GetPropertyName(kv.Key);

                // call SetProperty. VS telemetry will take care of finding correct
                // API based on given object type for us.
                // 
                // numeric data will show up in ES with measurement prefix.
                @event.Properties.Add(propertyName, kv.Value);
            }

            return @event;
        }
    }
}
