// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CorePointsToAnalysisData = IDictionary<AnalysisEntity, PointsToAbstractValue>;

    /// <summary>
    /// Aggregated PointsTo analysis data tracked by <see cref="PointsToAnalysis"/>.
    /// Contains the <see cref="CorePointsToAnalysisData"/> for entity PointsTo values and
    /// the predicated values based on true/false runtime values of predicated entities.
    /// </summary>
    internal sealed class PointsToAnalysisData : AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue>
    {
        public PointsToAnalysisData()
        {
        }

        public PointsToAnalysisData(CorePointsToAnalysisData fromData)
            : base(fromData)
        {
            AssertValidPointsToAnalysisData(fromData);
        }

        public PointsToAnalysisData(
            CorePointsToAnalysisData mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
            : base(mergedCoreAnalysisData, predicatedData1, predicatedData2, isReachableData, coreDataAnalysisDomain)
        {
            AssertValidPointsToAnalysisData(mergedCoreAnalysisData);
            AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData fromData)
            : base(fromData)
        {
            fromData.AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData data1, PointsToAnalysisData data2, MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
            data1.AssertValidPointsToAnalysisData();
            data2.AssertValidPointsToAnalysisData();
            AssertValidPointsToAnalysisData();
        }

        public override AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue> Clone() => new PointsToAnalysisData(this);

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue> other, MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);

        public override AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue> data, MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
        {
            Debug.Assert(IsReachableBlockData || !data.IsReachableBlockData);
            var mergedData = new PointsToAnalysisData(this, (PointsToAnalysisData)data, coreDataAnalysisDomain);
            mergedData.AssertValidPointsToAnalysisData();
            return mergedData;
        }

        public override void SetAbstractValue(AnalysisEntity key, PointsToAbstractValue value)
        {
            throw new NotImplementedException($"Use the supported overload of {nameof(SetAbstractValue)}");
        }

        public void SetAbstractValue(
            AnalysisEntity key,
            PointsToAbstractValue value,
            AbstractValueDomain<PointsToAbstractValue> valueDomain,
            Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
        {
            Debug.Assert(value.Kind != PointsToAbstractValueKind.Undefined);
            Debug.Assert(!isLValueFlowCaptureEntity(key) || value.Kind == PointsToAbstractValueKind.KnownLValueCaptures);

            // PointsToAbstractValueKind.Unknown value might point to some known locations.
            // If we are setting to PointsToAbstractValue.Unknown, 
            // ensure we don't lose those these locations, as that would a non-monotonic operation.
            if (value == PointsToAbstractValue.Unknown &&
                TryGetValue(key, out var currentValue) &&
                currentValue.Kind == PointsToAbstractValueKind.Unknown &&
                currentValue != PointsToAbstractValue.Unknown)
            {
                value = valueDomain.Merge(value, currentValue);
            }

            base.SetAbstractValue(key, value);
        }

        public override void Reset(Func<AnalysisEntity, PointsToAbstractValue, PointsToAbstractValue> getResetValue)
        {
            base.Reset(getResetValue);
            AssertValidPointsToAnalysisData();
        }

        [Conditional("DEBUG")]
        public void AssertValidPointsToAnalysisData()
        {
            AssertValidPointsToAnalysisData(CoreAnalysisData);
            AssertValidPredicatedAnalysisData(map => AssertValidPointsToAnalysisData(map));
        }

        [Conditional("DEBUG")]
        public static void AssertValidPointsToAnalysisData(CorePointsToAnalysisData map)
        {
            foreach (var value in map.Values)
            {
                Debug.Assert(value.Kind != PointsToAbstractValueKind.Undefined);
            }
        }
    }
}
