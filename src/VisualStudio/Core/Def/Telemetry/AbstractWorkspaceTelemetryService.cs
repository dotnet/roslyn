// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

internal abstract class AbstractWorkspaceTelemetryService : IWorkspaceTelemetryService, IDisposable
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

    [MemberNotNullWhen(true, nameof(CurrentSession))]
    public bool HasActiveSession
        => CurrentSession != null && CurrentSession.IsOptedIn;

    public bool IsUserMicrosoftInternal
        => HasActiveSession && CurrentSession.IsUserMicrosoftInternal;

    public string? SerializeCurrentSessionSettings()
        => CurrentSession?.SerializeSettings();

    public void RegisterUnexpectedExceptionLogger(TraceSource logger)
        => FaultReporter.RegisterLogger(logger);

    public void UnregisterUnexpectedExceptionLogger(TraceSource logger)
        => FaultReporter.UnregisterLogger(logger);

    public void Dispose()
    {
        // Ensure any aggregate telemetry is flushed when the catalog is destroyed.
        // It is fine for this to be called multiple times - if telemetry has already been flushed this will no-op.
        TelemetryLogging.Flush();
    }
}
