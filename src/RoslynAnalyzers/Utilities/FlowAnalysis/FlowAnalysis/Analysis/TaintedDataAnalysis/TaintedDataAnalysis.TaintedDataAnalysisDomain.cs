// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
