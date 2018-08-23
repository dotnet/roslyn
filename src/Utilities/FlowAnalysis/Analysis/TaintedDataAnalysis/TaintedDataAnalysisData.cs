// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    internal sealed class TaintedDataAnalysisData : AnalysisEntityBasedPredicateAnalysisData<TaintedDataAbstractValue>
    {
        public TaintedDataAnalysisData()
            : base()
        {
        }

        public TaintedDataAnalysisData(TaintedDataAnalysisData fromData)
            : base(fromData)
        {
        }

        public TaintedDataAnalysisData(TaintedDataAnalysisData fromData, TaintedDataAnalysisData data, MapAbstractDomain<AnalysisEntity, TaintedDataAbstractValue> coreDataAnalysisDomain)
            : base(fromData, data, coreDataAnalysisDomain)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<TaintedDataAbstractValue> Clone()
        {
            return new TaintedDataAnalysisData(this);
        }

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<TaintedDataAbstractValue> other, MapAbstractDomain<AnalysisEntity, TaintedDataAbstractValue> coreDataAnalysisDomain)
        {
            return this.BaseCompareHelper(other, coreDataAnalysisDomain);
        }

        public override AnalysisEntityBasedPredicateAnalysisData<TaintedDataAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<TaintedDataAbstractValue> data, MapAbstractDomain<AnalysisEntity, TaintedDataAbstractValue> coreDataAnalysisDomain)
        {
            return new TaintedDataAnalysisData(this, (TaintedDataAnalysisData )data, coreDataAnalysisDomain);
        }
    }
}
