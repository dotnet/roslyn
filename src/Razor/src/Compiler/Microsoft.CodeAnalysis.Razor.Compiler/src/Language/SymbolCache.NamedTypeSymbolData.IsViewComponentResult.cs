// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal partial class SymbolCache
{
    public sealed partial class NamedTypeSymbolData
    {
        private sealed class IsViewComponentResult
        {
            public bool IsViewComponent { get; }
            public INamedTypeSymbol ViewComponentAttribute { get; }
            public INamedTypeSymbol? NonViewComponentAttribute { get; }

            public IsViewComponentResult(INamedTypeSymbol symbol, INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
            {
                ViewComponentAttribute = viewComponentAttribute;
                NonViewComponentAttribute = nonViewComponentAttribute;

                if (symbol.DeclaredAccessibility != Accessibility.Public ||
                    symbol.IsAbstract ||
                    symbol.IsGenericType ||
                    AttributeIsDefined(symbol, nonViewComponentAttribute))
                {
                    IsViewComponent = false;
                }
                else
                {
                    IsViewComponent = symbol.Name.EndsWith(ViewComponentTypes.ViewComponentSuffix, StringComparison.Ordinal) ||
                        AttributeIsDefined(symbol, viewComponentAttribute);
                }
            }

            public bool IsMatchingCache(INamedTypeSymbol viewComponentAttribute, INamedTypeSymbol? nonViewComponentAttribute)
            {
                return SymbolEqualityComparer.Default.Equals(ViewComponentAttribute, viewComponentAttribute)
                    && SymbolEqualityComparer.Default.Equals(NonViewComponentAttribute, nonViewComponentAttribute);
            }

            private static bool AttributeIsDefined(INamedTypeSymbol type, INamedTypeSymbol? queryAttribute)
            {
                if (queryAttribute == null)
                {
                    return false;
                }

                var currentType = type;
                while (currentType != null)
                {
                    foreach (var attribute in currentType.GetAttributes())
                    {
                        if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, queryAttribute))
                        {
                            return true;
                        }
                    }

                    currentType = currentType.BaseType;
                }

                return false;
            }
        }
    }
}
