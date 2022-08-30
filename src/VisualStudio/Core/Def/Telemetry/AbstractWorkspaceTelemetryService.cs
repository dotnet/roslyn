// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal abstract class AbstractWorkspaceTelemetryService : IWorkspaceTelemetryService
    {
        public TelemetrySession? CurrentSession { get; private set; }

        protected abstract ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta);

        public void InitializeTelemetrySession(TelemetrySession telemetrySession, bool logDelta)
        {
            Contract.ThrowIfFalse(CurrentSession is null);

            Logger.SetLogger(CreateLogger(telemetrySession, logDelta));
            FaultReporter.RegisterTelemetrySesssion(telemetrySession);

            CurrentSession = telemetrySession;

            TelemetrySessionInitialized();
        }

        protected virtual void TelemetrySessionInitialized()
        {
        }

        public bool HasActiveSession
            => CurrentSession != null && CurrentSession.IsOptedIn;

        public string? SerializeCurrentSessionSettings()
            => CurrentSession?.SerializeSettings();

        public void RegisterUnexpectedExceptionLogger(TraceSource logger)
            => FaultReporter.RegisterLogger(logger);

        public void UnregisterUnexpectedExceptionLogger(TraceSource logger)
            => FaultReporter.UnregisterLogger(logger);
    }
}
