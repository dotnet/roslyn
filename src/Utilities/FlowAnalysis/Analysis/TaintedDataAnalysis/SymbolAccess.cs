// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Represents an access a symbol.
    /// </summary>
    /// <remarks>This is useful to track where tainted data originated from as a source, or where tainted data entered as a sink.</remarks>
    internal sealed class SymbolAccess : IEquatable<SymbolAccess>, IComparable<SymbolAccess>
    {
        public SymbolAccess(ISymbol symbol, SyntaxNode syntaxNode, ISymbol accessingMethod)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            SyntaxNode = syntaxNode ?? throw new ArgumentNullException(nameof(syntaxNode));
            AccessingMethod = accessingMethod ?? throw new ArgumentNullException(nameof(accessingMethod));
        }

        /// <summary>
        /// Symbol being accessed.
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// Syntax of the access.
        /// </summary>
        public SyntaxNode SyntaxNode { get; }

        /// <summary>
        /// What method has the code performing the access.
        /// </summary>
        public ISymbol AccessingMethod { get; }

        public bool Equals(SymbolAccess other)
        {
            if (other == null)
            {
                return false;
            }
            else if (Object.ReferenceEquals(this, other))
            {
                return true;
            }
            else
            {
                return this.SyntaxNode == other.SyntaxNode
                    && this.Symbol == other.Symbol
                    && this.AccessingMethod == other.AccessingMethod;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SymbolAccess other)
            {
                return this.Equals(other);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return HashUtilities.Combine(this.SyntaxNode.GetHashCode(),
                HashUtilities.Combine(this.Symbol.GetHashCode(),
                this.AccessingMethod.GetHashCode()));
        }

        public int CompareTo(SymbolAccess other)
        {
            return LocationComparer.Instance.Compare(this.SyntaxNode.GetLocation(), other.SyntaxNode.GetLocation());
        }
    }
}
