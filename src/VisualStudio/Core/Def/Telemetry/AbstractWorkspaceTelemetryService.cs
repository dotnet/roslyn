// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

internal abstract class AbstractWorkspaceTelemetryService : IWorkspaceTelemetryService, IDisposable
{
    public TelemetrySession? CurrentSession { get; private set; }

    protected abstract ImmutableArray<IEventSink> CreateEventSinks(TelemetrySession telemetrySession, bool logDelta);

    public void InitializeTelemetrySession(TelemetrySession telemetrySession, bool logDelta)
    {
        Contract.ThrowIfFalse(CurrentSession is null);

        RoslynTelemetry.SetEventSinks(CreateEventSinks(telemetrySession, logDelta));
        RoslynTelemetry.SetMetricSink(new VSMetricSink(telemetrySession));
        FaultReporter.RegisterTelemetrySesssion(telemetrySession);

        CurrentSession = telemetrySession;

        StartPeriodicFlush();
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
        RoslynTelemetry.Flush();
    }

    /// <summary>
    /// Posts whatever has accumulated every 30 minutes. Shutdown paths flush explicitly as well, because
    /// a host can exit too abruptly for a timer-based flush to run.
    /// </summary>
    private static void StartPeriodicFlush()
        => _ = PostCollectedTelemetryAsync();

    private static async Task PostCollectedTelemetryAsync()
    {
        await Task.Delay(TimeSpan.FromMinutes(30)).ConfigureAwait(false);

        RoslynTelemetry.Flush();

        // Create a fire and forget task to handle the next collection. This doesn't use
        // IAsynchronousOperationListener to track this work as no-one needs to ensure this is sent, and
        // creating a new item of work upon previous completion doesn't fit well in that model.
        _ = PostCollectedTelemetryAsync().ReportNonFatalErrorAsync();
    }
}
