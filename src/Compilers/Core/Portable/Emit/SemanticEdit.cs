// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Describes a symbol edit between two compilations. 
    /// For example, an addition of a method, an update of a method, removal of a type, etc.
    /// </summary>
    public readonly struct SemanticEdit : IEquatable<SemanticEdit>
    {
        /// <summary>
        /// The type of edit.
        /// </summary>
        public SemanticEditKind Kind { get; }

        /// <summary>
        /// The symbol from the earlier compilation,
        /// or null if the edit represents an addition.
        /// </summary>
        public ISymbol? OldSymbol { get; }

        /// <summary>
        /// The symbol from the later compilation, or the symbol of the containing type
        /// from the later compilation if the edit represents a deletion.
        /// </summary>
        public ISymbol? NewSymbol { get; }

        /// <summary>
        /// A map from syntax node in the later compilation to syntax node in the previous compilation, 
        /// or null if <see cref="PreserveLocalVariables"/> is false and the map is not needed or 
        /// the source of the current method is the same as the source of the previous method.
        /// </summary>
        /// <remarks>
        /// The map does not need to map all syntax nodes in the active method, only those syntax nodes
        /// that declare a local or generate a long lived local.
        /// </remarks>
        public Func<SyntaxNode, SyntaxNode?>? SyntaxMap { get; }

        /// <summary>
        /// True if the edit is an update of the active method and local values
        /// should be preserved; false otherwise.
        /// </summary>
        public bool PreserveLocalVariables { get; }

        /// <summary>
        /// Instrumentation update to be applied to a method.
        /// If not empty, <see cref="OldSymbol"/> and <see cref="NewSymbol"/> must be non-null <see cref="IMethodSymbol"/>s, and
        /// <see cref="Kind"/> must be <see cref="SemanticEditKind.Update"/>.
        /// </summary>
        public MethodInstrumentation Instrumentation { get; }

        // 4.6 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, bool preserveLocalVariables)
            : this(kind, oldSymbol, newSymbol, syntaxMap, preserveLocalVariables, MethodInstrumentation.Empty)
        {
        }

        /// <summary>
        /// Initializes an instance of <see cref="SemanticEdit"/>.
        /// </summary>
        /// <param name="kind">The type of edit.</param>
        /// <param name="oldSymbol">
        /// The symbol from the earlier compilation, or null if the edit represents an addition.
        /// </param>
        /// <param name="newSymbol">
        /// The symbol from the later compilation, or the symbol of the containing type
        /// from the later compilation if <paramref name="kind"/> is <see cref="SemanticEditKind.Delete"/>.
        /// </param>
        /// <param name="syntaxMap">
        /// A map from syntax node in the later compilation to syntax node in the previous compilation, 
        /// or null if <paramref name="preserveLocalVariables"/> is false and the map is not needed or 
        /// the source of the current method is the same as the source of the previous method.
        /// </param>
        /// <param name="preserveLocalVariables">
        /// True if the edit is an update of an active method and local values should be preserved; false otherwise.
        /// </param>
        /// <param name="instrumentation">
        /// Instrumentation update to be applied to a method.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldSymbol"/> or <paramref name="newSymbol"/> is null and the edit isn't a <see cref="SemanticEditKind.Insert"/> or <see cref="SemanticEditKind.Delete"/>, respectively.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> is not a valid kind.
        /// </exception>
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap = null, bool preserveLocalVariables = false, MethodInstrumentation instrumentation = default)
        {
            if (kind <= SemanticEditKind.None || kind > SemanticEditKind.Replace)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (oldSymbol == null && kind is not (SemanticEditKind.Insert or SemanticEditKind.Replace))
            {
                throw new ArgumentNullException(nameof(oldSymbol));
            }

            if (newSymbol == null)
            {
                throw new ArgumentNullException(nameof(newSymbol));
            }

            // Syntax map is only meaningful for update edits that preserve local variables.
            Debug.Assert(syntaxMap == null || (kind == SemanticEditKind.Update && preserveLocalVariables));

            // Partial methods should be implementations, not definitions.
            Debug.Assert(oldSymbol is not IMethodSymbol { PartialImplementationPart: not null });
            Debug.Assert(newSymbol is not IMethodSymbol { PartialImplementationPart: not null });

            // Check symbol kinds that can be deleted:
            Debug.Assert(kind != SemanticEditKind.Delete || oldSymbol is IMethodSymbol or IPropertySymbol or IEventSymbol);

            if (instrumentation.IsDefault)
            {
                instrumentation = MethodInstrumentation.Empty;
            }

            if (!instrumentation.IsEmpty)
            {
                if (kind != SemanticEditKind.Update)
                {
                    throw new ArgumentOutOfRangeException(nameof(kind));
                }

                if (oldSymbol is not IMethodSymbol)
                {
                    throw new ArgumentException(CodeAnalysisResources.MethodSymbolExpected, nameof(oldSymbol));
                }

                if (newSymbol is not IMethodSymbol)
                {
                    throw new ArgumentException(CodeAnalysisResources.MethodSymbolExpected, nameof(newSymbol));
                }

                foreach (var instrumentationKind in instrumentation.Kinds)
                {
                    if (!instrumentationKind.IsValid())
                    {
                        throw new ArgumentOutOfRangeException(nameof(MethodInstrumentation.Kinds), string.Format(CodeAnalysisResources.InvalidInstrumentationKind, instrumentationKind));
                    }
                }
            }

            Kind = kind;
            OldSymbol = oldSymbol;
            NewSymbol = newSymbol;
            PreserveLocalVariables = preserveLocalVariables;
            SyntaxMap = syntaxMap;
            Instrumentation = instrumentation;
        }

        // for testing non-public instrumentation kinds
        internal SemanticEdit(IMethodSymbol oldSymbol, IMethodSymbol newSymbol, ImmutableArray<InstrumentationKind> instrumentationKinds)
        {
            Kind = SemanticEditKind.Update;
            OldSymbol = oldSymbol;
            NewSymbol = newSymbol;
            Instrumentation = new MethodInstrumentation() { Kinds = instrumentationKinds };
        }

        // for testing:
        internal static SemanticEdit Create(SemanticEditKind kind, ISymbolInternal oldSymbol, ISymbolInternal newSymbol, Func<SyntaxNode, SyntaxNode>? syntaxMap = null, bool preserveLocalVariables = false)
            => new SemanticEdit(kind, oldSymbol?.GetISymbol(), newSymbol?.GetISymbol(), syntaxMap, preserveLocalVariables, instrumentation: default);

        public override int GetHashCode()
            => Hash.Combine(OldSymbol, Hash.Combine(NewSymbol, (int)Kind));

        public override bool Equals(object? obj)
            => obj is SemanticEdit other && Equals(other);

        /// <summary>
        /// <see cref="SemanticEdit"/>s are considered equal if they are of the same <see cref="Kind"/> and
        /// the corresponding <see cref="OldSymbol"/> and <see cref="NewSymbol"/> symbols are the same.
        /// The effects of edits that compare equal on the emitted metadata/IL are not necessarily the same.
        /// </summary>
        public bool Equals(SemanticEdit other)
            => Kind == other.Kind
                && (OldSymbol == null ? other.OldSymbol == null : OldSymbol.Equals(other.OldSymbol))
                && (NewSymbol == null ? other.NewSymbol == null : NewSymbol.Equals(other.NewSymbol));

        public static bool operator ==(SemanticEdit left, SemanticEdit right)
            => left.Equals(right);

        public static bool operator !=(SemanticEdit left, SemanticEdit right)
            => !(left == right);
    }
}
