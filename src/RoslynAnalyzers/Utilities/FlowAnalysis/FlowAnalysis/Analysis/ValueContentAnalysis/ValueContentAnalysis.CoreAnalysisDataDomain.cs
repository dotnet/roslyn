// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.ValueContentAnalysis
{
    using CoreValueContentAnalysisData = DictionaryAnalysisData<AnalysisEntity, ValueContentAbstractValue>;
    using ValueContentAnalysisResult = DataFlowAnalysisResult<ValueContentBlockAnalysisResult, ValueContentAbstractValue>;

    public partial class ValueContentAnalysis : ForwardDataFlowAnalysis<ValueContentAnalysisData, ValueContentAnalysisContext, ValueContentAnalysisResult, ValueContentBlockAnalysisResult, ValueContentAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for core analysis data tracked by <see cref="ValueContentAnalysis"/>.
        /// </summary>
        private sealed class CoreAnalysisDataDomain : AnalysisEntityMapAbstractDomain<ValueContentAbstractValue>
        {
            public CoreAnalysisDataDomain(AbstractValueDomain<ValueContentAbstractValue> valueDomain, PointsToAnalysisResult? pointsToAnalysisResult)
                : base(valueDomain, pointsToAnalysisResult)
            {
            }

            protected override ValueContentAbstractValue GetDefaultValue(AnalysisEntity analysisEntity) => ValueContentAbstractValue.MayBeContainsNonLiteralState;
            protected override bool CanSkipNewEntry(AnalysisEntity analysisEntity, ValueContentAbstractValue value) => value.NonLiteralState == ValueContainsNonLiteralState.Maybe;
            protected override void AssertValidEntryForMergedMap(AnalysisEntity analysisEntity, ValueContentAbstractValue value)
            {
                // No validation.
            }

            public CoreValueContentAnalysisData MergeAnalysisDataForBackEdge(CoreValueContentAnalysisData forwardEdgeAnalysisData, CoreValueContentAnalysisData backEdgeAnalysisData)
            {
                // Stop tracking values present in both branches if their is an assignment to different literal values from the back edge.
                // Clone the input forwardEdgeAnalysisData to ensure we don't overwrite the input dictionary.
                using (forwardEdgeAnalysisData = [.. forwardEdgeAnalysisData])
                {
                    var keysInMap1 = forwardEdgeAnalysisData.Keys.ToList();
                    foreach (var key in keysInMap1)
                    {
                        var forwardEdgeValue = forwardEdgeAnalysisData[key];
                        if (backEdgeAnalysisData.TryGetValue(key, out var backEdgeValue) &&
                            backEdgeValue != forwardEdgeValue &&
                            backEdgeValue.NonLiteralState == forwardEdgeValue.NonLiteralState)
                        {
                            forwardEdgeAnalysisData[key] = ValueContentAbstractValue.MayBeContainsNonLiteralState;
                        }
                    }

                    var resultMap = Merge(forwardEdgeAnalysisData, backEdgeAnalysisData);
                    Debug.Assert(Compare(forwardEdgeAnalysisData, resultMap) <= 0);
                    Debug.Assert(Compare(backEdgeAnalysisData, resultMap) <= 0);
                    return resultMap;
                }
            }
        }
    }
}
