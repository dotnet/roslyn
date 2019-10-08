// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

    internal partial class TaintedDataAnalysis
    {
        private sealed class TaintedDataAnalysisDomain : PredicatedAnalysisDataDomain<TaintedDataAnalysisData, TaintedDataAbstractValue>
        {
            public TaintedDataAnalysisDomain(MapAbstractDomain<AnalysisEntity, TaintedDataAbstractValue> coreDataAnalysisDomain)
                : base(coreDataAnalysisDomain)
            {
            }
        }
    }
}