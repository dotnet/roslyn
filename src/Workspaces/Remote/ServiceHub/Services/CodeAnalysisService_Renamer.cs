// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteRenamer
    {
        public Task<SerializableRenameLocations> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameOptionSet options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableRenameLocations>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await symbolAndProjectId.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    if (symbol == null)
                        return null;

                    var result = await Renamer.FindRenameLocationsAsync(
                        solution, symbol, options.Rehydrate(), cancellationToken).ConfigureAwait(false);
                    return result.Dehydrate(solution, cancellationToken);
                }
            }, cancellationToken);
        }
    }
}
