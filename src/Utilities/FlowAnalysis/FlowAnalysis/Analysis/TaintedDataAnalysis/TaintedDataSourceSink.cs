// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Represents endpoints of tainted data flowing from sources to a sink.
    /// </summary>
    internal sealed class TaintedDataSourceSink
    {
        public TaintedDataSourceSink(SymbolAccess sink, ImmutableHashSet<SinkKind> sinkKinds, ImmutableHashSet<SymbolAccess> sourceOrigins)
        {
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            SinkKinds = sinkKinds;
            SourceOrigins = sourceOrigins;
        }

        /// <summary>
        /// <see cref="SymbolAccess"/> of the sink that the tainted data enters.
        /// </summary>
        public SymbolAccess Sink { get; }

        /// <summary>
        /// Kind of sink (e.g. SQL).
        /// </summary>
        public ImmutableHashSet<SinkKind> SinkKinds { get; }

        /// <summary>
        /// <see cref="SymbolAccess"/>s of the origins of the tainted data.
        /// </summary>
        public ImmutableHashSet<SymbolAccess> SourceOrigins { get; }
    }
}
