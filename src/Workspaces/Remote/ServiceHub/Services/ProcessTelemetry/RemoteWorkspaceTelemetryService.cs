// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

[ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteWorkspaceTelemetryService() : AbstractWorkspaceTelemetryService
{
    /// <summary>
    /// Opt-in diagnostic sinks. Composed once, at startup, and thereafter toggled through their
    /// predicates by <c>IRemoteProcessTelemetryService.EnableLoggingAsync</c>.
    /// </summary>
    private EtwLogger? _etwLogger;
    private TraceLogger? _traceLogger;

    protected override IEventSink CreateLogger(TelemetrySession telemetrySession, bool logDelta)
    {
        _etwLogger = new EtwLogger(EtwLogger.DisabledPredicate);
        _traceLogger = new TraceLogger(EtwLogger.DisabledPredicate);

        return AggregateEventSink.Create(
            _etwLogger,
            _traceLogger,
            TelemetryLogger.Create(telemetrySession, logDelta),
            RoslynTelemetry.GetEventSink());
    }

    /// <inheritdoc cref="VisualStudioWorkspaceTelemetryService.UpdateDiagnosticSinkEnablement"/>
    internal void UpdateDiagnosticSinkEnablement(bool etwEnabled, bool traceEnabled, Func<FunctionId, bool> isEnabled)
    {
        _etwLogger?.UpdatePredicate(etwEnabled ? isEnabled : EtwLogger.DisabledPredicate);
        _traceLogger?.UpdatePredicate(traceEnabled ? isEnabled : EtwLogger.DisabledPredicate);
    }
}
