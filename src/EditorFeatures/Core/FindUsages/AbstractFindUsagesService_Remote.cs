// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService : IFindUsagesService
    {
        private static async Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, object callback, CancellationToken cancellationToken)
        {
            var outOfProcessAllowed = solution.Workspace.Options.GetOption(FindUsagesOptions.OutOfProcessAllowed);
            if (!outOfProcessAllowed)
            {
                return null;
            }

            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisServiceSessionAsync(
                solution, callback, cancellationToken).ConfigureAwait(false);
        }
    }
}