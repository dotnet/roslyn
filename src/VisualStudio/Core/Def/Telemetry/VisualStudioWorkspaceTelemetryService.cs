// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed class VisualStudioWorkspaceTelemetryService : AbstractWorkspaceTelemetryService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListenerProvider _asyncListenerProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceTelemetryService(
            VisualStudioWorkspace workspace,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asyncListenerProvider)
        {
            _workspace = workspace;
            _globalOptions = globalOptions;
            _asyncListenerProvider = asyncListenerProvider;
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession, bool logDelta)
            => AggregateLogger.Create(
                CodeMarkerLogger.Instance,
                new EtwLogger(FunctionIdOptions.CreateFunctionIsEnabledPredicate(_globalOptions)),
                TelemetryLogger.Create(telemetrySession, logDelta, _asyncListenerProvider),
                new FileLogger(_globalOptions),
                Logger.GetLogger());

        protected override void TelemetrySessionInitialized()
        {
            _ = Task.Run(async () =>
            {
                var client = await RemoteHostClient.TryGetClientAsync(_workspace, CancellationToken.None).ConfigureAwait(false);
                if (client == null)
                {
                    return;
                }

                var settings = SerializeCurrentSessionSettings();
                Contract.ThrowIfNull(settings);

                // Only log "delta" property for block end events if feature flag is enabled.
                var logDelta = _globalOptions.GetOption(DiagnosticOptionsStorage.LogTelemetryForBackgroundAnalyzerExecution);

                // initialize session in the remote service
                _ = await client.TryInvokeAsync<IRemoteProcessTelemetryService>(
                    (service, cancellationToken) => service.InitializeTelemetrySessionAsync(Process.GetCurrentProcess().Id, settings, logDelta, cancellationToken),
                    CancellationToken.None).ConfigureAwait(false);
            });
        }
    }
}
