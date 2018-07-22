// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            private readonly object _symbolOrTypeSymbolWithAnnotations;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(object symbolOrTypeSymbolWithAnnotations)
            {
                Debug.Assert(symbolOrTypeSymbolWithAnnotations != null);
                Debug.Assert(!(symbolOrTypeSymbolWithAnnotations is TypeSymbol));
                _symbolOrTypeSymbolWithAnnotations = symbolOrTypeSymbolWithAnnotations;
            }

            internal TypeSymbolWithAnnotations Type => _symbolOrTypeSymbolWithAnnotations as TypeSymbolWithAnnotations;
            internal Symbol Symbol => _symbolOrTypeSymbolWithAnnotations as Symbol ?? Type?.TypeSymbol;
            internal bool IsType => !(Type is null);
            internal bool IsAlias => (_symbolOrTypeSymbolWithAnnotations as Symbol)?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol;
            internal bool IsDefault => _symbolOrTypeSymbolWithAnnotations is null;

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateUnannotated(INonNullTypesContext nonNullTypesContext, Symbol symbol)
            {
                if (symbol is null)
                {
                    return default;
                }
                var type = symbol as TypeSymbol;
                if (type is null)
                {
                    return new NamespaceOrTypeOrAliasSymbolWithAnnotations(symbol);
                }
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations.CreateUnannotated(nonNullTypesContext, type));
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(type);
            }
        }
    }
}
