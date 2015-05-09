﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Describes a symbol edit between two compilations. 
    /// For example, an addition of a method, an update of a method, removal of a type, etc.
    /// </summary>
    public struct SemanticEdit : IEquatable<SemanticEdit>
    {
        /// <summary>
        /// The type of edit.
        /// </summary>
        public readonly SemanticEditKind Kind;

        /// <summary>
        /// The symbol from the earlier compilation,
        /// or null if the edit represents an addition.
        /// </summary>
        public readonly ISymbol OldSymbol;

        /// <summary>
        /// The symbol from the later compilation,
        /// or null if the edit represents a deletion.
        /// </summary>
        public readonly ISymbol NewSymbol;

        /// <summary>
        /// A map from syntax node in the later compilation to syntax node in the previous compilation, 
        /// or null if <see cref="PreserveLocalVariables"/> is false and the map is not needed or 
        /// the source of the current method is the same as the source of the previous method.
        /// </summary>
        /// <remarks>
        /// The map does not need to map all syntax nodes in the active method, only those syntax nodes
        /// that declare a local or generate a long lived local.
        /// </remarks>
        public readonly Func<SyntaxNode, SyntaxNode> SyntaxMap;

        /// <summary>
        /// True if the edit is an update of the active method and local values
        /// should be preserved; false otherwise.
        /// </summary>
        public readonly bool PreserveLocalVariables;

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
        /// <paramref name="oldSymbol"/> or <paramref name="newSymbol"/> is null and the edit isn't an <see cref="SemanticEditKind.Insert"/> or <see cref="SemanticEditKind.Delete"/>, respectively.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="kind"/> is not a valid kind.
        /// </exception>
        public SemanticEdit(SemanticEditKind kind, ISymbol oldSymbol, ISymbol newSymbol, Func<SyntaxNode, SyntaxNode> syntaxMap = null, bool preserveLocalVariables = false)
        {
            if (oldSymbol == null && kind != SemanticEditKind.Insert)
            {
                throw new ArgumentNullException(nameof(oldSymbol));
            }

            if (newSymbol == null && kind != SemanticEditKind.Delete)
            {
                throw new ArgumentNullException(nameof(newSymbol));
            }

            if (kind <= SemanticEditKind.None || kind > SemanticEditKind.Delete)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            this.Kind = kind;
            this.OldSymbol = oldSymbol;
            this.NewSymbol = newSymbol;
            this.PreserveLocalVariables = preserveLocalVariables;
            this.SyntaxMap = syntaxMap;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(OldSymbol,
                   Hash.Combine(NewSymbol,
                   (int)Kind));
        }

        public override bool Equals(object obj)
        {
            return obj is SemanticEdit && Equals((SemanticEdit)obj);
        }

        public bool Equals(SemanticEdit other)
        {
            return this.Kind == other.Kind
                && (this.OldSymbol == null ? other.OldSymbol == null : this.OldSymbol.Equals(other.OldSymbol))
                && (this.NewSymbol == null ? other.NewSymbol == null : this.NewSymbol.Equals(other.NewSymbol));
        }
    }
}
