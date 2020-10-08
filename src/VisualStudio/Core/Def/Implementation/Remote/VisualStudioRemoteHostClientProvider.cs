// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed class VisualStudioRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), WorkspaceKind.Host), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly IAsyncServiceProvider _vsServiceProvider;
            private readonly AsynchronousOperationListenerProvider _listenerProvider;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(SVsServiceProvider vsServiceProvider, AsynchronousOperationListenerProvider listenerProvider)
            {
                _vsServiceProvider = (IAsyncServiceProvider)vsServiceProvider;
                _listenerProvider = listenerProvider;
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                if (!RemoteHostOptions.IsUsingServiceHubOutOfProcess(workspaceServices)
                    || workspaceServices.Workspace is not VisualStudioWorkspace)
                {
                    // Run code in the current process
                    return new DefaultRemoteHostClientProvider();
                }

                return new VisualStudioRemoteHostClientProvider(workspaceServices, _vsServiceProvider, _listenerProvider);
            }
        }

        private readonly HostWorkspaceServices _services;
        private readonly AsyncLazy<RemoteHostClient?> _lazyClient;
        private readonly IAsyncServiceProvider _vsServiceProvider;
        private readonly AsynchronousOperationListenerProvider _listenerProvider;

        private VisualStudioRemoteHostClientProvider(HostWorkspaceServices services, IAsyncServiceProvider vsServiceProvider, AsynchronousOperationListenerProvider listenerProvider)
        {
            _services = services;
            _vsServiceProvider = vsServiceProvider;
            _listenerProvider = listenerProvider;
            _lazyClient = new AsyncLazy<RemoteHostClient?>(CreateHostClientAsync, cacheResult: true);
        }

        private async Task<RemoteHostClient?> CreateHostClientAsync(CancellationToken cancellationToken)
        {
            try
            {
                var brokeredServiceContainer = await _vsServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>().ConfigureAwait(false);
                var serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();

                var client = await ServiceHubRemoteHostClient.CreateAsync(_services, _listenerProvider, serviceBroker, cancellationToken).ConfigureAwait(false);

                // proffer in-proc brokered services:
                _ = brokeredServiceContainer.Proffer(SolutionAssetProvider.ServiceDescriptor, (_, _, _, _) => new ValueTask<object?>(new SolutionAssetProvider(_services)));

                return client;
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                return null;
            }
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken);
    }
}
