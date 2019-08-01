// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

    /// <summary>
    /// Abstract tainted data value shared by a set of one of more <see cref="AnalysisEntity"/> instances tracked by <see cref="TaintedDataAnalysis"/>.
    /// </summary>
    [DebuggerDisplay("{Kind} ({SourceOrigins.Count} source origins)")]
    internal sealed class TaintedDataAbstractValue : CacheBasedEquatable<TaintedDataAbstractValue>
    {
        public static readonly TaintedDataAbstractValue NotTainted = new TaintedDataAbstractValue(TaintedDataAbstractValueKind.NotTainted, ImmutableHashSet<SymbolAccess>.Empty);

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

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(HashUtilities.Combine(SourceOrigins));
            addPart(Kind.GetHashCode());
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
            Debug.Assert(value1 != null);
            Debug.Assert(value1.Kind == TaintedDataAbstractValueKind.Tainted);
            Debug.Assert(value2 != null);
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
