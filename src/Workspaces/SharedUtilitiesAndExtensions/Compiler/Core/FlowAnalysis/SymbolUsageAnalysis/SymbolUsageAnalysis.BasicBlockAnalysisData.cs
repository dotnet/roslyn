// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis;

internal static partial class SymbolUsageAnalysis
{
    /// <summary>
    /// Analysis data for a particular <see cref="BasicBlock"/> for <see cref="ControlFlowGraph"/>
    /// based dataflow analysis OR for the entire executable code block for high level operation
    /// tree based analysis.
    /// </summary>
    private sealed class BasicBlockAnalysisData : IDisposable
    {
        private static readonly ObjectPool<BasicBlockAnalysisData> s_pool =
            new(() => new BasicBlockAnalysisData());

        /// <summary>
        /// Map from each symbol to possible set of reachable write operations that are live at current program point.
        /// A write is live if there is no intermediate write operation that overwrites it.
        /// </summary>
        private readonly Dictionary<ISymbol, PooledHashSet<IOperation>> _reachingWrites;

        private BasicBlockAnalysisData()
            => _reachingWrites = [];

        public static BasicBlockAnalysisData GetInstance() => s_pool.Allocate();

        public void Dispose()
        {
            FreeAndClearValues();
            s_pool.Free(this);
        }

        private void FreeAndClearValues()
        {
            foreach (var value in _reachingWrites.Values)
            {
                value.Free();
            }

            _reachingWrites.Clear();
        }

        public void SetAnalysisDataFrom(BasicBlockAnalysisData other)
        {
            if (ReferenceEquals(this, other))
            {
                return;
            }

            FreeAndClearValues();
            AddEntries(_reachingWrites, other);
        }

        public void Clear(ISymbol symbol)
        {
            if (_reachingWrites.TryGetValue(symbol, out var value))
            {
                value.Free();
                _reachingWrites.Remove(symbol);
            }
        }

        /// <summary>
        /// Gets the currently reachable writes for the given symbol.
        /// </summary>
        public void ForEachCurrentWrite<TArg>(ISymbol symbol, Action<IOperation, TArg> action, TArg arg)
        {
            ForEachCurrentWrite(
                symbol,
                static (write, arg) =>
                {
                    arg.action(write, arg.arg);
                    return true;
                },
                (action, arg));
        }

        public bool ForEachCurrentWrite<TArg>(ISymbol symbol, Func<IOperation, TArg, bool> action, TArg arg)
        {
            if (_reachingWrites.TryGetValue(symbol, out var values))
            {
                foreach (var value in values)
                {
                    if (!action(value, arg))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Marks the given symbol write as a new unread write operation,
        /// potentially clearing out the prior write operations if <paramref name="maybeWritten"/> is <code>false</code>.
        /// </summary>
        public void OnWriteReferenceFound(ISymbol symbol, IOperation operation, bool maybeWritten)
        {
            if (!_reachingWrites.TryGetValue(symbol, out var values))
            {
                values = PooledHashSet<IOperation>.GetInstance();
                _reachingWrites.Add(symbol, values);
            }
            else if (!maybeWritten)
            {
                values.Clear();
            }

            values.Add(operation);
        }

        public bool Equals(BasicBlockAnalysisData other)
        {
            // Check if both _reachingWrites maps have same key-value pair count.
            if (other == null ||
                other._reachingWrites.Count != _reachingWrites.Count)
            {
                return false;
            }

            // Check if both _reachingWrites maps have same set of keys.  This is a quick out based on O(keys),
            // instead of doing the full O(k*v) check below.
            foreach (var key in _reachingWrites.Keys)
            {
                if (!other._reachingWrites.ContainsKey(key))
                    return false;
            }

            // Check if both _reachingWrites maps have same set of write operations for each tracked symbol.
            foreach (var (symbol, writes1) in _reachingWrites)
            {
                var writes2 = other._reachingWrites[symbol];
                if (!SetEquals(writes1, writes2))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Same as <see cref="HashSet{T}.SetEquals(IEnumerable{T})"/>, except this avoids allocations by
        /// enumerating the set directly with a no-alloc enumerator.
        /// </summary>
        private static bool SetEquals<T>(HashSet<T> set1, HashSet<T> set2)
        {
#if NET8_0_OR_GREATER
            // 📝 PERF: The boxed enumerator allocation that appears in some traces was fixed in .NET 8:
            // https://github.com/dotnet/runtime/pull/78613
            return set1.SetEquals(set2);
#else
            // same logic as https://github.com/dotnet/runtime/blob/62d6a8fe599ea3a77ef7af3c7660d398d692f062/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSet.cs#L1192

            if (set1.Count != set2.Count)
                return false;

            foreach (var operation in set1)
            {
                if (!set2.Contains(operation))
                    return false;
            }

            return true;
#endif
        }

        private bool IsEmpty => _reachingWrites.Count == 0;

        public static BasicBlockAnalysisData Merge(
            BasicBlockAnalysisData data1,
            BasicBlockAnalysisData data2,
            Action<BasicBlockAnalysisData> trackAllocatedData)
        {
            // Ensure that we don't return 'null' data if other the other data is non-null,
            // even if latter is Empty.
            if (data1 == null)
            {
                return data2;
            }
            else if (data2 == null)
            {
                return data1;
            }
            else if (data1.IsEmpty)
            {
                return data2;
            }
            else if (data2.IsEmpty)
            {
                return data1;
            }
            else if (data1.Equals(data2))
            {
                return data1;
            }

            var mergedData = GetInstance();

            AddEntries(mergedData._reachingWrites, data1);
            AddEntries(mergedData._reachingWrites, data2);

            if (mergedData.Equals(data1))
            {
                mergedData.Dispose();
                return data1;
            }
            else if (mergedData.Equals(data2))
            {
                mergedData.Dispose();
                return data2;
            }

            trackAllocatedData(mergedData);
            return mergedData;
        }

        private static void AddEntries(Dictionary<ISymbol, PooledHashSet<IOperation>> result, BasicBlockAnalysisData source)
        {
            if (source != null)
            {
                foreach (var (symbol, operations) in source._reachingWrites)
                {
                    if (!result.TryGetValue(symbol, out var values))
                    {
                        values = PooledHashSet<IOperation>.GetInstance();
                        result.Add(symbol, values);
                    }

#if NET
                    values.EnsureCapacity(values.Count + operations.Count);
#endif

                    // Enumerate explicitly, instead of calling AddRange, to avoid unnecessary expensive IEnumerator allocation.
                    foreach (var operation in operations)
                        values.Add(operation);
                }
            }
        }
    }
}
