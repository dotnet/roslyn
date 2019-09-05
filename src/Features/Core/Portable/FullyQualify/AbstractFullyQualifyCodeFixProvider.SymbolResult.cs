// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes.FullyQualify
{
    internal abstract partial class AbstractFullyQualifyCodeFixProvider : CodeFixProvider
    {
        private readonly struct SymbolResult : IEquatable<SymbolResult>, IComparable<SymbolResult>
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
                => Equals((SymbolResult)obj);

            public bool Equals(SymbolResult other)
                => Equals(Symbol, other.Symbol);

            public override int GetHashCode()
                => Symbol.GetHashCode();

            public SymbolResult WithSymbol(INamespaceOrTypeSymbol other)
                => new SymbolResult(other, Weight);

            public int CompareTo(SymbolResult other)
            {
                Debug.Assert(Symbol is INamespaceSymbol || !((INamedTypeSymbol)Symbol).IsGenericType);
                Debug.Assert(other.Symbol is INamespaceSymbol || !((INamedTypeSymbol)other.Symbol).IsGenericType);

                var diff = Weight - other.Weight;
                if (diff != 0)
                {
                    return diff;
                }

                return INamespaceOrTypeSymbolExtensions.CompareNameParts(
                    NameParts, other.NameParts, placeSystemNamespaceFirst: true);
            }
        }
    }
}
