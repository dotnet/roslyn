// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Test.Utilities.Remote;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Remote
{
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory), layer: ServiceLayer.Host), Shared]
    internal class RemoteHostClientFactory : IRemoteHostClientFactory
    {
        public async Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            // this is the point where we can create different kind of remote host client in future (cloud or etc)
            if (workspace.Options.GetOption(RemoteHostClientFactoryOptions.RemoteHost_InProc))
            {
                var client = await InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                // register workspace host for in proc remote host client
                await ServiceHubRemoteHostClient.RegisterWorkspaceHostAsync(workspace, client).ConfigureAwait(false);

                return client;
            }

            return await ServiceHubRemoteHostClient.CreateAsync(workspace, cancellationToken).ConfigureAwait(false);
        }
    }
}