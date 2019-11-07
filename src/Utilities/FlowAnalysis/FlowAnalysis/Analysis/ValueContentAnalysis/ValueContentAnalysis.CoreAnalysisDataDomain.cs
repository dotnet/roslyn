// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;

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
            public static readonly CoreAnalysisDataDomain Instance = new CoreAnalysisDataDomain(ValueContentAbstractValueDomain.Default);

            private CoreAnalysisDataDomain(AbstractValueDomain<ValueContentAbstractValue> valueDomain) : base(valueDomain)
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
                using (forwardEdgeAnalysisData = new CoreValueContentAnalysisData(forwardEdgeAnalysisData))
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