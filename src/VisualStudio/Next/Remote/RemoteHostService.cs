// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportWorkspaceServiceFactory(typeof(IRemoteHostService)), Shared]
    internal partial class RemoteHostServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new RemoteHostService(workspaceServices.Workspace);
        }

        private class RemoteHostService : IRemoteHostService
        {
            private readonly Workspace _workspace;
            private readonly SemaphoreSlim _lock;

            private CancellationTokenSource _shutdown;
            private Task<RemoteHost> _instance;

            public RemoteHostService(Workspace workspace)
            {
                _workspace = workspace;
                _lock = new SemaphoreSlim(initialCount: 1);
            }

            public void Enable()
            {
                using (_lock.DisposableWait())
                {
                    if (_instance != null)
                    {
                        // already enabled
                        return;
                    }

                    // make sure we run it on background thread
                    _shutdown = new CancellationTokenSource();
                    _instance = Task.Run(() => StartInternalAsync(), _shutdown.Token);
                }
            }

            public void Disable()
            {
                using (_lock.DisposableWait())
                {
                    if (_instance == null)
                    {
                        // already disabled
                        return;
                    }

                    var instance = _instance;
                    _instance = null;

                    _shutdown.Cancel();

                    try
                    {
                        instance.Result.Shutdown();
                    }
                    catch (OperationCanceledException)
                    {
                        // _instance wasn't finished running yet.
                    }
                }
            }

            public async Task<RemoteHost> GetRemoteHostAsync(CancellationToken cancellationToken)
            {
                var instance = _instance;
                if (instance == null)
                {
                    // service is in shutdown mode
                    return null;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // make sure instance was started
                return await instance.ConfigureAwait(false);
            }

            private async Task<RemoteHost> StartInternalAsync()
            {
                // TODO: abstract this out so that we can have more host than service hub
                var instance = await ServiceHubRemoteHost.CreateAsync(_workspace, _shutdown.Token).ConfigureAwait(false);
                instance.ConnectionChanged += OnConnectionChanged;

                return instance;
            }

            private void OnConnectionChanged(object sender, bool connection)
            {
                if (!connection)
                {
                    // TODO: make this logic better by making sure we don't endlessly retry to
                    //       get out of proc connection and make sure when we failed to make connection,
                    //       we change operation to use in proc implementation

                    // re-start remote host
                    _instance = StartInternalAsync();
                }
            }
        }
    }
}
