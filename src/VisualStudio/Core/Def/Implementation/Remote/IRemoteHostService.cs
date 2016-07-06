// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Remote
{
    // REVIEW: should this be in the workspace layer?
    internal interface IRemoteHostService : IWorkspaceService
    {
        // TODO: probably need to split service to registration service and one that return RemoteHost
        void Enable();
        void Disable();

        Task<RemoteHost> GetRemoteHostAsync(CancellationToken cancellationToken);
    }
}
