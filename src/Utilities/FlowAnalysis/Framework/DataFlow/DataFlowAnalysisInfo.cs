// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.FlowAnalysis.DataFlow
{
    /// <summary>
    /// Used by <see cref="DataFlowAnalysis"/> to store intermediate dataflow results for a basic block while executing flow analysis.
    /// Results of a dataflow analysis are exposed with <see cref="DataFlowAnalysisResult{TAnalysisResult, TAbstractAnalysisValue}"/>.
    /// </summary>
    internal class DataFlowAnalysisInfo<T>
    {
        public DataFlowAnalysisInfo(T input, T output)
        {
            Input = input;
            Output = output;
        }

        public T Input { get; }
        public T Output { get; }

        public DataFlowAnalysisInfo<T> WithInput(T input) => ReferenceEquals(input, Input) ? this : new DataFlowAnalysisInfo<T>(input, Output);
        public DataFlowAnalysisInfo<T> WithOutput(T output) => ReferenceEquals(output, Input) ? this : new DataFlowAnalysisInfo<T>(Input, output);
    }
}
