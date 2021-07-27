// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisData : AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue>
    {
        private InvocationCountAnalysisData(
            AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> data1,
            AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> data2,
            MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue> coreDataAnalysisDomain) : base(
                data1,
                data2,
                coreDataAnalysisDomain)
        {
        }

        public InvocationCountAnalysisData(InvocationCountAnalysisData fromData) : base(fromData)
        {
        }

        public InvocationCountAnalysisData(ImmutableDictionary<AnalysisEntity, InvocationCountAbstractValue> fromData) : base(fromData)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> Clone()
            => new InvocationCountAnalysisData(this);

        public override AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> WithMergedData(
            AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> data,
            MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue> coreDataAnalysisDomain)
            => new InvocationCountAnalysisData(this, data, coreDataAnalysisDomain);

        public override int Compare(
            AnalysisEntityBasedPredicateAnalysisData<InvocationCountAbstractValue> other,
            MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue> coreDataAnalysisDomain)
            => BaseCompareHelper(other, coreDataAnalysisDomain);
    }
}