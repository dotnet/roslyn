// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Not threadsafe.  Only use while under a lock.
    /// </summary>
    internal sealed class RemoteSolutionCache
    {
        /// <summary>
        /// The max number of solution instances we'll hold onto at a time.
        /// </summary>
        private const int MaxCapacity = 4;

        /// <summary>
        /// The total history kept.  Used to record telemetry about how useful it would be to increase the max capacity.
        /// </summary>
        private const int TotalHistory = 16;

        /// <summary>
        /// Keep track of when we find a checksum in the history, but we've dropped the solution for it.  This will help
        /// us determine what benefit we would get from expanding this cache.
        /// </summary>
        private readonly HistogramLogAggregator<int>.HistogramCounter _cacheMissAggregator =
            new(bucketSize: 1, maxBucketValue: int.MaxValue, bucketCount: TotalHistory + 1);

        /// <summary>
        /// The number of times we successfully found a solution.
        /// </summary>
        private int _cacheHits;

        /// <summary>
        /// The number of times we failed to find a solution, but could have found it if we cached more items (up to
        /// TotalHistory).  When this happens, we also store in <see cref="_cacheMissAggregator"/> which bucket it was
        /// found in to help us decide what a good cache value is.
        /// </summary>
        private int _cacheMissesInHistory;

        /// <summary>
        /// The number of times we failed to find a solution, and would not have found it even if we didn't cache more items.
        /// </summary>
        private int _cacheMissesNotInHistory;

        private sealed class SolutionCacheNode
        {
            public readonly Checksum Checksum;
            public Solution? Solution;

            public SolutionCacheNode(Checksum checksum)
            {
                Checksum = checksum;
            }
        }

        private readonly LinkedList<SolutionCacheNode> _cacheNodes = new();

        private void FindAndMoveNodeToFront(Checksum checksum)
        {
            var index = 0;
            for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
            {
                if (current.Value.Checksum == checksum)
                {
                    // Found the item.  Take it, move it to the front, and ensure it's pointing at this solution.
                    _cacheNodes.Remove(current);
                    _cacheNodes.AddFirst(current);

                    // Keep track if we would have found this if the cache was larger
                    if (current.Value.Solution == null)
                    {
                        _cacheHits++;
                    }
                    else
                    {
                        _cacheMissesInHistory++;
                        _cacheMissAggregator.IncreaseCount(index);
                    }
                    return;
                }
            }

            // Didn't find the item at all.  Just add to the front.
            //
            // Note: we don't record 
            _cacheNodes.AddFirst(new SolutionCacheNode(checksum));
            _cacheMissesNotInHistory++;
            return;
        }

        public void Add(Checksum checksum, Solution solution)
        {
            Contract.ThrowIfTrue(_cacheNodes.Count > TotalHistory);

            FindAndMoveNodeToFront(checksum);

            Contract.ThrowIfTrue(_cacheNodes.Count > TotalHistory + 1);
            Contract.ThrowIfNull(_cacheNodes.First);
            Contract.ThrowIfTrue(_cacheNodes.First.Value.Checksum != checksum);

            // Ensure we're holding onto the solution.
            _cacheNodes.First.Value.Solution = solution;

            // Now, if our history is too long, remove the last item.
            if (_cacheNodes.Count == TotalHistory + 1)
                _cacheNodes.RemoveLast();

            // Finally, ensure that only the first `MaxCapacity` are pointing at solutions and the rest are not.
            var index = 0;
            for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
            {
                if (index > MaxCapacity)
                {
                    current.Value.Solution = null;
                    break;
                }
            }
        }
    }

    internal sealed partial class RemoteWorkspace
    {
        /// <summary>
        /// The last solution for the primary branch fetched from the client.  Cached as it's very common to have a
        /// flurry of requests for the same checksum that don't run concurrently.  Only read/write while holding <see
        /// cref="_gate"/>.
        /// </summary>
        private (Checksum checksum, Solution solution) _lastRequestedPrimaryBranchSolution;

        /// <summary>
        /// The last solution requested by a service.  Cached as it's very common to have a flurry of requests for the
        /// same checksum that don't run concurrently.  Only read/write while holding <see cref="_gate"/>.
        /// </summary>
        private readonly RemoteSolutionCache _lastRequestedAnyBranchSolutions = new();

        /// <summary>
        /// Mapping from solution-checksum to the solution computed for it.  This is used so that we can hold a solution
        /// around as long as the checksum for it is being used in service of some feature operation (e.g.
        /// classification).  As long as we're holding onto it, concurrent feature requests for the same solution
        /// checksum can share the computation of that particular solution and avoid duplicated concurrent work.  Only
        /// read/write while holding <see cref="_gate"/>.
        /// </summary>
        private readonly Dictionary<Checksum, InFlightSolution> _solutionChecksumToSolution = [];

        /// <summary>
        /// Deliberately not cancellable.  This code must always run fully to completion.
        /// </summary>
        private InFlightSolution GetOrCreateSolutionAndAddInFlightCount_NoLock(
            AssetProvider assetProvider,
            Checksum solutionChecksum,
            int workspaceVersion,
            bool updatePrimaryBranch)
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

            CheckCacheInvariants_NoLock();

            var solution = GetOrCreateSolutionAndAddInFlightCount_NoLock();

            // The solution must now have a valid in-flight-count.
            Contract.ThrowIfTrue(solution.InFlightCount < 1);

            // We may be getting back a solution that only was computing a non-primary branch.  If we were asked
            // to compute the primary branch as well, let it know so it can start that now.
            if (updatePrimaryBranch)
            {
                solution.TryKickOffPrimaryBranchWork_NoLock((disconnectedSolution, cancellationToken) =>
                    this.TryUpdateWorkspaceCurrentSolutionAsync(workspaceVersion, disconnectedSolution, cancellationToken));
            }

            CheckCacheInvariants_NoLock();

            return solution;

            InFlightSolution GetOrCreateSolutionAndAddInFlightCount_NoLock()
            {
                Contract.ThrowIfFalse(_gate.CurrentCount == 0);

                if (_solutionChecksumToSolution.TryGetValue(solutionChecksum, out var solution))
                {
                    // The cached solution must have a valid in-flight-count
                    Contract.ThrowIfTrue(solution.InFlightCount < 1);

                    // Increase the count as our caller now is keeping this solution in-flight
                    solution.IncrementInFlightCount_NoLock();
                    Contract.ThrowIfTrue(solution.InFlightCount < 2);

                    return solution;
                }

                // See if we're being asked for a checksum we already have cached a solution for.  Safe to read directly
                // as we're holding _gate.
                var cachedSolution = _lastRequestedPrimaryBranchSolution.checksum == solutionChecksum
                    ? _lastRequestedPrimaryBranchSolution.solution
                    : _lastRequestedAnyBranchSolutions.Find(solutionChecksum);

                // We're the first call that is asking about this checksum.  Kick off async computation to compute it
                // (or use an existing cached value we already have).  Start with an in-flight-count of 1 to represent
                // our caller. 
                solution = new InFlightSolution(
                    this, solutionChecksum,
                    async cancellationToken => cachedSolution ?? await ComputeDisconnectedSolutionAsync(assetProvider, solutionChecksum, cancellationToken).ConfigureAwait(false));
                Contract.ThrowIfFalse(solution.InFlightCount == 1);

                _solutionChecksumToSolution.Add(solutionChecksum, solution);

                return solution;
            }
        }

        private void CheckCacheInvariants_NoLock()
        {
            Contract.ThrowIfFalse(_gate.CurrentCount == 0);

            foreach (var (solutionChecksum, solution) in _solutionChecksumToSolution)
            {
                // Anything in this dictionary is currently in flight with an existing request.  So it must have an
                // in-flight-count of at least 1.  Note: this in-flight-request may be an actual request that has come
                // in from the client.  Or it can be a virtual one we've created through _lastAnyBranchSolution or
                // _lastPrimaryBranchSolution
                Contract.ThrowIfTrue(solution.InFlightCount < 1);
                Contract.ThrowIfTrue(solutionChecksum != solution.SolutionChecksum);
            }
        }
    }
}
