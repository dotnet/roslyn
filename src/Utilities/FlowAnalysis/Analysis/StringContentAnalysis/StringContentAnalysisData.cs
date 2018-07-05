// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    using CoreStringContentAnalysisData = IDictionary<AnalysisEntity, StringContentAbstractValue>;

    /// <summary>
    /// Aggregated string content analysis data tracked by <see cref="StringContentAnalysis"/>.
    /// Contains the <see cref="CoreStringContentAnalysisData"/> for entity string content values and
    /// the predicated values based on true/false runtime values of predicated entities.
    /// </summary>
    /// <summary>
    internal sealed class StringContentAnalysisData : AnalysisEntityBasedPredicateAnalysisData<StringContentAbstractValue>
    {
        public StringContentAnalysisData()
        {
        }

        private StringContentAnalysisData(StringContentAnalysisData fromData)
            : base(fromData)
        {
        }

        private StringContentAnalysisData(StringContentAnalysisData data1, StringContentAnalysisData data2, MapAbstractDomain<AnalysisEntity, StringContentAbstractValue> coreDataAnalysisDomain)
            : base(data1, data2, coreDataAnalysisDomain)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<StringContentAbstractValue> Clone() => new StringContentAnalysisData(this);

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<StringContentAbstractValue> other, MapAbstractDomain<AnalysisEntity, StringContentAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);

        public override AnalysisEntityBasedPredicateAnalysisData<StringContentAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<StringContentAbstractValue> data, MapAbstractDomain<AnalysisEntity, StringContentAbstractValue> coreDataAnalysisDomain)
            => new StringContentAnalysisData(this, (StringContentAnalysisData)data, coreDataAnalysisDomain);
    }
}
