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
            private readonly bool _isNullableEnabled;

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type)
            {
                Debug.Assert(!type.IsNull);
                _type = type;
                _symbol = null;
                _isNullableEnabled = false; // Not meaningful for a TypeSymbolWithAnnotations, it already baked the fact into its content.
            }

            private NamespaceOrTypeOrAliasSymbolWithAnnotations(Symbol symbol, bool isNullableEnabled)
            {
                Debug.Assert(!(symbol is TypeSymbol));
                _type = default;
                _symbol = symbol;
                _isNullableEnabled = isNullableEnabled;
            }

            internal TypeSymbolWithAnnotations Type => _type;
            internal Symbol Symbol => _symbol ?? Type.TypeSymbol;
            internal bool IsType => !_type.IsNull;
            internal bool IsAlias => _symbol?.Kind == SymbolKind.Alias;
            internal NamespaceOrTypeSymbol NamespaceOrTypeSymbol => Symbol as NamespaceOrTypeSymbol;
            internal bool IsDefault => _type.IsNull && _symbol is null;

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
                    new NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations.Create(isNullableEnabled, type));
            }

            public static implicit operator NamespaceOrTypeOrAliasSymbolWithAnnotations(TypeSymbolWithAnnotations type)
            {
                return new NamespaceOrTypeOrAliasSymbolWithAnnotations(type);
            }
        }
    }
}
