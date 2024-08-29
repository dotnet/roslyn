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
internal sealed class RemoteWorkspaceTelemetryService() : AbstractVisualStudioWorkspaceTelemetryService
{
    protected override ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta)
        => AggregateLogger.Create(
            TelemetryLogger.Create(telemetrySession, logDelta),
            Logger.GetLogger());
}
