// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
    {
        internal abstract Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol { get; }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers().GetPublicSymbols();
        }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers(name).GetPublicSymbols();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers()
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers().GetPublicSymbols();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name).GetPublicSymbols();
        }

        ImmutableArray<INamedTypeSymbol> INamespaceOrTypeSymbol.GetTypeMembers(string name, int arity)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetTypeMembers(name, arity).GetPublicSymbols();
        }

        bool INamespaceOrTypeSymbol.IsNamespace => UnderlyingSymbol.Kind == SymbolKind.Namespace;

        bool INamespaceOrTypeSymbol.IsType => UnderlyingSymbol.Kind != SymbolKind.Namespace;
    }
}
