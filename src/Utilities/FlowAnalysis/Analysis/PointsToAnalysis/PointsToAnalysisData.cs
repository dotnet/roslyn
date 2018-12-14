// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Analyzer.Utilities.Extensions;

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
        public PointsToAnalysisData(Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
        {
            IsLValueFlowCaptureEntity = isLValueFlowCaptureEntity;
        }

        public PointsToAnalysisData(CorePointsToAnalysisData fromData, Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
            : base(fromData)
        {
            IsLValueFlowCaptureEntity = isLValueFlowCaptureEntity;

            AssertValidPointsToAnalysisData(fromData, ShouldBeTracked, isLValueFlowCaptureEntity);
        }

        public PointsToAnalysisData(
            CorePointsToAnalysisData mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain,
            Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
            : base(mergedCoreAnalysisData, predicatedData1, predicatedData2, isReachableData, coreDataAnalysisDomain)
        {
            IsLValueFlowCaptureEntity = isLValueFlowCaptureEntity;

            AssertValidPointsToAnalysisData(mergedCoreAnalysisData, ShouldBeTracked, isLValueFlowCaptureEntity);
            AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData fromData)
            : base(fromData)
        {
            IsLValueFlowCaptureEntity = fromData.IsLValueFlowCaptureEntity;

            fromData.AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData data1, PointsToAnalysisData data2, MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
            IsLValueFlowCaptureEntity = data1.IsLValueFlowCaptureEntity;

            data1.AssertValidPointsToAnalysisData();
            data2.AssertValidPointsToAnalysisData();
            AssertValidPointsToAnalysisData();
        }

        public Func<AnalysisEntity, bool> IsLValueFlowCaptureEntity { get; }

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

        public static bool ShouldBeTracked(ITypeSymbol typeSymbol) => typeSymbol.IsReferenceTypeOrNullableValueType();

        public bool ShouldBeTracked(AnalysisEntity analysisEntity)
            => ShouldBeTracked(analysisEntity.Type) || IsLValueFlowCaptureEntity(analysisEntity) || analysisEntity.IsThisOrMeInstance;

        public override void SetAbstractValue(AnalysisEntity key, PointsToAbstractValue value)
        {
            AssertValidPointsToAnalysisKeyValuePair(key, value, ShouldBeTracked, IsLValueFlowCaptureEntity);
            base.SetAbstractValue(key, value);
        }

        public override void Reset(Func<AnalysisEntity, PointsToAbstractValue, PointsToAbstractValue> getResetValue)
        {
            base.Reset(getResetValue);
            AssertValidPointsToAnalysisData();
        }

        [Conditional("DEBUG")]
        public void AssertNoFlowCaptureEntitiesTracked()
        {
            AssertNoFlowCaptureEntitiesTracked(CoreAnalysisData);
            AssertValidPredicatedAnalysisData(map => AssertNoFlowCaptureEntitiesTracked(map));

        }

        [Conditional("DEBUG")]
        private static void AssertNoFlowCaptureEntitiesTracked(CorePointsToAnalysisData map)
        {
            foreach (var key in map.Keys)
            {
                Debug.Assert(key.CaptureIdOpt == null);
            }
        }

        [Conditional("DEBUG")]
        public void AssertValidPointsToAnalysisData()
        {
            AssertValidPointsToAnalysisData(CoreAnalysisData, ShouldBeTracked, IsLValueFlowCaptureEntity);
            AssertValidPredicatedAnalysisData(map => AssertValidPointsToAnalysisData(map, ShouldBeTracked, IsLValueFlowCaptureEntity));
        }

        [Conditional("DEBUG")]
        private static void AssertValidPointsToAnalysisData(
            CorePointsToAnalysisData map,
            Func<AnalysisEntity, bool> shouldBeTracked,
            Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
        {
            foreach (var kvp in map)
            {
                AssertValidPointsToAnalysisKeyValuePair(kvp.Key, kvp.Value, shouldBeTracked, isLValueFlowCaptureEntity);
            }
        }

        [Conditional("DEBUG")]
        private static void AssertValidPointsToAnalysisKeyValuePair(
            AnalysisEntity key,
            PointsToAbstractValue value,
            Func<AnalysisEntity, bool> shouldBeTracked,
            Func<AnalysisEntity, bool> isLValueFlowCaptureEntity)
        {
            Debug.Assert(value.Kind != PointsToAbstractValueKind.Undefined);
            Debug.Assert(!isLValueFlowCaptureEntity(key) || value.Kind == PointsToAbstractValueKind.KnownLValueCaptures);
            Debug.Assert(shouldBeTracked(key));
        }
    }
}
