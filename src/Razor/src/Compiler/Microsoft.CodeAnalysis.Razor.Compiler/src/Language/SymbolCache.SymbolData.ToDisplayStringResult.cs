// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class SymbolData
    {
        private sealed class ToDisplayStringResult
        {
            private readonly ISymbol _symbol;

            private string? _emptyDisplayFormatValue;
            private string? _fullNameTypeDisplayFormatValue;
            private string? _globallyQualifiedFullNameTypeDisplayFormatValue;

            public ToDisplayStringResult(ISymbol symbol)
            {
                _symbol = symbol;
            }

            public string GetDefaultDisplayString()
            {
                return GetToDisplayStringResult(_symbol, format: null, ref _emptyDisplayFormatValue);
            }

            public string GetFullName()
            {
                return GetToDisplayStringResult(_symbol, format: WellKnownSymbolDisplayFormats.FullNameTypeDisplayFormat, ref _fullNameTypeDisplayFormatValue);
            }

            public string GetGloballyQualifiedFullName()
            {
                return GetToDisplayStringResult(_symbol, format: WellKnownSymbolDisplayFormats.GloballyQualifiedFullNameTypeDisplayFormat, ref _globallyQualifiedFullNameTypeDisplayFormatValue);
            }

            private static string GetToDisplayStringResult(ISymbol symbol, SymbolDisplayFormat? format, ref string? cachedValue)
            {
#pragma warning disable RS0030 // Do not use banned APIs
                // This is the only location which should call ISymbol.ToDisplayString.
                // All callers of this method should cache the result into a field.
                cachedValue ??= symbol.ToDisplayString(format);
#pragma warning restore RS0030 // Do not use banned APIs

                return cachedValue;
            }
        }
    }
}
