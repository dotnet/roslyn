// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Not threadsafe.  Only use while under a lock.
/// </summary>
internal sealed class RemoteSolutionCache<TChecksum, TSolution>
    where TChecksum : struct
    where TSolution : class
{
    /// <summary>
    /// The max number of solution instances we'll hold onto at a time.
    /// </summary>
    private readonly int _maxCapacity;

    /// <summary>
    /// The total history kept.  Used to record telemetry about how useful it would be to increase the max capacity.
    /// </summary>
    private readonly int _totalHistory;

    /// <summary>
    /// Keep track of when we find a checksum in the history, but we've dropped the solution for it.  This will help
    /// us determine what benefit we would get from expanding this cache.
    /// </summary>
    private readonly HistogramLogAggregator<int>.HistogramCounter _cacheMissCounter;

    /// <summary>
    /// The number of times we successfully found a solution.
    /// </summary>
    private int _cacheHits;

    /// <summary>
    /// The number of times we failed to find a solution, but could have found it if we cached more items (up to
    /// TotalHistory).  When this happens, we also store in <see cref="_cacheMissCounter"/> which bucket it was
    /// found in to help us decide what a good cache value is.
    /// </summary>
    private int _cacheMissesInHistory;

    /// <summary>
    /// The number of times we failed to find a solution, and would not have found it even if we didn't cache more items.
    /// </summary>
    private int _cacheMissesNotInHistory;

    private readonly LinkedList<CacheNode> _cacheNodes = new();

    public RemoteSolutionCache(int maxCapacity = 4, int totalHistory = 16)
    {
        _maxCapacity = maxCapacity;
        _totalHistory = totalHistory;
        _cacheMissCounter = new(bucketSize: 1, maxBucketValue: int.MaxValue, bucketCount: _totalHistory + 1);
    }

    private void FindAndMoveNodeToFront(TChecksum checksum)
    {
        var index = 0;
        for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
        {
            if (current.Value.Checksum.Equals(checksum))
            {
                // Found the item.  Take it, move it to the front, and ensure it's pointing at this solution.
                _cacheNodes.Remove(current);
                _cacheNodes.AddFirst(current);
                return;
            }
        }

        // Didn't find the item at all.  Just add to the front.
        _cacheNodes.AddFirst(new CacheNode(checksum));
    }

    public void Add(TChecksum checksum, TSolution solution)
    {
        Contract.ThrowIfTrue(_cacheNodes.Count > _totalHistory);

        FindAndMoveNodeToFront(checksum);

        Contract.ThrowIfTrue(_cacheNodes.Count > _totalHistory + 1);
        Contract.ThrowIfNull(_cacheNodes.First);
        Contract.ThrowIfFalse(_cacheNodes.First.Value.Checksum.Equals(checksum));

        // Ensure we're holding onto the solution.
        _cacheNodes.First.Value.Solution = solution;

        // Now, if our history is too long, remove the last item.
        if (_cacheNodes.Count == _totalHistory + 1)
            _cacheNodes.RemoveLast();

        // Finally, ensure that only the first `MaxCapacity` are pointing at solutions and the rest are not.
        var index = 0;
        for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
        {
            if (index >= _maxCapacity)
            {
                current.Value.Solution = null;
                break;
            }
        }
    }

    public TSolution? Find(TChecksum checksum)
    {
        // Note: we intentionally do not move an item when we find it.  That's because our caller will always call 'Add'
        // with the found solution afterwards.  This will ensure that that solution makes it to the front of the line.
        var index = 0;
        for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
        {
            if (current.Value.Checksum.Equals(checksum))
            {
                // Found it!
                if (current.Value.Solution is null)
                {
                    // Track that we would have been able to return this if our history was longer.
                    _cacheMissesInHistory++;
                    _cacheMissCounter.IncreaseCount(index);
                }
                else
                {
                    // Success!
                    _cacheHits++;
                }

                return current.Value.Solution;
            }
        }

        // Couldn't find it at all, even in the history.
        _cacheMissesNotInHistory++;
        return null;
    }

    public void ReportTelemetry()
    {
        Logger.Log(FunctionId.RemoteWorkspace_SolutionCachingStatistics, KeyValueLogMessage.Create(m =>
        {
            m.Add(nameof(_cacheHits), _cacheHits);
            m.Add(nameof(_cacheMissesInHistory), _cacheMissesInHistory);
            m.Add(nameof(_cacheMissesNotInHistory), _cacheMissesNotInHistory);
            _cacheMissCounter.WriteTelemetryPropertiesTo(m, prefix: nameof(_cacheMissCounter));
        }));
    }

    private sealed class CacheNode(TChecksum checksum)
    {
        public readonly TChecksum Checksum = checksum;
        public TSolution? Solution;
    }
}
