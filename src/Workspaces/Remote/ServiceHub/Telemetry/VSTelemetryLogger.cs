// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Telemetry
{
    internal class VSTelemetryLogger : ILogger
    {
        /// <summary>
        /// Telemetry session. can be null if it is not available in current context
        /// such as in unit test
        /// </summary>
        private static TelemetrySession s_sessionOpt;

        private const string Start = "Start";
        private const string End = "End";
        private const string BlockId = "BlockId";
        private const string Duration = "Duration";
        private const string CancellationRequested = "CancellationRequested";

        private readonly TelemetrySession _session;

        public VSTelemetryLogger(TelemetrySession session)
        {
            Contract.ThrowIfNull(session);
            _session = session;
        }

        public static TelemetrySession SessionOpt => s_sessionOpt;

        public static void SetTelemetrySession(TelemetrySession session)
        {
            // it can be only set once
            Contract.ThrowIfFalse(s_sessionOpt == null);
            s_sessionOpt = session;
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

                var telemetryEvent = CreateTelemetryEvent(functionId, logMessage: kvLogMessage);
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
                var telemetryEvent = CreateTelemetryEvent(functionId, Start, kvLogMessage);
                SetBlockId(telemetryEvent, functionId, blockId);

                _session.PostEvent(telemetryEvent);
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
                var telemetryEvent = CreateTelemetryEvent(functionId, End);
                SetBlockId(telemetryEvent, functionId, blockId);

                var durationName = functionId.GetPropertyName(Duration);
                telemetryEvent.Properties.Add(durationName, delta);

                var cancellationName = functionId.GetPropertyName(CancellationRequested);
                telemetryEvent.Properties.Add(cancellationName, cancellationToken.IsCancellationRequested);

                _session.PostEvent(telemetryEvent);
            }
            catch
            {
            }
        }

        private TelemetryEvent CreateTelemetryEvent(FunctionId functionId, string eventKey = null, KeyValueLogMessage logMessage = null)
        {
            var eventName = functionId.GetEventName(eventKey);
            var telemetryEvent = new TelemetryEvent(eventName);

            if (logMessage == null || !logMessage.ContainsProperty)
            {
                return telemetryEvent;
            }

            foreach (var kv in logMessage.Properties)
            {
                var propertyName = functionId.GetPropertyName(kv.Key);

                // call SetProperty. VS telemetry will take care of finding correct
                // API based on given object type for us.
                // 
                // numeric data will show up in ES with measurement prefix.
                telemetryEvent.Properties.Add(propertyName, kv.Value);
            }

            return telemetryEvent;
        }

        private void SetBlockId(TelemetryEvent telemetryEvent, FunctionId functionId, int blockId)
        {
            var blockIdName = functionId.GetPropertyName(BlockId);
            telemetryEvent.Properties.Add(blockIdName, blockId);
        }
    }
}
