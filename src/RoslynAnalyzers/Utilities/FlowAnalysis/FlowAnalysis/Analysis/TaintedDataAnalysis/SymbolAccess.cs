// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
    /// <summary>
    /// Represents an access to a symbol.
    /// </summary>
    /// <remarks>This is useful to track where tainted data originated from as a source, or where tainted data entered as a sink.</remarks>
    internal sealed class SymbolAccess : CacheBasedEquatable<SymbolAccess>
    {
        public SymbolAccess(ISymbol symbol, SyntaxNode syntaxNode, ISymbol accessingMethod)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            if (syntaxNode == null)
            {
                throw new ArgumentNullException(nameof(syntaxNode));
            }

            Location = syntaxNode.GetLocation();
            AccessingMethod = accessingMethod ?? throw new ArgumentNullException(nameof(accessingMethod));
        }

        public SymbolAccess(ISymbol symbol, Location location, ISymbol accessingMethod)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Location = location ?? throw new ArgumentNullException(nameof(location));
            AccessingMethod = accessingMethod ?? throw new ArgumentNullException(nameof(accessingMethod));
        }

        /// <summary>
        /// Symbol being accessed.
        /// </summary>
        public ISymbol Symbol { get; }

        /// <summary>
        /// Location of the access, from the SyntaxNode.
        /// </summary>
        public Location Location { get; }

        /// <summary>
        /// What method contains the code performing the access.
        /// </summary>
        public ISymbol AccessingMethod { get; }

        protected override void ComputeHashCodeParts(ref RoslynHashCode hashCode)
        {
            hashCode.Add(Location.GetHashCode());
            hashCode.Add(Symbol.GetHashCode());
            hashCode.Add(AccessingMethod.GetHashCode());
        }

        protected override bool ComputeEqualsByHashCodeParts(CacheBasedEquatable<SymbolAccess> obj)
        {
            var other = (SymbolAccess)obj;
            return Location.GetHashCode() == other.Location.GetHashCode()
                && Symbol.GetHashCode() == other.Symbol.GetHashCode()
                && AccessingMethod.GetHashCode() == other.AccessingMethod.GetHashCode();
        }
    }
}
