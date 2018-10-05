// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            private readonly TypeSymbolWithAnnotations _type;
            private readonly Symbol _symbol;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type, Symbol symbol)
            {
                Debug.Assert(type.IsNull != (symbol is null));
                Debug.Assert(!(symbol is TypeSymbol));
                _type = type;
                _symbol = symbol;
            }

            internal TypeSymbolWithAnnotations Type => _type;
            internal Symbol Symbol => _symbol ?? Type.TypeSymbol;
            internal bool IsType => !_type.IsNull;
            internal bool IsAlias => _symbol?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol;
            internal bool IsDefault => _type.IsNull && _symbol is null;

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateUnannotated(INonNullTypesContext nonNullTypesContext, Symbol symbol)
            {
                if (symbol is null)
                {
                    return default;
                }
                var type = symbol as TypeSymbol;
                return type is null ?
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(default, symbol) :
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations.Create(nonNullTypesContext, type), null);
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(type, null);
            }
        }
    }
}
