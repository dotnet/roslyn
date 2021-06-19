// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;
using System;

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
        /// The symbol from the later compilation,
        /// or null if the edit represents a deletion.
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
        /// Initializes an instance of <see cref="SemanticEdit"/>.
        /// </summary>
        /// <param name="kind">The type of edit.</param>
        /// <param name="oldSymbol">
        /// The symbol from the earlier compilation, or null if the edit represents an addition.
        /// </param>
        /// <param name="newSymbol">
        /// The symbol from the later compilation, or null if the edit represents a deletion.
        /// </param>
        /// <param name="syntaxMap">
        /// A map from syntax node in the later compilation to syntax node in the previous compilation, 
        /// or null if <paramref name="preserveLocalVariables"/> is false and the map is not needed or 
        /// the source of the current method is the same as the source of the previous method.
        /// </param>
        /// <param name="preserveLocalVariables">
        /// True if the edit is an update of an active method and local values should be preserved; false otherwise.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="oldSymbol"/> or <paramref name="newSymbol"/> is null and the edit isn't a <see cref="SemanticEditKind.Insert"/> or <see cref="SemanticEditKind.Delete"/>, respectively.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> is not a valid kind.
        /// </exception>
        public SemanticEdit(SemanticEditKind kind, ISymbol? oldSymbol, ISymbol? newSymbol, Func<SyntaxNode, SyntaxNode?>? syntaxMap = null, bool preserveLocalVariables = false)
        {
            if (oldSymbol == null && kind is not (SemanticEditKind.Insert or SemanticEditKind.InsertExisting))
            {
                throw new ArgumentNullException(nameof(oldSymbol));
            }

            if (newSymbol == null && kind != SemanticEditKind.Delete)
            {
                throw new ArgumentNullException(nameof(newSymbol));
            }

            if (kind <= SemanticEditKind.None || kind > SemanticEditKind.InsertExisting)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            Kind = kind;
            OldSymbol = oldSymbol;
            NewSymbol = newSymbol;
            PreserveLocalVariables = preserveLocalVariables;
            SyntaxMap = syntaxMap;
        }

        internal static SemanticEdit Create(SemanticEditKind kind, ISymbolInternal oldSymbol, ISymbolInternal newSymbol, Func<SyntaxNode, SyntaxNode>? syntaxMap = null, bool preserveLocalVariables = false)
            => new SemanticEdit(kind, oldSymbol?.GetISymbol(), newSymbol?.GetISymbol(), syntaxMap, preserveLocalVariables);

        public override int GetHashCode()
            => Hash.Combine(OldSymbol, Hash.Combine(NewSymbol, (int)Kind));

        public override bool Equals(object? obj)
            => obj is SemanticEdit other && Equals(other);

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
