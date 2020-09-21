// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed class VisualStudioWorkspaceTelemetryService : AbstractWorkspaceTelemetryService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IGlobalOptionService _optionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceTelemetryService(
            VisualStudioWorkspace workspace,
            IGlobalOptionService optionsService)
        {
            _workspace = workspace;
            _optionsService = optionsService;
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession)
            => AggregateLogger.Create(
                CodeMarkerLogger.Instance,
                new EtwLogger(_optionsService),
                new VSTelemetryLogger(telemetrySession),
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

                // initialize session in the remote service
                await client.RunRemoteAsync(
                    WellKnownServiceHubService.RemoteHost,
                    nameof(IRemoteHostService.InitializeTelemetrySession),
                    solution: null,
                    new object?[] { Process.GetCurrentProcess().Id, settings },
                    callbackTarget: null,
                    CancellationToken.None).ConfigureAwait(false);
            });
        }
    }
}
