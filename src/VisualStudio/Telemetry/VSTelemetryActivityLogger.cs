// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal sealed class VSTelemetryActivityLogger : ForegroundThreadAffinitizedObject, ILogger
    {
        private static readonly HashSet<FunctionId> s_functionIds
            = new HashSet<FunctionId>()
            {
                FunctionId.NavigateTo_Search,
                FunctionId.Rename_InlineSession,
                FunctionId.CommandHandler_FindAllReference,
                FunctionId.CommandHandler_FormatCommand
            };

        private readonly IVsTelemetryService _service;
        private readonly ConcurrentDictionary<int, TelemetryActivity> _pendingActivities;

        public VSTelemetryActivityLogger(IVsTelemetryService service) : base(assertIsForeground: true)
        {
            _service = service;
            _pendingActivities = new ConcurrentDictionary<int, TelemetryActivity>(concurrencyLevel: 2, capacity: 10);

            // Fetch the session synchronously on the UI thread; if this doesn't happen before we try using this on
            // the background thread then we will experience hangs like we see in this bug:
            // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=190808
            var unused = TelemetryHelper.DefaultTelemetrySession;
        }

        public bool IsEnabled(FunctionId functionId)
        {
            return CanHandle(functionId);
        }

        public void Log(FunctionId functionId, LogMessage logMessage)
        {
            Contract.Fail("Shouldn't be called");
        }

        public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken)
        {
            var eventName = functionId.GetEventName();
            _pendingActivities[uniquePairId] = new TelemetryActivity(_service, eventName, startCodeMarker: 0, endCodeMarker: 0, codeMarkerData: null, parentCorrelationId: Guid.Empty);
        }

        public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken)
        {
            TelemetryActivity activity;
            if (!_pendingActivities.TryRemove(uniquePairId, out activity))
            {
                Contract.Requires(false, "when can this happen?");
                return;
            }

            activity.Dispose();
        }

        private static bool CanHandle(FunctionId functionId)
        {
            return s_functionIds.Contains(functionId);
        }
    }
}
