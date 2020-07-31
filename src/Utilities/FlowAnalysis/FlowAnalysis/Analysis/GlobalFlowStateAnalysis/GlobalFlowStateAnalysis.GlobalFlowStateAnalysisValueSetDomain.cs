// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.GlobalFlowStateAnalysis
{
    using GlobalFlowStateAnalysisData = DictionaryAnalysisData<AnalysisEntity, GlobalFlowStateAnalysisValueSet>;
    using GlobalFlowStateAnalysisResult = DataFlowAnalysisResult<GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>;

    internal partial class GlobalFlowStateAnalysis : ForwardDataFlowAnalysis<GlobalFlowStateAnalysisData, GlobalFlowStateAnalysisContext, GlobalFlowStateAnalysisResult, GlobalFlowStateBlockAnalysisResult, GlobalFlowStateAnalysisValueSet>
    {
        internal class GlobalFlowStateAnalysisValueSetDomain : AbstractValueDomain<GlobalFlowStateAnalysisValueSet>
        {
            public static GlobalFlowStateAnalysisValueSetDomain Instance = new GlobalFlowStateAnalysisValueSetDomain();

            private GlobalFlowStateAnalysisValueSetDomain() { }

            public override GlobalFlowStateAnalysisValueSet Bottom => GlobalFlowStateAnalysisValueSet.Unset;

            public override GlobalFlowStateAnalysisValueSet UnknownOrMayBeValue => GlobalFlowStateAnalysisValueSet.Unknown;

            public override int Compare(GlobalFlowStateAnalysisValueSet oldValue, GlobalFlowStateAnalysisValueSet newValue, bool assertMonotonicity)
            {
                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    return oldValue.Equals(newValue) ? 0 : -1;
                }
                else if (oldValue.Kind < newValue.Kind)
                {
                    return -1;
                }
                else
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }
            }

            public override GlobalFlowStateAnalysisValueSet Merge(GlobalFlowStateAnalysisValueSet value1, GlobalFlowStateAnalysisValueSet value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Unset)
                {
                    return value2;
                }
                else if (value2.Kind == GlobalFlowStateAnalysisValueSetKind.Unset)
                {
                    return value1;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Unknown || value2.Kind == GlobalFlowStateAnalysisValueSetKind.Unknown)
                {
                    return GlobalFlowStateAnalysisValueSet.Unknown;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Empty)
                {
                    return value2;
                }
                else if (value2.Kind == GlobalFlowStateAnalysisValueSetKind.Empty)
                {
                    return value1;
                }

                Debug.Assert(value1.Kind == GlobalFlowStateAnalysisValueSetKind.Known);
                Debug.Assert(value2.Kind == GlobalFlowStateAnalysisValueSetKind.Known);

                return new GlobalFlowStateAnalysisValueSet(value1, value2);
            }

            public static GlobalFlowStateAnalysisValueSet Intersect(GlobalFlowStateAnalysisValueSet value1, GlobalFlowStateAnalysisValueSet value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Unset)
                {
                    return value2;
                }
                else if (value2.Kind == GlobalFlowStateAnalysisValueSetKind.Unset)
                {
                    return value1;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Unknown || value2.Kind == GlobalFlowStateAnalysisValueSetKind.Unknown)
                {
                    return GlobalFlowStateAnalysisValueSet.Unknown;
                }
                else if (value1.Kind == GlobalFlowStateAnalysisValueSetKind.Empty || value2.Kind == GlobalFlowStateAnalysisValueSetKind.Empty)
                {
                    return GlobalFlowStateAnalysisValueSet.Empty;
                }
                else if (value1 == value2)
                {
                    return value1;
                }

                Debug.Assert(value1.Kind == GlobalFlowStateAnalysisValueSetKind.Known);
                Debug.Assert(value2.Kind == GlobalFlowStateAnalysisValueSetKind.Known);

                if (value1.Height == 0 && value2.Height == 0)
                {
                    return Intersect(value1, value2);
                }

                var currentNodes = new Queue<GlobalFlowStateAnalysisValueSet>();
                using var candidateNodes = PooledHashSet<GlobalFlowStateAnalysisValueSet>.GetInstance();
                int candidateHeight = 0;
                if (value1.Height <= value2.Height)
                {
                    candidateNodes.Add(value1);
                    currentNodes.Enqueue(value2);
                    candidateHeight = value1.Height;
                }

                if (value2.Height <= value1.Height)
                {
                    candidateNodes.Add(value2);
                    currentNodes.Enqueue(value1);
                    candidateHeight = value2.Height;
                }

                while (currentNodes.Count > 0)
                {
                    var node = currentNodes.Dequeue();
                    foreach (var parent in node.Parents)
                    {
                        if (candidateNodes.Contains(parent))
                        {
                            continue;
                        }

                        if (parent.Height > candidateHeight)
                        {
                            currentNodes.Enqueue(parent);
                            continue;
                        }

                        foreach (var candidate in candidateNodes)
                        {
                            currentNodes.Enqueue(candidate);
                        }

                        if (parent.Height < candidateHeight)
                        {
                            candidateNodes.Clear();
                        }

                        candidateNodes.Add(parent);
                        candidateHeight = parent.Height;
                    }
                }

                if (candidateNodes.Count == 1)
                {
                    return candidateNodes.Single();
                }

                GlobalFlowStateAnalysisValueSet? result = null;
                foreach (var candidate in candidateNodes)
                {
                    if (result == null)
                    {
                        result = candidate;
                    }
                    else if (!TryIntersect(candidate, result, out result))
                    {
                        return GlobalFlowStateAnalysisValueSet.Empty;
                    }
                }

                return result!;

                static GlobalFlowStateAnalysisValueSet Intersect(GlobalFlowStateAnalysisValueSet value1, GlobalFlowStateAnalysisValueSet value2)
                {
                    _ = TryIntersect(value1, value2, out var result);
                    return result;
                }

                static bool TryIntersect(GlobalFlowStateAnalysisValueSet value1, GlobalFlowStateAnalysisValueSet value2, out GlobalFlowStateAnalysisValueSet result)
                {
                    Debug.Assert(value1.Height == value2.Height);
                    var sets = value1.AnalysisValues.IntersectSet(value2.AnalysisValues);
                    if (sets.IsEmpty)
                    {
                        result = GlobalFlowStateAnalysisValueSet.Empty;
                        return false;
                    }

                    if (sets.Count == value1.AnalysisValues.Count)
                    {
                        result = value1;
                    }
                    else if (sets.Count == value2.AnalysisValues.Count)
                    {
                        result = value2;
                    }
                    else
                    {
                        result = new GlobalFlowStateAnalysisValueSet(sets, value1.Parents, value1.Height, GlobalFlowStateAnalysisValueSetKind.Known);
                    }

                    return true;
                }
            }
        }
    }
}
