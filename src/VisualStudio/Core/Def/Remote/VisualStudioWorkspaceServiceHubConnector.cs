// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    /// <summary>
    /// Connects <see cref="VisualStudioWorkspace"/> to the ServiceHub services.
    /// Launches ServiceHub if it is not running yet and starts services that push information from <see cref="VisualStudioWorkspace"/> to the ServiceHub process.
    /// </summary>
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed class VisualStudioWorkspaceServiceHubConnector : IEventListener<object>, IEventListenerStoppable
    {
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;

        private GlobalNotificationRemoteDeliveryService? _globalNotificationDelivery;
        private Task<RemoteHostClient?>? _remoteClientInitializationTask;
        private SolutionChecksumUpdater? _checksumUpdater;
#pragma warning disable IDE0044 // Add readonly modifier
        private CancellationTokenSource _disposalCancellationSource;
#pragma warning restore IDE0044 // Add readonly modifier

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceServiceHubConnector(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            IThreadingContext threadingContext)
        {
            _listenerProvider = listenerProvider;
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;
            _disposalCancellationSource = new CancellationTokenSource();
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            if (workspace is not VisualStudioWorkspace || IVsShellExtensions.IsInCommandLineMode(_threadingContext.JoinableTaskFactory))
            {
                return;
            }

            // only push solution snapshot from primary (VS) workspace:
            _checksumUpdater = new SolutionChecksumUpdater(workspace, _globalOptions, _listenerProvider, _disposalCancellationSource.Token);

            _globalNotificationDelivery = new GlobalNotificationRemoteDeliveryService(workspace.Services, _disposalCancellationSource.Token);

            // start launching remote process, so that the first service that needs it doesn't need to wait for it:
            var service = workspace.Services.GetRequiredService<IRemoteHostClientProvider>();
            _remoteClientInitializationTask = service.TryGetRemoteHostClientAsync(_disposalCancellationSource.Token);
        }

        public void StopListening(Workspace workspace)
        {
            if (!(workspace is VisualStudioWorkspace) || IVsShellExtensions.IsInCommandLineMode(_threadingContext.JoinableTaskFactory))
            {
                return;
            }

            _disposalCancellationSource.Cancel();
            _disposalCancellationSource.Dispose();

            _checksumUpdater?.Shutdown();
            _globalNotificationDelivery?.Dispose();

            // dispose remote client if its initialization has completed:
            _remoteClientInitializationTask?.ContinueWith(
                previousTask => previousTask.Result?.Dispose(),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
        }
    }
}
