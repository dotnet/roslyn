// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Remote.Razor;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed class TestBrokeredServiceInterceptor : IRazorBrokeredServiceInterceptor
{
    private readonly Dictionary<Checksum, Solution> _solutions = [];
    private readonly Dictionary<SolutionId, Solution> _localToRemoteSolutionMap = [];

    public async Task<RazorSolutionWrapper> GetSolutionInfoAsync(Solution solution, CancellationToken cancellationToken)
    {
        // Using compilation state, since that is what is used in the real SolutionAssetStorage class.
        // Compilation state is the SolutionState checksum, plus source generator info, which seems pretty relevant :)
        var checksum = await solution.CompilationState.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

        lock (_solutions)
        {
            if (!_solutions.TryGetValue(checksum, out _))
            {
                _solutions.Add(checksum, solution);
            }
        }

        return checksum;
    }

    public ValueTask RunServiceAsync(
        Func<CancellationToken, ValueTask> implementation,
        CancellationToken cancellationToken)
        => implementation(cancellationToken);

    public ValueTask<T> RunServiceAsync<T>(
        RazorSolutionWrapper solutionInfo,
        Func<Solution, ValueTask<T>> implementation,
        CancellationToken cancellationToken)
    {
        var solution = GetSolution(solutionInfo);

        Assert.NotNull(solution);

        // Rather than actually syncing assets, we just let the test author directly map from a local solution
        // to a remote solution;
        if (_localToRemoteSolutionMap.TryGetValue(solution.Id, out var remoteSolution))
        {
            solution = remoteSolution;
        }

        return implementation(solution);
    }

    internal void MapSolutionIdToRemote(SolutionId localSolutionId, Solution remoteSolution)
    {
        _localToRemoteSolutionMap.Add(localSolutionId, remoteSolution);
    }

    private Solution? GetSolution(RazorSolutionWrapper solutionInfo)
    {
        lock (_solutions)
        {
            _solutions.TryGetValue(solutionInfo.Checksum, out var solution);

            return solution;
        }
    }
}
