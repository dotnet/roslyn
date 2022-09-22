// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;
using VSThreading = Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed class VisualStudioRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), WorkspaceKind.Host), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly IAsyncServiceProvider _vsServiceProvider;
            private readonly AsynchronousOperationListenerProvider _listenerProvider;
            private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;
            private readonly IGlobalOptionService _globalOptions;
            private readonly IThreadingContext _threadingContext;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(
                SVsServiceProvider vsServiceProvider,
                AsynchronousOperationListenerProvider listenerProvider,
                IGlobalOptionService globalOptions,
                IThreadingContext threadingContext,
                [ImportMany] IEnumerable<Lazy<IRemoteServiceCallbackDispatcher, RemoteServiceCallbackDispatcherRegistry.ExportMetadata>> callbackDispatchers)
            {
                _vsServiceProvider = (IAsyncServiceProvider)vsServiceProvider;
                _globalOptions = globalOptions;
                _listenerProvider = listenerProvider;
                _threadingContext = threadingContext;
                _callbackDispatchers = new RemoteServiceCallbackDispatcherRegistry(callbackDispatchers);
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                // We don't want to bring up the OOP process in a VS cloud environment client instance
                // Avoids proffering brokered services on the client instance.
                if (!_globalOptions.GetOption(RemoteHostOptions.OOP64Bit) ||
                    workspaceServices.Workspace is not VisualStudioWorkspace ||
                    workspaceServices.GetRequiredService<IWorkspaceContextService>().IsCloudEnvironmentClient())
                {
                    // Run code in the current process
                    return new DefaultRemoteHostClientProvider();
                }

                return new VisualStudioRemoteHostClientProvider(workspaceServices, _globalOptions, _vsServiceProvider, _threadingContext, _listenerProvider, _callbackDispatchers);
            }
        }

        private readonly HostWorkspaceServices _services;
        private readonly IGlobalOptionService _globalOptions;
        private readonly VSThreading.AsyncLazy<RemoteHostClient?> _lazyClient;
        private readonly IAsyncServiceProvider _vsServiceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly AsynchronousOperationListenerProvider _listenerProvider;
        private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;

        private VisualStudioRemoteHostClientProvider(
            HostWorkspaceServices services,
            IGlobalOptionService globalOptions,
            IAsyncServiceProvider vsServiceProvider,
            IThreadingContext threadingContext,
            AsynchronousOperationListenerProvider listenerProvider,
            RemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            _services = services;
            _globalOptions = globalOptions;
            _vsServiceProvider = vsServiceProvider;
            _threadingContext = threadingContext;
            _listenerProvider = listenerProvider;
            _callbackDispatchers = callbackDispatchers;

            // using VS AsyncLazy here since Roslyn's is not compatible with JTF. 
            // Our ServiceBroker services may be invoked by other VS components under JTF.
            _lazyClient = new VSThreading.AsyncLazy<RemoteHostClient?>(CreateHostClientAsync, threadingContext.JoinableTaskFactory);
        }

        private async Task<RemoteHostClient?> CreateHostClientAsync()
        {
            try
            {
                var brokeredServiceContainer = await _vsServiceProvider.GetServiceAsync<SVsBrokeredServiceContainer, IBrokeredServiceContainer>(_threadingContext.JoinableTaskFactory).ConfigureAwait(false);
                var serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();

                var configuration =
                    (_globalOptions.GetOption(RemoteHostOptions.OOPCoreClrFeatureFlag) ? RemoteProcessConfiguration.Core : 0) |
                    (_globalOptions.GetOption(RemoteHostOptions.OOPServerGCFeatureFlag) ? RemoteProcessConfiguration.ServerGC : 0) |
                    (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler) ? RemoteProcessConfiguration.EnableSolutionCrawler : 0);

                // VS AsyncLazy does not currently support cancellation:
                var client = await ServiceHubRemoteHostClient.CreateAsync(_services, configuration, _listenerProvider, serviceBroker, _callbackDispatchers, CancellationToken.None).ConfigureAwait(false);

                // proffer in-proc brokered services:
                _ = brokeredServiceContainer.Proffer(SolutionAssetProvider.ServiceDescriptor, (_, _, _, _) => ValueTaskFactory.FromResult<object?>(new SolutionAssetProvider(_services)));

                return client;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return null;
            }
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken);
    }
}
