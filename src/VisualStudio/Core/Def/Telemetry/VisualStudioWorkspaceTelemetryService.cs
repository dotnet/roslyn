// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectTelemetry;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IWorkspaceTelemetryService)), Shared]
    internal sealed partial class VisualStudioWorkspaceTelemetryService : AbstractWorkspaceTelemetryService
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IGlobalOptionService _optionsService;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _asynchronousOperationListener;

        /// <summary>
        /// Queue where we enqueue the information we get from OOP to process in batch in the future.
        /// </summary>
        private readonly AsyncBatchingWorkQueue<ProjectTelemetryData> _workQueue;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceTelemetryService(
            VisualStudioWorkspace workspace,
            IGlobalOptionService optionsService,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _workspace = workspace;
            _optionsService = optionsService;
            _threadingContext = threadingContext;
            _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.Telemetry);
            InitializeTelemetrySession(TelemetryService.DefaultSession);

            _workQueue = new AsyncBatchingWorkQueue<ProjectTelemetryData>(
                TimeSpan.FromSeconds(1),
                NotifyTelemetryServiceAsync,
                threadingContext.DisposalToken);
        }

        protected override ILogger CreateLogger(TelemetrySession telemetrySession)
            => AggregateLogger.Create(
                CodeMarkerLogger.Instance,
                new EtwLogger(_optionsService),
                new VSTelemetryLogger(telemetrySession),
                new FileLogger(_optionsService),
                Logger.GetLogger());

        /// <summary>
        /// Once the telemetry session and service has been created, pass the session
        /// to the OOP so we can create the telemetry session there.
        /// </summary>
        protected override void TelemetrySessionInitialized()
        {
            Logger.Log(FunctionId.Run_Environment,
                KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));

            var asyncToken = _asynchronousOperationListener.BeginAsyncOperation(nameof(TelemetrySessionInitialized));
            _ = Task.Run(() => InitializeRemoteTelemetryAsync(_threadingContext.DisposalToken), _threadingContext.DisposalToken).CompletesAsyncOperation(asyncToken);
        }

        private async Task InitializeRemoteTelemetryAsync(CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(_workspace, cancellationToken).ConfigureAwait(false);
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
                cancellationToken).ConfigureAwait(false);
        }
    }
}
