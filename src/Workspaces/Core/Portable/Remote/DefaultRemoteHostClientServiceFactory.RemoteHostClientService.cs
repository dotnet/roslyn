// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class DefaultRemoteHostClientServiceFactory
    {
        public class RemoteHostClientService : IRemoteHostClientService
        {
            private readonly Workspace _workspace;
            private readonly IRemoteHostClientFactory _remoteHostClientFactory;

            private AsyncLazy<RemoteHostClient> _lazyInstance;

            public RemoteHostClientService(Workspace workspace)
            {
                var remoteHostClientFactory = workspace.Services.GetService<IRemoteHostClientFactory>();
                if (remoteHostClientFactory == null)
                {
                    // no implementation of remote host client
                    return;
                }

                _workspace = workspace;
                _remoteHostClientFactory = remoteHostClientFactory;

                _lazyInstance = CreateNewLazyRemoteHostClient();
            }

            public bool IsEnabled()
            {
                return !(_lazyInstance is null);
            }

            public Task<RemoteHostClient> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                if (_lazyInstance == null)
                {
                    return SpecializedTasks.Default<RemoteHostClient>();
                }

                return _lazyInstance.GetValueAsync(cancellationToken);
            }

            public async Task RequestNewRemoteHostAsync(CancellationToken cancellationToken)
            {
                var instance = await TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
                if (instance == null)
                {
                    return;
                }

                _lazyInstance = CreateNewLazyRemoteHostClient();

                // let people know this remote host client is being disconnected
                instance.Shutdown();
            }

            private AsyncLazy<RemoteHostClient> CreateNewLazyRemoteHostClient()
            {
                return new AsyncLazy<RemoteHostClient>(c => _remoteHostClientFactory.CreateAsync(_workspace, c), cacheResult: true);
            }
        }
    }
}
