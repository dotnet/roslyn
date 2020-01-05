// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory)), Shared]
    internal class RemoteHostClientFactory : IRemoteHostClientFactory
    {
        [ImportingConstructor]
        public RemoteHostClientFactory()
        {
        }

        public async Task<RemoteHostClient?> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            // this is the point where we can create different kind of remote host client in future (cloud or etc)
            return await ServiceHubRemoteHostClient.CreateAsync(workspace, cancellationToken).ConfigureAwait(false);
        }
    }
}
