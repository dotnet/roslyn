// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal class VSTelemetryLogger : ILogger
    {
        private const string Start = "Start";
        private const string End = "End";
        private const string BlockId = "BlockId";
        private const string Duration = "Duration";
        private const string CancellationRequested = "CancellationRequested";

        private readonly IVsTelemetryService _service;
        private readonly IVsTelemetrySession _session;

        public VSTelemetryLogger(IVsTelemetryService service)
        {
            _service = service;
            _session = service.GetDefaultSession();
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
                    _session.PostSimpleEvent(functionId.GetEventName());
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
                telemetryEvent.SetIntProperty(durationName, delta);

                var cancellationName = functionId.GetPropertyName(CancellationRequested);
                telemetryEvent.SetBoolProperty(cancellationName, cancellationToken.IsCancellationRequested);

                _session.PostEvent(telemetryEvent);
            }
            catch
            {
            }
        }

        private IVsTelemetryEvent CreateTelemetryEvent(FunctionId functionId, string eventKey = null, KeyValueLogMessage logMessage = null)
        {
            var eventName = functionId.GetEventName(eventKey);
            var telemetryEvent = _service.CreateEvent(eventName);

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
                telemetryEvent.SetProperty(propertyName, kv.Value);
            }

            return telemetryEvent;
        }

        private void SetBlockId(IVsTelemetryEvent telemetryEvent, FunctionId functionId, int blockId)
        {
            var blockIdName = functionId.GetPropertyName(BlockId);
            telemetryEvent.SetIntProperty(blockIdName, blockId);
        }
    }
}
