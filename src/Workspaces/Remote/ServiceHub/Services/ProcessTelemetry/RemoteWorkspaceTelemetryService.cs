// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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
    /// Composed once, at startup, mirroring the VS host. Toggled through its predicate by
    /// <c>IRemoteProcessTelemetryService.EnableLoggingAsync</c>.
    /// </summary>
    private EtwLogger? _etwLogger;

    protected override ImmutableArray<IEventSink> CreateEventSinks(TelemetrySession telemetrySession, bool logDelta)
    {
        _etwLogger = new EtwLogger(EtwLogger.DisabledPredicate);

        return
        [
            _etwLogger,
            TelemetryLogger.Create(telemetrySession, logDelta),
        ];
    }

    /// <inheritdoc cref="VisualStudioWorkspaceTelemetryService.UpdateEtwEnablement"/>
    internal void UpdateEtwEnablement(bool enabled, Func<FunctionId, bool> isEnabled)
        => _etwLogger?.UpdatePredicate(enabled ? isEnabled : EtwLogger.DisabledPredicate);
}
