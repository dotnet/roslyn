// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
            => _session.IsOptedIn;

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            if (IgnoreMessage(logMessage))
            {
                return;
            }

            try
            {
                if (logMessage is KeyValueLogMessage { ContainsProperty: false })
                {
                    // guard us from exception thrown by telemetry
                    _session.PostEvent(functionId.GetEventName());
                    return;
                }

                var telemetryEvent = CreateTelemetryEvent(functionId, logMessage);
                _session.PostEvent(telemetryEvent);
            }
            catch
            {
            }
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int blockId, CancellationToken cancellationToken)
        {
            if (IgnoreMessage(logMessage))
            {
                return;
            }

            try
            {
                // guard us from exception thrown by telemetry
                var kind = GetKind(logMessage);

                _pendingScopes[blockId] = CreateAndStartScope(kind, functionId);
            }
            catch
            {
            }
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int blockId, int delta, CancellationToken cancellationToken)
        {
            if (IgnoreMessage(logMessage))
            {
                return;
            }

            try
            {
                // guard us from exception thrown by telemetry
                var kind = GetKind(logMessage);

                switch (kind)
                {
                    case LogType.Trace:
                        EndScope<OperationEvent>(functionId, blockId, logMessage, cancellationToken);
                        return;
                    case LogType.UserAction:
                        EndScope<UserTaskEvent>(functionId, blockId, logMessage, cancellationToken);
                        return;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(kind);
                }
            }
            catch
            {
            }
        }

        private static bool IgnoreMessage(LogMessage logMessage)
            => logMessage.LogLevel < LogLevel.Information;

        private static LogType GetKind(LogMessage logMessage)
            => logMessage is KeyValueLogMessage kvLogMessage
                                ? kvLogMessage.Kind
                                : logMessage.LogLevel switch
                                {
                                    >= LogLevel.Information => LogType.UserAction,
                                    _ => LogType.Trace
                                };

        private void EndScope<T>(FunctionId functionId, int blockId, LogMessage logMessage, CancellationToken cancellationToken)
            where T : OperationEvent
        {
            if (!_pendingScopes.TryRemove(blockId, out var value))
            {
                Debug.Assert(false, "when can this happen?");
                return;
            }

            var operation = (TelemetryScope<T>)value;

            UpdateEvent(operation.EndEvent, functionId, logMessage);
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
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        private static TelemetryEvent CreateTelemetryEvent(FunctionId functionId, LogMessage logMessage)
        {
            var eventName = functionId.GetEventName();
            var telemetryEvent = new TelemetryEvent(eventName);

            return UpdateEvent(telemetryEvent, functionId, logMessage);
        }

        private static TelemetryEvent UpdateEvent(TelemetryEvent telemetryEvent, FunctionId functionId, LogMessage logMessage)
        {
            if (logMessage is KeyValueLogMessage kvLogMessage)
            {
                AppendProperties(telemetryEvent, functionId, kvLogMessage);
            }
            else
            {
                var message = logMessage.GetMessage();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    var propertyName = functionId.GetPropertyName("Message");
                    telemetryEvent.Properties.Add(propertyName, message);
                }
            }

            return telemetryEvent;
        }

        private static void AppendProperties(TelemetryEvent telemetryEvent, FunctionId functionId, KeyValueLogMessage logMessage)
        {
            foreach (var (key, value) in logMessage.Properties)
            {
                // call SetProperty. VS telemetry will take care of finding correct
                // API based on given object type for us.
                // 
                // numeric data will show up in ES with measurement prefix.

                telemetryEvent.Properties.Add(functionId.GetPropertyName(key), value switch
                {
                    PiiValue pii => new TelemetryPiiProperty(pii.Value),
                    IEnumerable<object> items => new TelemetryComplexProperty(items.Select(item => (item is PiiValue pii) ? pii.Value : item)),
                    _ => value
                });
            }
        }
    }
}
