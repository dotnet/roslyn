// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify
{
    internal abstract partial class AbstractFullyQualifyCodeFixProvider : CodeFixProvider
    {
        private struct SymbolResult : IEquatable<SymbolResult>, IComparable<SymbolResult>
        {
            public readonly INamespaceOrTypeSymbol Symbol;
            public readonly int Weight;
            public readonly IReadOnlyList<string> NameParts;

            public SymbolResult(INamespaceOrTypeSymbol symbol, int weight)
            {
                Symbol = symbol;
                Weight = weight;
                NameParts = INamespaceOrTypeSymbolExtensions.GetNameParts(symbol);
            }

            public override bool Equals(object obj)
            {
                return Equals((SymbolResult)obj);
            }

            public bool Equals(SymbolResult other)
            {
                return Equals(Symbol, other.Symbol);
            }

            public override int GetHashCode()
            {
                return Symbol.GetHashCode();
            }

            public SymbolResult WithSymbol(INamespaceOrTypeSymbol other)
            {
                return new SymbolResult(other, Weight);
            }

            public int CompareTo(SymbolResult other)
            {
                Contract.Requires(this.Symbol is INamespaceSymbol || !((INamedTypeSymbol)this.Symbol).IsGenericType);
                Contract.Requires(other.Symbol is INamespaceSymbol || !((INamedTypeSymbol)other.Symbol).IsGenericType);

                var diff = this.Weight - other.Weight;
                if (diff != 0)
                {
                    return diff;
                }

                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                    this.NameParts, other.NameParts);
            }
        }
    }
}