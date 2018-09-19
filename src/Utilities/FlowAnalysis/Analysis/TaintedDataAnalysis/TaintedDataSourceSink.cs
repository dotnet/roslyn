using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Represents endpoints of tainted data flowing from sources to a sink.
    /// </summary>
    internal sealed class TaintedDataSourceSink
    {
        public TaintedDataSourceSink(SyntaxNode sinkSyntax, SinkKind sinkKind, ImmutableArray<SyntaxNode> sourceOrigins)
        {
            this.SinkSyntax = sinkSyntax;
            this.SinkKind = sinkKind;
            this.SourceOrigins = sourceOrigins;
        }

        /// <summary>
        /// <see cref="SyntaxNode"/> of the sink that the tainted data enters.
        /// </summary>
        public SyntaxNode SinkSyntax { get; }

        /// <summary>
        /// Kind of sink (e.g. SQL).
        /// </summary>
        public SinkKind SinkKind { get; }

        /// <summary>
        /// <see cref="SyntaxNode"/>s of the origins of the tainted data.
        /// </summary>
        public ImmutableArray<SyntaxNode> SourceOrigins { get; }
    }
}
