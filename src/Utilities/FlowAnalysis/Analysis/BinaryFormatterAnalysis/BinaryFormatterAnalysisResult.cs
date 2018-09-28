// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.BinaryFormatterAnalysis
{
    /// <summary>
    /// Analysis result from execution of <see cref="BinaryFormatterAnalysis"/> on a control flow graph.
    /// </summary>
    internal sealed class BinaryFormatterAnalysisResult : DataFlowAnalysisResult<BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue>
    {
        public BinaryFormatterAnalysisResult(
            DataFlowAnalysisResult<BinaryFormatterBlockAnalysisResult, BinaryFormatterAbstractValue> parameterValidationAnalysisResult,
            ImmutableDictionary<IOperation, BinaryFormatterAbstractValue> hazardousUsages)
            : base(parameterValidationAnalysisResult)
        {
            this.HazardousUsages = hazardousUsages;
        }

        public ImmutableDictionary<IOperation, BinaryFormatterAbstractValue> HazardousUsages { get; }
    }
}
