// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Used by <see cref="DataFlowAnalysis"/> to store intermediate dataflow results for a basic block while executing flow analysis.
    /// Results of a dataflow analysis are exposed with <see cref="DataFlowAnalysisResult{TAnalysisResult, TAbstractAnalysisValue}"/>.
    /// </summary>
    internal sealed class DataFlowAnalysisInfo<TAnalysisData> : IDisposable
        where TAnalysisData : AbstractAnalysisData
    {
        public DataFlowAnalysisInfo(TAnalysisData input, TAnalysisData output)
        {
            Debug.Assert(!ReferenceEquals(input, output) || input == null);
            Input = input;
            Output = output;
        }

        public TAnalysisData Input { get; }
        public TAnalysisData Output { get; }

        public DataFlowAnalysisInfo<TAnalysisData> WithInput(TAnalysisData input) => ReferenceEquals(input, Input) ? this : new DataFlowAnalysisInfo<TAnalysisData>(input, Output);
        public DataFlowAnalysisInfo<TAnalysisData> WithOutput(TAnalysisData output) => ReferenceEquals(output, Input) ? this : new DataFlowAnalysisInfo<TAnalysisData>(Input, output);

        public void Dispose()
        {
            Input?.Dispose();
            Output?.Dispose();
        }
    }
}
