// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal abstract class NamespaceOrTypeSymbol : Symbol, INamespaceOrTypeSymbol
    {
        internal abstract Symbols.NamespaceOrTypeSymbol UnderlyingNamespaceOrTypeSymbol { get; }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers()
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers().SelectAsArray(m => m.GetPublicSymbol());
        }

        ImmutableArray<ISymbol> INamespaceOrTypeSymbol.GetMembers(string name)
        {
            return UnderlyingNamespaceOrTypeSymbol.GetMembers(name).SelectAsArray(m => m.GetPublicSymbol());
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
