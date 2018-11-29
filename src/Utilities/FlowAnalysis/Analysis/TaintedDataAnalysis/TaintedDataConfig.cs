// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Manages tainted data sources, sanitizers, and sinks for all the 
    /// different tainted data analysis rules.
    /// </summary>
    /// <remarks>This is centralized, so that rules that use the same set of 
    /// sources and sanitizers but have different sinks, can reuse equivalent
    /// <see cref="TaintedDataAnalysisContext"/>s, and thus reuse the same
    /// dataflow analysis result, so DFA doesn't have be invoked multiple 
    /// times.</remarks>
    internal class TaintedDataConfig
    {
        public static TaintedDataConfig Instance { get; }

        static TaintedDataConfig()
        {
            Instance = new TaintedDataConfig();
        }

        public TaintedDataSymbolMap<SourceInfo> GetSourceInfos(WellKnownTypeProvider wellKnownTypeProvider, SinkKind sinkKind)
        {
            IEnumerable<SourceInfo> sourceInfos;

            switch (sinkKind)
            {
                case SinkKind.Sql:
                    sourceInfos = WebInputSources.SourceInfos;
                    break;
                    
                default:
                    Debug.Fail($"Unknown SinkKind {sinkKind}");
                    sourceInfos = Enumerable.Empty<SourceInfo>();
                    break;                    
            }

            return new TaintedDataSymbolMap<SourceInfo>(wellKnownTypeProvider, sourceInfos);
        }
    }
}
