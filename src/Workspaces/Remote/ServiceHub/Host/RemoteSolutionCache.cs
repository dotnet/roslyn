// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// LRU cache of checksum+solution pairs.  Used to keep track of the last few solutions the remote server knows about,
/// helping to avoid unnecessary syncs/recreations of those solutions while many requests are coming into the server.
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
    /// Keep track of what index we found find a checksum at in the history.  This will help us tell both if the cache
    /// is too large, or if it's too small.
    /// </summary>
    private readonly HistogramLogAggregator<int>.HistogramCounter _cacheHitIndexHistogram;

    /// <summary>
    /// The number of times we successfully found a solution.  When this happens we'll increment <see
    /// cref="_cacheHitIndexHistogram"/>.
    /// </summary>
    private int _cacheHits;

    /// <summary>
    /// The number of times we failed to find a solution, but could have found it if we cached more items (up to
    /// TotalHistory).  When this happens we'll increment <see cref="_cacheHitIndexHistogram"/>.
    /// </summary>
    private int _cacheMissesInHistory;

    /// <summary>
    /// The number of times we failed to find a solution, and would not have found it even if we didn't cache more items.
    /// </summary>
    private int _cacheMissesNotInHistory;

    /// <summary>
    /// The list of checksum+solution pairs.  Note: only the first <see cref="_maxCapacity"/> items will actually point
    /// at a non-null solution.  The ones after that will point at <see langword="null"/>.  We store both so that we can
    /// collect useful telemetry on how much benefit we would get by having a larger history.
    /// </summary>
    private readonly LinkedList<CacheNode> _cacheNodes = new();

    public RemoteSolutionCache(int maxCapacity = 4, int totalHistory = 16)
    {
        _maxCapacity = maxCapacity;
        _totalHistory = totalHistory;
        _cacheHitIndexHistogram = new(bucketSize: 1, maxBucketValue: int.MaxValue, bucketCount: _totalHistory + 1);
    }

    private void FindAndMoveNodeToFront(TChecksum checksum)
    {
        var index = 0;
        for (var current = _cacheNodes.First; current != null; current = current.Next, index++)
        {
            if (current.Value.Checksum.Equals(checksum))
            {
                // Found the item.  
                if (index > 0)
                {
                    // If it's not already at the front, move it there.
                    _cacheNodes.Remove(current);
                    _cacheNodes.AddFirst(current);
                }

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
                }
                else
                {
                    // Success!
                    _cacheHits++;
                }

                _cacheHitIndexHistogram.IncreaseCount(index);
                return current.Value.Solution;
            }
        }

        // Couldn't find it at all, even in the history.
        _cacheMissesNotInHistory++;
        return null;
    }

    public void ReportTelemetry()
    {
        Logger.Log(FunctionId.RemoteWorkspace_SolutionCachingStatistics, KeyValueLogMessage.Create(static (m, @this) =>
        {
            m.Add(nameof(_cacheHits), @this._cacheHits);
            m.Add(nameof(_cacheMissesInHistory), @this._cacheMissesInHistory);
            m.Add(nameof(_cacheMissesNotInHistory), @this._cacheMissesNotInHistory);
            @this._cacheHitIndexHistogram.WriteTelemetryPropertiesTo(m, prefix: nameof(_cacheHitIndexHistogram));
        }, this));
    }

    public void AddAllTo(HashSet<TSolution> solutions)
    {
        foreach (var node in _cacheNodes)
            solutions.AddIfNotNull(node.Solution);
    }

    private sealed class CacheNode(TChecksum checksum)
    {
        public readonly TChecksum Checksum = checksum;
        public TSolution? Solution;
    }
}
