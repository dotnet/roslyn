// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.InvocationCountAnalysis
{
    using InvocationCountAnalysisData = DictionaryAnalysisData<AnalysisEntity, InvocationCountAnalysisValue>;

    internal class InvocationCountBlockAnalysisResult : AbstractBlockAnalysisResult
    {
        public InvocationCountBlockAnalysisResult(BasicBlock basicBlock, InvocationCountAnalysisData data) : base(basicBlock)
        {
            Data = data.ToImmutableDictionary();
        }

        public ImmutableDictionary<AnalysisEntity, InvocationCountAnalysisValue> Data { get; }
    }
}
