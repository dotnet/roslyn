// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
        /// Associates a syntax node in the later compilation to an error that should be
        /// reported at runtime by the IL generated for the node, if any.
        /// </summary>
        public Func<SyntaxNode, RuntimeRudeEdit?>? RuntimeRudeEdit { get; }

        /// <summary>
        /// Instrumentation update to be applied to a method.
        /// If not empty, <see cref="OldSymbol"/> and <see cref="NewSymbol"/> must be non-null <see cref="IMethodSymbol"/>s, and
        /// <see cref="Kind"/> must be <see cref="SemanticEditKind.Update"/>.
        /// </summary>
        public MethodInstrumentation Instrumentation { get; }

        // 4.6 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        [Obsolete("Use other overload")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, bool preserveLocalVariables)
            : this(kind, oldSymbol, newSymbol, syntaxMap, preserveLocalVariables, MethodInstrumentation.Empty)
        {
        }

        // 4.8 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
#pragma warning disable IDE0060 // Remove unused parameter
        [Obsolete("Use other overload")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap, bool preserveLocalVariables, MethodInstrumentation instrumentation)
            : this(kind, oldSymbol, newSymbol, syntaxMap, runtimeRudeEdit: null, MethodInstrumentation.Empty)
        {
        }
#pragma warning restore

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
        /// or null if the method state (locals, closures, etc.) doesn't need to be preserved.
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
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap = null, Func<SyntaxNode, RuntimeRudeEdit?>? runtimeRudeEdit = null, MethodInstrumentation instrumentation = default)
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

            // runtime rude edits should only be specified for methods with a syntax map:
            if (runtimeRudeEdit != null && syntaxMap == null)
            {
                throw new ArgumentNullException(nameof(syntaxMap));
            }

            if (syntaxMap != null)
            {
                if (kind != SemanticEditKind.Update)
                {
                    throw new ArgumentException("Syntax map can only be specified for updates", nameof(syntaxMap));
                }

                if (oldSymbol is not IMethodSymbol)
                {
                    throw new ArgumentException(CodeAnalysisResources.MethodSymbolExpected, nameof(oldSymbol));
                }

                if (newSymbol is not IMethodSymbol)
                {
                    throw new ArgumentException(CodeAnalysisResources.MethodSymbolExpected, nameof(newSymbol));
                }
            }

            // https://github.com/dotnet/roslyn/issues/73772: should we also do this check for partial properties?
            if (oldSymbol is IMethodSymbol { PartialImplementationPart: not null })
            {
                throw new ArgumentException("Partial method implementation required", nameof(oldSymbol));
            }

            if (newSymbol is IMethodSymbol { PartialImplementationPart: not null })
            {
                throw new ArgumentException("Partial method implementation required", nameof(newSymbol));
            }

            if (kind == SemanticEditKind.Delete && oldSymbol is not (IMethodSymbol or IPropertySymbol or IEventSymbol))
            {
                throw new ArgumentException("Deleted symbol must be a method, property or an event", nameof(oldSymbol));
            }

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
            SyntaxMap = syntaxMap;
            Instrumentation = instrumentation;
            RuntimeRudeEdit = runtimeRudeEdit;
        }

        /// <summary>
        /// True if <see cref="SyntaxMap"/> is not null.
        /// </summary>
        [MemberNotNullWhen(returnValue: true, nameof(SyntaxMap))]
        public bool PreserveLocalVariables => SyntaxMap != null;

        // for testing non-public instrumentation kinds
        internal SemanticEdit(IMethodSymbol oldSymbol, IMethodSymbol newSymbol, ImmutableArray<InstrumentationKind> instrumentationKinds)
        {
            Kind = SemanticEditKind.Update;
            OldSymbol = oldSymbol;
            NewSymbol = newSymbol;
            Instrumentation = new MethodInstrumentation() { Kinds = instrumentationKinds };
        }

        // for testing:
        internal static SemanticEdit Create(SemanticEditKind kind, ISymbolInternal oldSymbol, ISymbolInternal newSymbol, Func<SyntaxNode, SyntaxNode>? syntaxMap = null)
            => new SemanticEdit(kind, oldSymbol?.GetISymbol(), newSymbol?.GetISymbol(), syntaxMap, instrumentation: default);

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
