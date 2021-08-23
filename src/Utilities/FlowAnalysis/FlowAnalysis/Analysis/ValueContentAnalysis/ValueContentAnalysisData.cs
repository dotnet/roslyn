// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using CoreValueContentAnalysisData = DictionaryAnalysisData<AnalysisEntity, ValueContentAbstractValue>;

    /// <summary>
    /// Aggregated value content analysis data tracked by <see cref="ValueContentAnalysis"/>.
    /// Contains the <see cref="CoreValueContentAnalysisData"/> for entity data values and
    /// the predicated values based on true/false runtime values of predicated entities.
    /// </summary>
    public sealed class ValueContentAnalysisData : AnalysisEntityBasedPredicateAnalysisData<ValueContentAbstractValue>
    {
        internal ValueContentAnalysisData()
        {
        }

        internal ValueContentAnalysisData(IDictionary<AnalysisEntity, ValueContentAbstractValue> fromData)
            : base(fromData)
        {
        }

        private ValueContentAnalysisData(ValueContentAnalysisData fromData)
            : base(fromData)
        {
        }

        private ValueContentAnalysisData(ValueContentAnalysisData data1, ValueContentAnalysisData data2, MapAbstractDomain<AnalysisEntity, ValueContentAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
        }

        internal ValueContentAnalysisData(
            CoreValueContentAnalysisData mergedCoreAnalysisData,
            PredicatedAnalysisData<AnalysisEntity, ValueContentAbstractValue> predicatedData1,
            PredicatedAnalysisData<AnalysisEntity, ValueContentAbstractValue> predicatedData2,
            bool isReachableData,
            MapAbstractDomain<AnalysisEntity, ValueContentAbstractValue> coreDataAnalysisDomain)
            : base(mergedCoreAnalysisData, predicatedData1, predicatedData2, isReachableData, coreDataAnalysisDomain)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<ValueContentAbstractValue> Clone() => new ValueContentAnalysisData(this);

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<ValueContentAbstractValue> other, MapAbstractDomain<AnalysisEntity, ValueContentAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);

        public override AnalysisEntityBasedPredicateAnalysisData<ValueContentAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<ValueContentAbstractValue> data, MapAbstractDomain<AnalysisEntity, ValueContentAbstractValue> coreDataAnalysisDomain)
            => new ValueContentAnalysisData(this, (ValueContentAnalysisData)data, coreDataAnalysisDomain);

        internal void Reset(ValueContentAbstractValue resetValue)
        {
            base.Reset((analysisEntity, currentValue) => resetValue);
        }
    }
}
