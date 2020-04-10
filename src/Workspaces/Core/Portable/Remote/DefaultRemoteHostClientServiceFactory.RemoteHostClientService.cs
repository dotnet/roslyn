// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                => !(_lazyInstance is null);

            public Task<RemoteHostClient> TryGetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                if (_lazyInstance == null)
                {
                    return SpecializedTasks.Null<RemoteHostClient>();
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

                instance.Dispose();
            }

            private AsyncLazy<RemoteHostClient> CreateNewLazyRemoteHostClient()
                => new AsyncLazy<RemoteHostClient>(c => _remoteHostClientFactory.CreateAsync(_workspace, c), cacheResult: true);
        }
    }
}
