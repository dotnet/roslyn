// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Abstract tainted data value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="TaintedDataAnalysis"/>.
    /// </summary>
    [DebuggerDisplay("{Kind} ({SourceOrigins.Count} source origins)")]
    internal sealed class TaintedDataAbstractValue : CacheBasedEquatable<TaintedDataAbstractValue>
    {
        public static readonly TaintedDataAbstractValue NotTainted = new(TaintedDataAbstractValueKind.NotTainted, ImmutableHashSet<SymbolAccess>.Empty);

        private TaintedDataAbstractValue(TaintedDataAbstractValueKind kind, ImmutableHashSet<SymbolAccess> sourceOrigins)
        {
            this.Kind = kind;
            this.SourceOrigins = sourceOrigins;
        }

        /// <summary>
        /// The abstract value that this is representing.
        /// </summary>
        public TaintedDataAbstractValueKind Kind { get; }

        /// <summary>
        /// SyntaxNodes where the tainted data originated from.
        /// </summary>
        public ImmutableHashSet<SymbolAccess> SourceOrigins { get; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(HashUtilities.Combine(SourceOrigins));
            hashCode.Add(((int)Kind).GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<TaintedDataAbstractValue> obj)
        {
            var other = (TaintedDataAbstractValue)obj;
            return HashUtilities.Combine(SourceOrigins) == HashUtilities.Combine(other.SourceOrigins)
                && ((int)Kind).GetHashCode() == ((int)other.Kind).GetHashCode();
        }

        /// <summary>
        /// Creates a TaintedDataAbstractValue that's marked as tainted.
        /// </summary>
        /// <param name="accessingSyntax">Where the tainted data is originally coming from.</param>
        /// <param name="taintedSymbol">Symbol that's tainted.</param>
        /// <param name="accessingMethod">Method that's accessing the tainted data.</param>
        /// <returns>New TaintedDataAbstractValue that's marked as tainted.</returns>
        internal static TaintedDataAbstractValue CreateTainted(ISymbol taintedSymbol, SyntaxNode accessingSyntax, ISymbol accessingMethod)
        {
            return new TaintedDataAbstractValue(
                TaintedDataAbstractValueKind.Tainted,
                ImmutableHashSet.Create<SymbolAccess>(
                    new SymbolAccess(
                        taintedSymbol,
                        accessingSyntax,
                        accessingMethod)));
        }

        /// <summary>
        /// Merge two tainted abstract values together.
        /// </summary>
        /// <param name="value1">First tainted abstract value.</param>
        /// <param name="value2">Second tainted abstract value.</param>
        /// <returns>Tainted abstract value containing both sets of source origins.</returns>
        internal static TaintedDataAbstractValue MergeTainted(TaintedDataAbstractValue value1, TaintedDataAbstractValue value2)
        {
            Debug.Assert(value1.Kind == TaintedDataAbstractValueKind.Tainted);
            Debug.Assert(value2.Kind == TaintedDataAbstractValueKind.Tainted);

            return new TaintedDataAbstractValue(
                TaintedDataAbstractValueKind.Tainted,
                value1.SourceOrigins.Union(value2.SourceOrigins));
        }

        /// <summary>
        /// Merges multiple tainted abstract values together.
        /// </summary>
        /// <param name="taintedValues">Enumeration of tainted abstract values.</param>
        /// <returns>Tainted abstract value containing the super set of source origins.</returns>
        internal static TaintedDataAbstractValue MergeTainted(IEnumerable<TaintedDataAbstractValue> taintedValues)
        {
            var sourceOriginsBuilder = PooledHashSet<SymbolAccess>.GetInstance();
            foreach (TaintedDataAbstractValue value in taintedValues)
            {
                Debug.Assert(value.Kind == TaintedDataAbstractValueKind.Tainted);

                sourceOriginsBuilder.UnionWith(value.SourceOrigins);
            }

            return new TaintedDataAbstractValue(
                TaintedDataAbstractValueKind.Tainted,
                sourceOriginsBuilder.ToImmutableAndFree());
        }
    }
}
