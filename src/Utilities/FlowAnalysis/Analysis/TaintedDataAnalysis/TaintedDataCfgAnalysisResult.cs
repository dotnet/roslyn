// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    using System.Collections.Immutable;

    /// <summary>
    /// Result from execution of <see cref="TaintedDataAnalysis"/> on a control flow graph.
    /// </summary>
    /// <remarks>
    /// 1. Within the control flow graph, what unsanitized sources end up in sinks.
    /// 
    /// (Needed for interprocedural?)
    /// 2. What inputs can lead to sinks.
    /// 
    /// 3. What outputs get sanitized.
    /// 
    /// 4. What sources are read.
    /// </remarks>
    internal class TaintedDataCfgAnalysisResult
    {
        /// <summary>
        /// Within the control flow graph, what unsanitized sources end up in sinks.
        /// </summary>
        public ImmutableArray<TaintedSourceSinkPair> SourceSinkPairs { get; private set; }
    }
}
