// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Represents endpoints of tainted data flowing from sources to a sink.
    /// </summary>
    internal sealed class TaintedDataSourceSink
    {
        public TaintedDataSourceSink(SymbolAccess sink, SinkKind sinkKind, ImmutableArray<SymbolAccess> sourceOrigins)
        {
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            SinkKind = sinkKind;
            SourceOrigins = sourceOrigins;
        }

        /// <summary>
        /// <see cref="SymbolAccess"/> of the sink that the tainted data enters.
        /// </summary>
        public SymbolAccess Sink { get; }

        /// <summary>
        /// Kind of sink (e.g. SQL).
        /// </summary>
        public SinkKind SinkKind { get; }

        /// <summary>
        /// <see cref="SymbolAccess"/>s of the origins of the tainted data.
        /// </summary>
        public ImmutableArray<SymbolAccess> SourceOrigins { get; }
    }
}
