// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class AccessibilityExtensions
    {
        internal static bool MatchesSymbol(this Accessibility accessibility, ISymbol symbol)
        {
            return GetAccessibility(symbol) == accessibility;
        }

        internal static XElement CreateXElement(this Accessibility accessibility)
        {
            return new XElement("AccessibilityKind", accessibility);
        }

        internal static Accessibility FromXElement(XElement accessibilityElement)
        {
            return (Accessibility)Enum.Parse(typeof(Accessibility), accessibilityElement.Value);
        }

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

                case SymbolKind.Method when ((IMethodSymbol)currentSymbol).MethodKind == MethodKind.LocalFunction:
                    // Always treat local functions as 'local'
                    return Accessibility.NotApplicable;

                default:
                    return currentSymbol.DeclaredAccessibility;
                }
            }

            return Accessibility.NotApplicable;
        }
    }
}
