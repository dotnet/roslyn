// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FlowAnalysis.ReachingDefinitions
{
    internal static partial class ReachingDefinitionsAnalysis
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

            private readonly Dictionary<ISymbol, PooledHashSet<IOperation>> _reachingDefinitions;

            private BasicBlockAnalysisData()
            {
                _reachingDefinitions = new Dictionary<ISymbol, PooledHashSet<IOperation>>();
            }

            public static BasicBlockAnalysisData GetInstance() => s_pool.Allocate();

            public void Free()
            {
                FreeAndClearValues();
                s_pool.Free(this);
            }

            private void FreeAndClearValues()
            {
                foreach (var value in _reachingDefinitions.Values)
                {
                    value.Free();
                }

                _reachingDefinitions.Clear();
            }

            public void SetAnalysisDataFrom(BasicBlockAnalysisData other)
            {
                if (ReferenceEquals(this, other))
                {
                    return;
                }

                FreeAndClearValues();
                AddEntries(_reachingDefinitions, other);
            }

            /// <summary>
            /// Gets the currently reachable definitions (writes) for the given symbol.
            /// </summary>
            public IEnumerable<IOperation> GetCurrentDefinitions(ISymbol symbol)
            {
                if (_reachingDefinitions.TryGetValue(symbol, out var values))
                {
                    foreach (var value in values)
                    {
                        yield return value;
                    }
                }
            }

            /// <summary>
            /// Marks the given symbol definition as a new unused reaching definition,
            /// potentially clearing out the prior reaching definitions.
            /// </summary>
            public void OnWriteReferenceFound(ISymbol symbol, IOperation operation, bool maybeWritten)
            {
                if (!_reachingDefinitions.TryGetValue(symbol, out var values))
                {
                    values = PooledHashSet<IOperation>.GetInstance();
                    _reachingDefinitions.Add(symbol, values);
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
                var hashCode = _reachingDefinitions.Count;
                foreach ((int keyHash, int valueHash) in _reachingDefinitions.Select(GetKeyValueHashCodeTuple).OrderBy(hashTuple => hashTuple.keyHash))
                {
                    hashCode = Hash.Combine(Hash.Combine(keyHash, valueHash), hashCode);
                }

                return hashCode;

                // Local functions.
                (int keyHash, int valueHash) GetKeyValueHashCodeTuple(KeyValuePair<ISymbol, PooledHashSet<IOperation>> keyValuePair)
                    => (keyValuePair.Key.GetHashCode(), Hash.Combine(keyValuePair.Value.Count, Hash.CombineValues(keyValuePair.Value)));
            }

            private bool IsEmpty => _reachingDefinitions.Count == 0;

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
                AddEntries(mergedData._reachingDefinitions, data1);
                AddEntries(mergedData._reachingDefinitions, data2);

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
                    foreach (var kvp in source._reachingDefinitions)
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
