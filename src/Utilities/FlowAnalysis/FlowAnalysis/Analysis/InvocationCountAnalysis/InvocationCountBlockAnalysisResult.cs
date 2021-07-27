// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Microsoft.CodeAnalysis.AnalyzerUtilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    internal class InvocationCountBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public ImmutableDictionary<AnalysisEntity, InvocationCountAbstractValue> Data { get; }

        public InvocationCountBlockAnalysisResult(InvocationCountAnalysisData invocationCountAbstractValue, BasicBlock basicBlock) : base(basicBlock)
        {
            Data = invocationCountAbstractValue.CoreAnalysisData.ToImmutableDictionary();
        }
    }
}