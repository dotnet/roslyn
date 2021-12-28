// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
