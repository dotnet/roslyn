// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingWorkspaceExtensions
    {
        public async static Task<UnitTestingRemoteHostClientWrapper> TryGetUnitTestingRemoteHostClientWrapperAsync(this Workspace workspace, CancellationToken cancellationToken)
        {
            var remoteHostClient = await workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return new UnitTestingRemoteHostClientWrapper(remoteHostClient);
        }
    }
}
