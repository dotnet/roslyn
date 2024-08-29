// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry;

[ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class VisualStudioWorkspaceTelemetryService(
    IThreadingContext threadingContext,
    VisualStudioWorkspace workspace,
    IGlobalOptionService globalOptions) : AbstractWorkspaceTelemetryService
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly VisualStudioWorkspace _workspace = workspace;
    private readonly IGlobalOptionService _globalOptions = globalOptions;

    protected override ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta)
        => AggregateLogger.Create(
            CodeMarkerLogger.Instance,
            new EtwLogger(FunctionIdOptions.CreateFunctionIsEnabledPredicate(_globalOptions)),
            TelemetryLogger.Create(telemetrySession, logDelta),
            new FileLogger(_globalOptions),
            Logger.GetLogger());

    protected override void TelemetrySessionInitialized()
    {
        var cancellationToken = _threadingContext.DisposalToken;
        _ = Task.Run(async () =>
        {

            // Wait until the remote host was created by some other party (we don't want to cause it to happen ourselves
            // in the call to RemoteHostClient below).
            await RemoteHostClient.WaitForFirstConnection(_workspace, cancellationToken).ConfigureAwait(false);

            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return;

            var settings = SerializeCurrentSessionSettings();
            Contract.ThrowIfNull(settings);

            // Only log "delta" property for block end events if feature flag is enabled.
            var logDelta = _globalOptions.GetOption(DiagnosticOptionsStorage.LogTelemetryForBackgroundAnalyzerExecution);

            // initialize session in the remote service
            _ = await client.TryInvokeAsync<IRemoteProcessTelemetryService>(
                (service, cancellationToken) => service.InitializeTelemetrySessionAsync(Process.GetCurrentProcess().Id, settings, logDelta, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
