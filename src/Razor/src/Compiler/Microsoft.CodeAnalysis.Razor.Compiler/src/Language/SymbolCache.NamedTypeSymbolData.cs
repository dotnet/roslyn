// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class NamedTypeSymbolData(INamedTypeSymbol symbol)
    {
        private readonly INamedTypeSymbol _symbol = symbol;
        private IsViewComponentResult? _isViewComponentResult;

        public bool IsViewComponent(INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
        {
            var isViewComponentResult = _isViewComponentResult;
            if (isViewComponentResult is null
                || !isViewComponentResult.IsMatchingCache(viewComponentAttribute, nonViewComponentAttribute))
            {
                isViewComponentResult = new IsViewComponentResult(_symbol, viewComponentAttribute, nonViewComponentAttribute);
                _isViewComponentResult = isViewComponentResult;
            }

            return isViewComponentResult.IsViewComponent;
        }
    }
}
