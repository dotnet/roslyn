// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    internal partial class TaintedDataAnalysis
    {
        private sealed class CoreTaintedDataAnalysisDataDomain : AnalysisEntityMapAbstractDomain<TaintedDataAbstractValue>
        {
            public CoreTaintedDataAnalysisDataDomain(PointsToAnalysisResult? pointsToAnalysisResult)
                : base(TaintedDataAbstractValueDomain.Default, pointsToAnalysisResult)
            {
            }

            protected override bool CanSkipNewEntry(AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
            {
                return value.Kind == TaintedDataAbstractValueKind.NotTainted;
            }

            protected override TaintedDataAbstractValue GetDefaultValue(AnalysisEntity analysisEntity)
            {
                return TaintedDataAbstractValue.NotTainted;
            }

            protected override void AssertValidEntryForMergedMap(AnalysisEntity analysisEntity, TaintedDataAbstractValue value)
            {
            }
        }
    }
}
