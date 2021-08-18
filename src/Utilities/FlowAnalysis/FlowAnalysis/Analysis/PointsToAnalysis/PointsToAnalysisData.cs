// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis
{
    using CorePointsToAnalysisData = DictionaryAnalysisData<AnalysisEntity, PointsToAbstractValue>;

    /// <summary>
    /// Aggregated PointsTo analysis data tracked by <see cref="PointsToAnalysis"/>.
    /// Contains the <see cref="CorePointsToAnalysisData"/> for entity PointsTo values and
    /// the predicated values based on true/false runtime values of predicated entities.
    /// </summary>
    public sealed class PointsToAnalysisData : AnalysisEntityBasedPredicateAnalysisData<PointsToAbstractValue>
    {
        internal PointsToAnalysisData()
        {
        }

        internal PointsToAnalysisData(IDictionary<AnalysisEntity, PointsToAbstractValue> fromData)
            : base(fromData)
        {
            AssertValidPointsToAnalysisData(fromData);
        }

        internal PointsToAnalysisData(
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
            AssertValidPointsToAnalysisKeyValuePair(key, value);
            base.SetAbstractValue(key, value);
        }

        public override void Reset(Func<AnalysisEntity, PointsToAbstractValue, PointsToAbstractValue> getResetValue)
        {
            base.Reset(getResetValue);
            AssertValidPointsToAnalysisData();
        }

        [Conditional("DEBUG")]
        internal void AssertNoFlowCaptureEntitiesTracked()
        {
            AssertNoFlowCaptureEntitiesTracked(CoreAnalysisData);
            AssertValidPredicatedAnalysisData(map => AssertNoFlowCaptureEntitiesTracked(map));

        }

        [Conditional("DEBUG")]
        private static void AssertNoFlowCaptureEntitiesTracked(CorePointsToAnalysisData map)
        {
            foreach (var key in map.Keys)
            {
                Debug.Assert(key.CaptureId == null);
            }
        }

        [Conditional("DEBUG")]
        internal void AssertValidPointsToAnalysisData()
        {
            AssertValidPointsToAnalysisData(CoreAnalysisData);
            AssertValidPredicatedAnalysisData(map => AssertValidPointsToAnalysisData(map));
        }

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisData(IDictionary<AnalysisEntity, PointsToAbstractValue> map)
        {
            if (map is CorePointsToAnalysisData corePointsToAnalysisData)
            {
                Debug.Assert(!corePointsToAnalysisData.IsDisposed);
            }

            foreach (var kvp in map)
            {
                AssertValidPointsToAnalysisKeyValuePair(kvp.Key, kvp.Value);
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisKeyValuePair(
            AnalysisEntity key,
            PointsToAbstractValue value)
        {
            Debug.Assert(value.Kind != PointsToAbstractValueKind.Undefined);
            Debug.Assert(!key.IsLValueFlowCaptureEntity || value.Kind == PointsToAbstractValueKind.KnownLValueCaptures);
            Debug.Assert(PointsToAnalysis.ShouldBeTracked(key, PointsToAnalysisKind.Complete));
        }
    }
}
