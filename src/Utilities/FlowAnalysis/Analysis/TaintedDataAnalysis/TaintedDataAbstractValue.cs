// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Abstract tainted data value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="TaintedDataAnalysis"/>.
    /// </summary>
    internal class TaintedDataAbstractValue : CacheBasedEquatable<TaintedDataAbstractValue>
    {
        public static readonly TaintedDataAbstractValue Unknown = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.Unknown, ImmutableHashSet<SyntaxNode>.Empty);
        public static readonly TaintedDataAbstractValue NotTainted = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.NotTainted, ImmutableHashSet<SyntaxNode>.Empty);

        private TaintedDataAbstractValue(TaintedDataAbstractValueKind kind, ImmutableHashSet<SyntaxNode> sourceLocations)
        {
            this.Kind = kind;
            this.SourceOrigins = sourceLocations;
        }

        /// <summary>
        /// The abstract value that this is representing.
        /// </summary>
        public TaintedDataAbstractValueKind Kind { get; }

        /// <summary>
        /// SyntaxNodes where the tainted data originated from.
        /// </summary>
        public ImmutableHashSet<SyntaxNode> SourceOrigins { get; }

        protected override int ComputeHashCode()
        {
            return HashUtilities.Combine(this.SourceOrigins, this.Kind.GetHashCode());
        }

        /// <summary>
        /// Creates a TaintedDataAbstractValue that's marked as tainted.
        /// </summary>
        /// <param name="sourceOrigin">Where the tainted data is originally coming from.</param>
        /// <returns>New TaintedDataAbstractValue that's marked as tainted.</returns>
        internal static TaintedDataAbstractValue CreateTainted(SyntaxNode sourceOrigin)
        {
            return new TaintedDataAbstractValue(TaintedDataAbstractValueKind.Tainted, ImmutableHashSet.Create<SyntaxNode>(sourceOrigin));
        }

        /// <summary>
        /// Merge two tainted abstract values together.
        /// </summary>
        /// <param name="value1">First tainted abstract value.</param>
        /// <param name="value2">Second tainted abstract value.</param>
        /// <returns>Tainted abstract value containing both sets of source origins.</returns>
        internal static TaintedDataAbstractValue MergeTainted(TaintedDataAbstractValue value1, TaintedDataAbstractValue value2)
        {
            Debug.Assert(value1 != null);
            Debug.Assert(value1.Kind == TaintedDataAbstractValueKind.Tainted);
            Debug.Assert(value2 != null);
            Debug.Assert(value2.Kind == TaintedDataAbstractValueKind.Tainted);

            return new TaintedDataAbstractValue(
                TaintedDataAbstractValueKind.Tainted,
                value1.SourceOrigins.Union(value2.SourceOrigins));
        }
    }
}
