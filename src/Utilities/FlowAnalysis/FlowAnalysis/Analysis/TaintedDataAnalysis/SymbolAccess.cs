// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis.DataFlow;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis
{
#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>

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

        protected override void ComputeHashCodeParts(Action<int> addPart)
        {
            addPart(Location.GetHashCode());
            addPart(Symbol.GetHashCode());
            addPart(AccessingMethod.GetHashCode());
        }
    }
}
