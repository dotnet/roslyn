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
            private readonly Task<RemoteHostClient> _instanceTask;

            public RemoteHostClientService(Workspace workspace)
            {
                var remoteHostClientFactory = workspace.Services.GetService<IRemoteHostClientFactory>();
                if (remoteHostClientFactory == null)
                {
                    // no implementation of remote host client
                    return;
                }

                _instanceTask = Task.Run(() => remoteHostClientFactory.CreateAsync(workspace, CancellationToken.None), CancellationToken.None);
            }

            public Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (_instanceTask == null)
                {
                    // service is in shutdown mode or not enabled
                    return SpecializedTasks.Default<RemoteHostClient>();
                }

                return _instanceTask;
            }
        }
    }
}
