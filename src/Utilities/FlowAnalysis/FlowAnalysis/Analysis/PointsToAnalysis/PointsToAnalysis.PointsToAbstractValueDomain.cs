// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    public partial class PointsToAnalysis : ForwardDataFlowAnalysis<PointsToAnalysisData, PointsToAnalysisContext, PointsToAnalysisResult, PointsToBlockAnalysisResult, PointsToAbstractValue>
    {
        /// <summary>
        /// Abstract value domain for <see cref="PointsToAnalysis"/> to merge and compare <see cref="PointsToAbstractValue"/> values.
        /// </summary>
        private class PointsToAbstractValueDomain : AbstractValueDomain<PointsToAbstractValue>
        {
            public static PointsToAbstractValueDomain Default = new PointsToAbstractValueDomain();
            private readonly SetAbstractDomain<AbstractLocation> _locationsDomain = SetAbstractDomain<AbstractLocation>.Default;
            private readonly SetAbstractDomain<IOperation> _lValueCapturesDomain = SetAbstractDomain<IOperation>.Default;

            private PointsToAbstractValueDomain() { }

            public override PointsToAbstractValue Bottom => PointsToAbstractValue.Undefined;

            public override PointsToAbstractValue UnknownOrMayBeValue => PointsToAbstractValue.Unknown;

            public override int Compare(PointsToAbstractValue oldValue, PointsToAbstractValue newValue, bool assertMonotonicity)
            {
                if (ReferenceEquals(oldValue, newValue))
                {
                    return 0;
                }

                if (oldValue.Kind == newValue.Kind)
                {
                    int locationsCompareResult = _locationsDomain.Compare(oldValue.Locations, newValue.Locations);
                    int lValueCapturesCompareResult = _lValueCapturesDomain.Compare(oldValue.LValueCapturedOperations, newValue.LValueCapturedOperations);
                    var nullCompareResult = NullAbstractValueDomain.Default.Compare(oldValue.NullState, newValue.NullState);
                    if (locationsCompareResult > 0 || lValueCapturesCompareResult > 0 || nullCompareResult > 0)
                    {
                        FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                        return 1;
                    }
                    else if (locationsCompareResult < 0 || lValueCapturesCompareResult < 0 || nullCompareResult < 0)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else if (oldValue.Kind < newValue.Kind)
                {
#if DEBUG
                    if (NullAbstractValueDomain.Default.Compare(oldValue.NullState, newValue.NullState) > 0)
                    {
                        FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    }
#endif
                    return -1;
                }
                else
                {
                    FireNonMonotonicAssertIfNeeded(assertMonotonicity);
                    return 1;
                }
            }

            public override PointsToAbstractValue Merge(PointsToAbstractValue value1, PointsToAbstractValue value2)
            {
                PointsToAbstractValue result;
                if (value1 == value2)
                {
                    result = value1;
                }
                else if (value1.Kind == PointsToAbstractValueKind.Invalid)
                {
                    result = value2.Kind == PointsToAbstractValueKind.Undefined ?
                        PointsToAbstractValue.Unknown :
                        value2;
                }
                else if (value2.Kind == PointsToAbstractValueKind.Invalid)
                {
                    result = value1.Kind == PointsToAbstractValueKind.Undefined ?
                        PointsToAbstractValue.Unknown :
                        value1;
                }
                else if (value1.Kind == PointsToAbstractValueKind.Undefined)
                {
                    result = value2;
                }
                else if (value2.Kind == PointsToAbstractValueKind.Undefined)
                {
                    result = value1;
                }
                else if (value1.Kind == PointsToAbstractValueKind.Unknown ||
                         value2.Kind == PointsToAbstractValueKind.Unknown)
                {
                    result = PointsToAbstractValue.Unknown;
                }
                else if (value1.Kind == PointsToAbstractValueKind.UnknownNull)
                {
                    return value2.NullState == NullAbstractValue.Null ?
                        PointsToAbstractValue.UnknownNull :
                        PointsToAbstractValue.Unknown;
                }
                else if (value2.Kind == PointsToAbstractValueKind.UnknownNull)
                {
                    return value1.NullState == NullAbstractValue.Null ?
                        PointsToAbstractValue.UnknownNull :
                        PointsToAbstractValue.Unknown;
                }
                else if (value1.Kind == PointsToAbstractValueKind.UnknownNotNull)
                {
                    return value2.NullState == NullAbstractValue.NotNull ?
                        PointsToAbstractValue.UnknownNotNull :
                        PointsToAbstractValue.Unknown;
                }
                else if (value2.Kind == PointsToAbstractValueKind.UnknownNotNull)
                {
                    return value1.NullState == NullAbstractValue.NotNull ?
                        PointsToAbstractValue.UnknownNotNull :
                        PointsToAbstractValue.Unknown;
                }
                else if (value1.Kind == PointsToAbstractValueKind.KnownLValueCaptures)
                {
                    Debug.Assert(value2.Kind == PointsToAbstractValueKind.KnownLValueCaptures);
                    var mergedLValueCaptures = _lValueCapturesDomain.Merge(value1.LValueCapturedOperations, value2.LValueCapturedOperations);
                    result = PointsToAbstractValue.Create(mergedLValueCaptures);
                }
                else
                {
                    Debug.Assert(value1.Kind == PointsToAbstractValueKind.KnownLocations);
                    Debug.Assert(value2.Kind == PointsToAbstractValueKind.KnownLocations);

                    var mergedLocations = _locationsDomain.Merge(value1.Locations, value2.Locations);
                    var mergedNullState = NullAbstractValueDomain.Default.Merge(value1.NullState, value2.NullState);
                    result = PointsToAbstractValue.Create(mergedLocations, mergedNullState);
                }

                Debug.Assert(Compare(value1, result) <= 0);
                Debug.Assert(Compare(value2, result) <= 0);

                return result;
            }
        }
    }
}
