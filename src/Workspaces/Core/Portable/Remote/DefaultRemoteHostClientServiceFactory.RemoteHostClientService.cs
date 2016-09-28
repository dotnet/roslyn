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
            private readonly AsyncLazy<RemoteHostClient> _lazyInstance;

            public RemoteHostClientService(Workspace workspace)
            {
                var remoteHostClientFactory = workspace.Services.GetService<IRemoteHostClientFactory>();
                if (remoteHostClientFactory == null)
                {
                    // no implementation of remote host client
                    return;
                }

                _lazyInstance = new AsyncLazy<RemoteHostClient>(c => remoteHostClientFactory.CreateAsync(workspace, c), cacheResult: true);
            }

            public Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                if (_lazyInstance == null)
                {
                    return SpecializedTasks.Default<RemoteHostClient>();
                }

                return _lazyInstance.GetValueAsync(cancellationToken);
            }
        }
    }
}
