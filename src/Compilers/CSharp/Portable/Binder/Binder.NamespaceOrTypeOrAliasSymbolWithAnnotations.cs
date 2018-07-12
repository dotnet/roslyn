// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            internal readonly TypeSymbolWithAnnotations Type;
            internal readonly Symbol Symbol;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type, Symbol symbol)
            {
                Debug.Assert((type is null) || (symbol is null));
                Debug.Assert(!(symbol is TypeSymbol));
                Type = type;
                Symbol = symbol;
            }

            internal bool IsType => !(Type is null);
            internal bool IsAlias => Symbol?.Kind == SymbolKind.Alias;
            internal Symbol SymbolOrType => Symbol ?? Type?.TypeSymbol;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => (NamespaceOrTypeSymbol)SymbolOrType;
            internal bool IsDefault => Symbol is null && Type is null;

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateNonNull(bool nonNullTypes, Symbol symbol)
            {
                if (symbol is null)
                {
                    return default;
                }
                var type = symbol as TypeSymbol;
                if (type is null)
                {
                    return new NamespaceOrTypeOrAliasSymbolWithAnnotations(null, symbol);
                }
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations.CreateNonNull(nonNullTypes, type), null);
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(type, null);
            }

            public static explicit operator TypeSymbolWithAnnotations(NamespaceOrTypeOrAliasSymbolWithAnnotations type)
            {
                return type.Type;
            }
        }
    }
}
