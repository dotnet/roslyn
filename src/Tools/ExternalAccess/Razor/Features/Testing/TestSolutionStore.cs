// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor;

internal sealed class TestSolutionStore
{
    private readonly Dictionary<Checksum, Solution> _solutions = [];

    internal async Task<RazorPinnedSolutionInfoWrapper> AddAsync(Solution solution, CancellationToken cancellationToken)
    {
        // Using compilation state, since that is what is used in the real SolutionAssetStorage class
        // Compilation state is the SolutionState checksum, plus source generator info, which seems pretty relevant :)
        var checksum = await solution.CompilationState.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

        lock (_solutions)
        {
            if (_solutions.TryGetValue(checksum, out var existingSolution))
            {
                return checksum;
            }

            _solutions.Add(checksum, solution);
        }

        return checksum;
    }

    internal Solution? Get(RazorPinnedSolutionInfoWrapper solutionInfo)
    {
        lock (_solutions)
        {
            _solutions.TryGetValue(solutionInfo.UnderlyingObject, out var solution);

            return solution;
        }
    }
}
