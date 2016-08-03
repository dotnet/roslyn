// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory)), Shared]
    internal class RemoteHostClientFactory : IRemoteHostClientFactory
    {
        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            // this is the point where we can create different kind of remote host client in future (cloud or etc)
            return ServiceHubRemoteHostClient.CreateAsync(workspace, cancellationToken);
        }
    }
}
