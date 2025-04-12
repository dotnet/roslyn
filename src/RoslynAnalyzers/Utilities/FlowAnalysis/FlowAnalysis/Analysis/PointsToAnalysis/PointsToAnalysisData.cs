// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly Func<ITypeSymbol?, bool> _isDisposable;

        internal PointsToAnalysisData(Func<ITypeSymbol?, bool> isDisposable)
        {
            _isDisposable = isDisposable;
        }

        internal PointsToAnalysisData(IDictionary<AnalysisEntity, PointsToAbstractValue> fromData, Func<ITypeSymbol?, bool> isDisposable)
            : base(fromData)
        {
            _isDisposable = isDisposable;

            AssertValidPointsToAnalysisData(fromData, isDisposable);
        }

        internal PointsToAnalysisData(
            CorePointsToAnalysisData mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, PointsToAbstractValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain,
            Func<ITypeSymbol?, bool> isDisposable)
            : base(mergedCoreAnalysisData, predicatedData1, predicatedData2, isReachableData, coreDataAnalysisDomain)
        {
            _isDisposable = isDisposable;

            AssertValidPointsToAnalysisData(mergedCoreAnalysisData, isDisposable);
            AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData fromData)
            : base(fromData)
        {
            _isDisposable = fromData._isDisposable;

            fromData.AssertValidPointsToAnalysisData();
        }

        private PointsToAnalysisData(PointsToAnalysisData data1, PointsToAnalysisData data2, MapAbstractDomain<AnalysisEntity, PointsToAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
            _isDisposable = data1._isDisposable;

            data1.AssertValidPointsToAnalysisData();
            data2.AssertValidPointsToAnalysisData();
            AssertValidPointsToAnalysisData();
        }

        protected override AbstractValueDomain<PointsToAbstractValue> ValueDomain => PointsToAnalysis.ValueDomainInstance;
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
            AssertValidPointsToAnalysisKeyValuePair(key, value, _isDisposable);
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
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - https://github.com/dotnet/roslyn/issues/63464
            AssertValidPredicatedAnalysisData(map => AssertNoFlowCaptureEntitiesTracked(map));
#pragma warning restore IDE0200 // Remove unnecessary lambda expression
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
            AssertValidPointsToAnalysisData(CoreAnalysisData, _isDisposable);
#pragma warning disable IDE0200 // Remove unnecessary lambda expression - https://github.com/dotnet/roslyn/issues/63464
            AssertValidPredicatedAnalysisData(map => AssertValidPointsToAnalysisData(map, _isDisposable));
#pragma warning restore IDE0200 // Remove unnecessary lambda expression
        }

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisData(IDictionary<AnalysisEntity, PointsToAbstractValue> map, Func<ITypeSymbol?, bool> isDisposable)
        {
            if (map is CorePointsToAnalysisData corePointsToAnalysisData)
            {
                Debug.Assert(!corePointsToAnalysisData.IsDisposed);
            }

            foreach (var kvp in map)
            {
                AssertValidPointsToAnalysisKeyValuePair(kvp.Key, kvp.Value, isDisposable);
            }
        }

        [Conditional("DEBUG")]
        internal static void AssertValidPointsToAnalysisKeyValuePair(
            AnalysisEntity key,
            PointsToAbstractValue value,
            Func<ITypeSymbol?, bool> isDisposable)
        {
            Debug.Assert(value.Kind != PointsToAbstractValueKind.Undefined);
            Debug.Assert(!key.IsLValueFlowCaptureEntity || value.Kind == PointsToAbstractValueKind.KnownLValueCaptures);
            Debug.Assert(PointsToAnalysis.ShouldBeTracked(key, PointsToAnalysisKind.Complete, isDisposable));
        }
    }
}
