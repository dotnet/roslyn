// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostClientExtensions
    {
        /// <summary>
        /// Synchronize given solution as primary workspace solution in remote host
        /// </summary>
        public static async Task SynchronizePrimaryWorkspaceAsync(this Workspace workspace, Solution solution, CancellationToken cancellationToken)
        {
            if (solution.BranchId != solution.Workspace.PrimaryBranchId)
            {
                return;
            }

            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
            {
                var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                _ = await client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService,
                    nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync),
                    new object[] { checksum, solution.WorkspaceVersion },
                    solution,
                    callbackTarget: null,
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
