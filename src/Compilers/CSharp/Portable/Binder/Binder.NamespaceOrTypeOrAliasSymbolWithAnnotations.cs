// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        internal readonly struct NamespaceOrTypeOrAliasSymbolWithAnnotations
        {
            private readonly TypeWithAnnotations _typeWithAnnotations;
            private readonly Symbol _symbol;
            private readonly bool _isNullableEnabled;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                Debug.Assert(typeWithAnnotations.HasType);
                _typeWithAnnotations = typeWithAnnotations;
                _symbol = null;
                _isNullableEnabled = false; // Not meaningful for a TypeWithAnnotations, it already baked the fact into its content.
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(Symbol symbol, bool isNullableEnabled)
            {
                Debug.Assert(!(symbol is TypeSymbol));
                _typeWithAnnotations = default;
                _symbol = symbol;
                _isNullableEnabled = isNullableEnabled;
            }

            internal TypeWithAnnotations TypeWithAnnotations => _typeWithAnnotations;
            internal Symbol Symbol => _symbol ?? TypeWithAnnotations.Type;
            internal bool IsType => !_typeWithAnnotations.IsDefault;
            internal bool IsAlias => _symbol?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol;
            internal bool IsDefault => !_typeWithAnnotations.HasType && _symbol is null;

            internal bool IsNullableEnabled
            {
                get
                {
                    Debug.Assert(_symbol?.Kind == SymbolKind.Alias); // Not meaningful to use this property otherwise
                    return _isNullableEnabled;
                }
            }

            internal static NamespaceOrTypeOrAliasSymbolWithAnnotations CreateUnannotated(bool isNullableEnabled, Symbol symbol)
            {
                if (symbol is null)
                {
                    return default;
                }
                var type = symbol as TypeSymbol;
                return type is null ?
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(symbol, isNullableEnabled) :
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations.Create(isNullableEnabled, type));
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeWithAnnotations typeWithAnnotations)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(typeWithAnnotations);
            }
        }
    }
}
