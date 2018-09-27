// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using CoreBinaryFormatterAnalysisData = IDictionary<AnalysisEntity, BinaryFormatterAbstractValue>;


    internal sealed class BinaryFormatterAnalysisData : AnalysisEntityBasedPredicateAnalysisData<BinaryFormatterAbstractValue>
    {
        public BinaryFormatterAnalysisData()
            : base()
        {
        }

        public BinaryFormatterAnalysisData(CoreBinaryFormatterAnalysisData fromData)
            : base(fromData)
        {
        }

        public BinaryFormatterAnalysisData(BinaryFormatterAnalysisData fromData)
            : base(fromData)
        {
        }

        public BinaryFormatterAnalysisData(BinaryFormatterAnalysisData fromData, BinaryFormatterAnalysisData data, MapAbstractDomain<AnalysisEntity, BinaryFormatterAbstractValue> coreDataAnalysisDomain)
            : base(fromData, data, coreDataAnalysisDomain)
        {
        }

        public override AnalysisEntityBasedPredicateAnalysisData<BinaryFormatterAbstractValue> Clone()
        {
            return new BinaryFormatterAnalysisData(this);
        }

        public override int Compare(AnalysisEntityBasedPredicateAnalysisData<BinaryFormatterAbstractValue> other, MapAbstractDomain<AnalysisEntity, BinaryFormatterAbstractValue> coreDataAnalysisDomain)
        {
            return this.BaseCompareHelper(other, coreDataAnalysisDomain);
        }

        public override AnalysisEntityBasedPredicateAnalysisData<BinaryFormatterAbstractValue> WithMergedData(AnalysisEntityBasedPredicateAnalysisData<BinaryFormatterAbstractValue> data, MapAbstractDomain<AnalysisEntity, BinaryFormatterAbstractValue> coreDataAnalysisDomain)
        {
            return new BinaryFormatterAnalysisData(this, (BinaryFormatterAnalysisData)data, coreDataAnalysisDomain);
        }
    }
}
