// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountAnalysisDomain : PredicatedAnalysisDataDomain<InvocationCountAnalysisData, InvocationCountAbstractValue>
    {
        public InvocationCountAnalysisDomain(MapAbstractDomain<AnalysisEntity, InvocationCountAbstractValue> coreDataAnalysisDomain)
            : base(coreDataAnalysisDomain)
        {
        }
    }
}