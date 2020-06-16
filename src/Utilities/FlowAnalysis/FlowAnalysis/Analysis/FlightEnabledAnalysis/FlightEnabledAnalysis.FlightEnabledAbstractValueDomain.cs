// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.FlightEnabledAnalysis
{
    using FlightEnabledAnalysisData = DictionaryAnalysisData<AnalysisEntity, FlightEnabledAbstractValue>;
    using FlightEnabledAnalysisResult = DataFlowAnalysisResult<FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>;

    internal partial class FlightEnabledAnalysis : ForwardDataFlowAnalysis<FlightEnabledAnalysisData, FlightEnabledAnalysisContext, FlightEnabledAnalysisResult, FlightEnabledBlockAnalysisResult, FlightEnabledAbstractValue>
    {
        private class FlightEnabledAbstractValueDomain : AbstractValueDomain<FlightEnabledAbstractValue>
        {
            public static FlightEnabledAbstractValueDomain Instance = new FlightEnabledAbstractValueDomain();

            private FlightEnabledAbstractValueDomain() { }

            public override FlightEnabledAbstractValue Bottom => FlightEnabledAbstractValue.Unset;

            public override FlightEnabledAbstractValue UnknownOrMayBeValue => FlightEnabledAbstractValue.Unknown;

            public override int Compare(FlightEnabledAbstractValue oldValue, FlightEnabledAbstractValue newValue, bool assertMonotonicity)
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

            public override FlightEnabledAbstractValue Merge(FlightEnabledAbstractValue value1, FlightEnabledAbstractValue value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Unset)
                {
                    return value2;
                }
                else if (value2.Kind == FlightEnabledAbstractValueKind.Unset)
                {
                    return value1;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Unknown || value2.Kind == FlightEnabledAbstractValueKind.Unknown)
                {
                    return FlightEnabledAbstractValue.Unknown;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Empty)
                {
                    return value2;
                }
                else if (value2.Kind == FlightEnabledAbstractValueKind.Empty)
                {
                    return value1;
                }

                Debug.Assert(value1.Kind == FlightEnabledAbstractValueKind.Known);
                Debug.Assert(value2.Kind == FlightEnabledAbstractValueKind.Known);

                return new FlightEnabledAbstractValue(value1, value2);
            }

            public static FlightEnabledAbstractValue Intersect(FlightEnabledAbstractValue value1, FlightEnabledAbstractValue value2)
            {
                if (value1 == null)
                {
                    return value2;
                }
                else if (value2 == null)
                {
                    return value1;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Unset)
                {
                    return value2;
                }
                else if (value2.Kind == FlightEnabledAbstractValueKind.Unset)
                {
                    return value1;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Unknown || value2.Kind == FlightEnabledAbstractValueKind.Unknown)
                {
                    return FlightEnabledAbstractValue.Unknown;
                }
                else if (value1.Kind == FlightEnabledAbstractValueKind.Empty || value2.Kind == FlightEnabledAbstractValueKind.Empty)
                {
                    return FlightEnabledAbstractValue.Empty;
                }
                else if (value1 == value2)
                {
                    return value1;
                }

                Debug.Assert(value1.Kind == FlightEnabledAbstractValueKind.Known);
                Debug.Assert(value2.Kind == FlightEnabledAbstractValueKind.Known);

                if (value1.Height == 0 && value2.Height == 0)
                {
                    return Intersect(value1, value2);
                }

                var currentNodes = new Queue<FlightEnabledAbstractValue>();
                using var candidateNodes = PooledHashSet<FlightEnabledAbstractValue>.GetInstance();
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

                FlightEnabledAbstractValue? result = null;
                foreach (var candidate in candidateNodes)
                {
                    if (result == null)
                    {
                        result = candidate;
                    }
                    else if (!TryIntersect(candidate, result, out result))
                    {
                        return FlightEnabledAbstractValue.Empty;
                    }
                }

                return result!;

                static FlightEnabledAbstractValue Intersect(FlightEnabledAbstractValue value1, FlightEnabledAbstractValue value2)
                {
                    _ = TryIntersect(value1, value2, out var result);
                    return result;
                }

                static bool TryIntersect(FlightEnabledAbstractValue value1, FlightEnabledAbstractValue value2, out FlightEnabledAbstractValue result)
                {
                    Debug.Assert(value1.Height == value2.Height);
                    var sets = value1.EnabledFlights.IntersectSet(value2.EnabledFlights);
                    if (sets.IsEmpty)
                    {
                        result = FlightEnabledAbstractValue.Empty;
                        return false;
                    }

                    if (sets.Count == value1.EnabledFlights.Count)
                    {
                        result = value1;
                    }
                    else if (sets.Count == value2.EnabledFlights.Count)
                    {
                        result = value2;
                    }
                    else
                    {
                        result = new FlightEnabledAbstractValue(sets, value1.Parents, value1.Height, FlightEnabledAbstractValueKind.Known);
                    }

                    return true;
                }
            }
        }
    }
}
