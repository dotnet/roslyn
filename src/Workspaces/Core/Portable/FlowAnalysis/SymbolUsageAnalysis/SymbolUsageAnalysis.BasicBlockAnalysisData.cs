// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.SymbolUsageAnalysis
{
    internal static partial class SymbolUsageAnalysis
    {
        /// <summary>
        /// Analysis data for a particular <see cref="BasicBlock"/> for <see cref="ControlFlowGraph"/>
        /// based dataflow analysis OR for the entire executable code block for high level operation
        /// tree based analysis.
        /// </summary>
        private sealed class BasicBlockAnalysisData : IEquatable<BasicBlockAnalysisData>
        {
            private static readonly ObjectPool<BasicBlockAnalysisData> s_pool =
                new ObjectPool<BasicBlockAnalysisData>(() => new BasicBlockAnalysisData());

            /// <summary>
            /// Map from each symbol to possible set of reachable write operations that are live at current program point.
            /// A write is live if there is no intermediate write operation that overwrites it.
            /// </summary>
            private readonly Dictionary<ISymbol, PooledHashSet<IOperation>> _reachingWrites;

            private BasicBlockAnalysisData()
            {
                _reachingWrites = new Dictionary<ISymbol, PooledHashSet<IOperation>>();
            }

            public static BasicBlockAnalysisData GetInstance() => s_pool.Allocate();

            public void Free()
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

            /// <summary>
            /// Gets the currently reachable writes for the given symbol.
            /// </summary>
            public IEnumerable<IOperation> GetCurrentWrites(ISymbol symbol)
            {
                if (_reachingWrites.TryGetValue(symbol, out var values))
                {
                    foreach (var value in values)
                    {
                        yield return value;
                    }
                }
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
                => this.GetHashCode() == (other?.GetHashCode() ?? 0);

            public override bool Equals(object obj)
            {
                return Equals(obj as BasicBlockAnalysisData);
            }

            public override int GetHashCode()
            {
                var hashCode = _reachingWrites.Count;
                foreach ((int keyHash, int valueHash) in _reachingWrites.Select(GetKeyValueHashCodeTuple).OrderBy(hashTuple => hashTuple.keyHash))
                {
                    hashCode = Hash.Combine(Hash.Combine(keyHash, valueHash), hashCode);
                }

                return hashCode;

                // Local functions.
                (int keyHash, int valueHash) GetKeyValueHashCodeTuple(KeyValuePair<ISymbol, PooledHashSet<IOperation>> keyValuePair)
                    => (keyValuePair.Key.GetHashCode(), Hash.Combine(keyValuePair.Value.Count, Hash.CombineValues(keyValuePair.Value)));
            }

            private bool IsEmpty => _reachingWrites.Count == 0;

            public static BasicBlockAnalysisData Merge(
                BasicBlockAnalysisData data1,
                BasicBlockAnalysisData data2,
                Func<BasicBlockAnalysisData> createBasicBlockAnalysisData)
            {
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

                var mergedData = createBasicBlockAnalysisData();
                AddEntries(mergedData._reachingWrites, data1);
                AddEntries(mergedData._reachingWrites, data2);

                if (mergedData.Equals(data1))
                {
                    return data1;
                }
                else if (mergedData.Equals(data2))
                {
                    return data2;
                }

                return mergedData;
            }

            private static void AddEntries(Dictionary<ISymbol, PooledHashSet<IOperation>> result, BasicBlockAnalysisData source)
            {
                if (source != null)
                {
                    foreach (var kvp in source._reachingWrites)
                    {
                        if (!result.TryGetValue(kvp.Key, out var values))
                        {
                            values = PooledHashSet<IOperation>.GetInstance();
                            result.Add(kvp.Key, values);
                        }

                        values.AddRange(kvp.Value);
                    }
                }
            }
        }
    }
}
