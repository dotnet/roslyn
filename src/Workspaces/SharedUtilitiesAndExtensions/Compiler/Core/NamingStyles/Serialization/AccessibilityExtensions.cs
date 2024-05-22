// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;

internal static class AccessibilityExtensions
{
    internal static bool MatchesSymbol(this Accessibility accessibility, ISymbol symbol)
        => GetAccessibility(symbol) == accessibility;

    internal static XElement CreateXElement(this Accessibility accessibility)
        => new("AccessibilityKind", accessibility);

    internal static Accessibility FromXElement(XElement accessibilityElement)
        => (Accessibility)Enum.Parse(typeof(Accessibility), accessibilityElement.Value);

    private static Accessibility GetAccessibility(ISymbol symbol)
    {
        for (var currentSymbol = symbol; currentSymbol != null; currentSymbol = currentSymbol.ContainingSymbol)
        {
            switch (currentSymbol.Kind)
            {
                case SymbolKind.Namespace:
                    return Accessibility.Public;

                case SymbolKind.Parameter:
                case SymbolKind.TypeParameter:
                    continue;

                case SymbolKind.Method:
                    switch (((IMethodSymbol)currentSymbol).MethodKind)
                    {
                        case MethodKind.AnonymousFunction:
                        case MethodKind.LocalFunction:
                            // Always treat anonymous and local functions as 'local'
                            return Accessibility.NotApplicable;

                        default:
                            return currentSymbol.DeclaredAccessibility;
                    }

                default:
                    return currentSymbol.DeclaredAccessibility;
            }
        }

        return Accessibility.NotApplicable;
    }
}
