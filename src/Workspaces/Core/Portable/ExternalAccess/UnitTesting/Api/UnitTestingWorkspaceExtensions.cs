﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal static class UnitTestingWorkspaceExtensions
    {
        public async static Task<UnitTestingRemoteHostClientWrapper> TryGetUnitTestingRemoteHostClientWrapperAsync(this Workspace workspace, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            return new UnitTestingRemoteHostClientWrapper(client);
        }
    }
}
