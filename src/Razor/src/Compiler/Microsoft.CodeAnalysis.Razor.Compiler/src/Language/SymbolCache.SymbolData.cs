// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class SymbolData(ISymbol symbol)
    {
        private readonly ISymbol _symbol = symbol;

        private ToDisplayStringResult? _toDisplayStringResult;

        public string GetDefaultDisplayString()
        {
            _toDisplayStringResult ??= new ToDisplayStringResult(_symbol);

            return _toDisplayStringResult.GetDefaultDisplayString();
        }

        public string GetFullName()
        {
            _toDisplayStringResult ??= new ToDisplayStringResult(_symbol);

            return _toDisplayStringResult.GetFullName();
        }

        public string GetGloballyQualifiedFullName()
        {
            _toDisplayStringResult ??= new ToDisplayStringResult(_symbol);

            return _toDisplayStringResult.GetGloballyQualifiedFullName();
        }
    }
}
