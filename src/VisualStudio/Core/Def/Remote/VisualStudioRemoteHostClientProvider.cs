// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Preview;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using VSThreading = Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed class VisualStudioRemoteHostClientProvider : IRemoteHostClientProvider
    {
        [ExportWorkspaceServiceFactory(typeof(IRemoteHostClientProvider), [WorkspaceKind.Host, WorkspaceKind.Preview]), Shared]
        internal sealed class Factory : IWorkspaceServiceFactory
        {
            private readonly VisualStudioWorkspace _vsWorkspace;
            private readonly IVsService<IBrokeredServiceContainer> _brokeredServiceContainer;
            private readonly AsynchronousOperationListenerProvider _listenerProvider;
            private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;
            private readonly IGlobalOptionService _globalOptions;
            private readonly IThreadingContext _threadingContext;

            private readonly object _gate = new();
            private VisualStudioRemoteHostClientProvider? _cachedVSInstance;

            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory(
                VisualStudioWorkspace vsWorkspace,
                IVsService<SVsBrokeredServiceContainer, IBrokeredServiceContainer> brokeredServiceContainer,
                AsynchronousOperationListenerProvider listenerProvider,
                IGlobalOptionService globalOptions,
                IThreadingContext threadingContext,
                [ImportMany] IEnumerable<Lazy<IRemoteServiceCallbackDispatcher, RemoteServiceCallbackDispatcherRegistry.ExportMetadata>> callbackDispatchers)
            {
                _globalOptions = globalOptions;
                _vsWorkspace = vsWorkspace;
                _brokeredServiceContainer = brokeredServiceContainer;
                _listenerProvider = listenerProvider;
                _threadingContext = threadingContext;
                _callbackDispatchers = new RemoteServiceCallbackDispatcherRegistry(callbackDispatchers);
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            {
                Debug.Assert(workspaceServices.Workspace is VisualStudioWorkspace or PreviewWorkspace);

                // We don't want to bring up the OOP process in a VS cloud environment client instance
                // Avoids proffering brokered services on the client instance.
                if (!_globalOptions.GetOption(RemoteHostOptionsStorage.OOP64Bit) ||
                    // If the host services are different, then we can't use the cached VS instance, fall back to in-proc.
                    // This can happen for preview workspace in Tools|Options.
                    workspaceServices.SolutionServices.WorkspaceServices.HostServices != _vsWorkspace.Services.HostServices ||
                    workspaceServices.GetRequiredService<IWorkspaceContextService>().IsCloudEnvironmentClient())
                {
                    // Run code in the current process
                    return new DefaultRemoteHostClientProvider();
                }

                lock (_gate)
                {
                    // If we have a cached vs instance, then we can return that instance since we know they have the same host services.
                    // Otherwise, create and cache an instance based on vs workspace for future callers with same services.
                    if (_cachedVSInstance is null)
                        _cachedVSInstance = new VisualStudioRemoteHostClientProvider(_vsWorkspace.Services.SolutionServices, _globalOptions, _brokeredServiceContainer, _threadingContext, _listenerProvider, _callbackDispatchers);

                    return _cachedVSInstance;
                }
            }
        }

        public readonly SolutionServices Services;
        private readonly IGlobalOptionService _globalOptions;
        private readonly VSThreading.AsyncLazy<RemoteHostClient?> _lazyClient;
        private readonly IVsService<IBrokeredServiceContainer> _brokeredServiceContainer;
        private readonly AsynchronousOperationListenerProvider _listenerProvider;
        private readonly RemoteServiceCallbackDispatcherRegistry _callbackDispatchers;
        private readonly TaskCompletionSource<bool> _clientCreationSource = new();

        private VisualStudioRemoteHostClientProvider(
            SolutionServices services,
            IGlobalOptionService globalOptions,
            IVsService<IBrokeredServiceContainer> brokeredServiceContainer,
            IThreadingContext threadingContext,
            AsynchronousOperationListenerProvider listenerProvider,
            RemoteServiceCallbackDispatcherRegistry callbackDispatchers)
        {
            Services = services;
            _globalOptions = globalOptions;
            _brokeredServiceContainer = brokeredServiceContainer;
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
                var brokeredServiceContainer = await _brokeredServiceContainer.GetValueAsync().ConfigureAwait(false);
                var serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();

                var configuration =
                    _globalOptions.GetOption(RemoteHostOptionsStorage.OOPServerGCFeatureFlag) ? RemoteProcessConfiguration.ServerGC : 0;

                // VS AsyncLazy does not currently support cancellation:
                var client = await ServiceHubRemoteHostClient.CreateAsync(Services, configuration, _listenerProvider, serviceBroker, _callbackDispatchers, CancellationToken.None).ConfigureAwait(false);

                // proffer in-proc brokered services:
                _ = brokeredServiceContainer.Proffer(SolutionAssetProvider.ServiceDescriptor, (_, _, _, _) => ValueTaskFactory.FromResult<object?>(new SolutionAssetProvider(Services)));

                return client;
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e))
            {
                return null;
            }
            finally
            {
                _clientCreationSource.SetResult(true);
            }
        }

        public Task<RemoteHostClient?> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            => _lazyClient.GetValueAsync(cancellationToken);

        public Task WaitForClientCreationAsync(CancellationToken cancellationToken)
            => _clientCreationSource.Task.WithCancellation(cancellationToken);
    }
}
