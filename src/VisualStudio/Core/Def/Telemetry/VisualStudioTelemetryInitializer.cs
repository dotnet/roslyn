// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class VisualStudioTelemetryInitializer : IEventListener<object>
    {
        private readonly VisualStudioWorkspace _visualStudioWorkspace;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _asynchronousOperationListener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTelemetryInitializer(
            VisualStudioWorkspace visualStudioWorkspace,
            IThreadingContext threadingContext,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _visualStudioWorkspace = visualStudioWorkspace;
            _threadingContext = threadingContext;
            _asynchronousOperationListener = listenerProvider.GetListener(FeatureAttribute.Telemetry);
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            if (workspace is VisualStudioWorkspace)
            {
                _ = StartListeningAsync();
            }
        }

        private async Task StartListeningAsync()
        {
            // Have to catch all exceptions coming through here as this is called from a
            // fire-and-forget method and we want to make sure nothing leaks out.
            try
            {
                using var token = _asynchronousOperationListener.BeginAsyncOperation(nameof(InitializeTelemetryAsync));
                await InitializeTelemetryAsync(_threadingContext.DisposalToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Cancellation is normal (during VS closing).  Just ignore.
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                // Otherwise report a watson for any other exception.  Don't bring down VS.  This is
                // a BG service we don't want impacting the user experience.
            }
        }

        private async Task InitializeTelemetryAsync(CancellationToken cancellationToken)
        {
            // Fetch the session synchronously on the UI thread; if this doesn't happen before we try using this on
            // the background thread then we will experience hangs like we see in this bug:
            // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/_workitems?_a=edit&id=190808 or
            // https://devdiv.visualstudio.com/DevDiv/_workitems?id=296981&_a=edit
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var session = TelemetryService.DefaultSession;

            // Now that the telemetry session has been initialized, the rest of the work can happen off of the UI thread.
            await TaskScheduler.Default;

            var telemetryService = (VisualStudioWorkspaceTelemetryService)_visualStudioWorkspace.Services.GetRequiredService<IWorkspaceTelemetryService>();
            await telemetryService.InitializeTelemetrySessionAsync(session, cancellationToken).ConfigureAwait(false);

            Logger.Log(FunctionId.Run_Environment,
                KeyValueLogMessage.Create(m => m["Version"] = FileVersionInfo.GetVersionInfo(typeof(VisualStudioWorkspace).Assembly.Location).FileVersion));
        }
    }
}
