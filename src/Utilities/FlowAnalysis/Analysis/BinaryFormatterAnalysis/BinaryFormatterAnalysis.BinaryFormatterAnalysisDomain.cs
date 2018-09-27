// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    using System;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
    using Microsoft.CodeAnalysis.Operations;

    internal partial class BinaryFormatterAnalysis
    {
        private sealed class BinaryFormatterAnalysisDomain : PredicatedAnalysisDataDomain<BinaryFormatterAnalysisData, BinaryFormatterAbstractValue>
        {
            public BinaryFormatterAnalysisDomain(MapAbstractDomain<AnalysisEntity, BinaryFormatterAbstractValue> coreDataAnalysisDomain)
                : base(coreDataAnalysisDomain)
            {
            }
        }
    }
}