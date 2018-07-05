// StringContentright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow.StringContentAnalysis
{
    internal partial class StringContentAnalysis : ForwardDataFlowAnalysis<StringContentAnalysisData, StringContentBlockAnalysisResult, StringContentAbstractValue>
    {
        /// <summary>
        /// An abstract analysis domain implementation for core analysis data tracked by <see cref="StringContentAnalysis"/>.
        /// </summary>
        private sealed class CoreAnalysisDataDomain : AnalysisEntityMapAbstractDomain<StringContentAbstractValue>
        {
            public static readonly CoreAnalysisDataDomain Instance = new CoreAnalysisDataDomain(StringContentAbstractValueDomain.Default);

            private CoreAnalysisDataDomain(AbstractValueDomain<StringContentAbstractValue> valueDomain) : base(valueDomain)
            {
            }

            protected override StringContentAbstractValue GetDefaultValue(AnalysisEntity analysisEntity) => StringContentAbstractValue.MayBeContainsNonLiteralState;
            protected override bool CanSkipNewEntry(AnalysisEntity analysisEntity, StringContentAbstractValue value) => value.NonLiteralState == StringContainsNonLiteralState.Maybe;
        }
    }
}