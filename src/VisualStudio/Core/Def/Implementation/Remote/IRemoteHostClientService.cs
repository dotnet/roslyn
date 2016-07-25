// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Remote
{
    /// <summary>
    /// Return <see cref="RemoteHostClient"/> user can use to communicate with remote host (ex, ServiceHub) 
    /// </summary>
    internal interface IRemoteHostClientService : IWorkspaceService
    {
        // TODO: split service to registration service and one that return RemoteHostClient
        void Enable();
        void Disable();

        Task<RemoteHostClient> GetRemoteHostClientAsync(CancellationToken cancellationToken);
    }
}
