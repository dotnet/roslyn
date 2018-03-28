// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Xml.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class AccessibilityExtensions
    {
        internal static bool MatchesSymbol(this Accessibility accessibility, ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == accessibility;
        }

        internal static XElement CreateXElement(this Accessibility accessibility)
        {
            return new XElement("AccessibilityKind", accessibility);
        }

        internal static Accessibility FromXElement(XElement accessibilityElement)
        {
            return (Accessibility)Enum.Parse(typeof(Accessibility), accessibilityElement.Value);
        }
    }
}
