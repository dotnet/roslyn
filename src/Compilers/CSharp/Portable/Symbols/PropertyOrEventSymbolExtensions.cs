// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Helper methods that exist to share code between properties and events.
    /// </summary>
    internal static class PEPropertyOrEventHelpers
    {
        internal static ISet<PropertySymbol> GetPropertiesForExplicitlyImplementedAccessor(MethodSymbol accessor)
        {
            return GetSymbolsForExplicitlyImplementedAccessor<PropertySymbol>(accessor);
        }

        internal static ISet<EventSymbol> GetEventsForExplicitlyImplementedAccessor(MethodSymbol accessor)
        {
            return GetSymbolsForExplicitlyImplementedAccessor<EventSymbol>(accessor);
        }

        // CONSIDER: the 99% case is a very small set.  A list might be more efficient in such cases.
        private static ISet<T> GetSymbolsForExplicitlyImplementedAccessor<T>(MethodSymbol accessor) where T : Symbol
        {
            if ((object)accessor == null)
            {
                return SpecializedCollections.EmptySet<T>();
            }

            ImmutableArray<MethodSymbol> implementedAccessors = accessor.ExplicitInterfaceImplementations;
            if (implementedAccessors.Length == 0)
            {
                return SpecializedCollections.EmptySet<T>();
            }

            var symbolsForExplicitlyImplementedAccessors = new HashSet<T>();
            foreach (var implementedAccessor in implementedAccessors)
            {
                var associatedProperty = implementedAccessor.AssociatedSymbol as T;
                if ((object)associatedProperty != null)
                {
                    symbolsForExplicitlyImplementedAccessors.Add(associatedProperty);
                }
            }
            return symbolsForExplicitlyImplementedAccessors;
        }

        // Properties and events from metadata do not have explicit accessibility. Instead,
        // the accessibility reported for the PEPropertySymbol or PEEventSymbol is the most
        // restrictive level that is no more restrictive than the getter/adder and setter/remover.
        internal static Accessibility GetDeclaredAccessibilityFromAccessors(MethodSymbol accessor1, MethodSymbol accessor2)
        {
            if ((object)accessor1 == null)
            {
                return ((object)accessor2 == null) ? Accessibility.NotApplicable : accessor2.DeclaredAccessibility;
            }
            else if ((object)accessor2 == null)
            {
                return accessor1.DeclaredAccessibility;
            }

            return GetDeclaredAccessibilityFromAccessors(accessor1.DeclaredAccessibility, accessor2.DeclaredAccessibility);
        }

        internal static Accessibility GetDeclaredAccessibilityFromAccessors(Accessibility accessibility1, Accessibility accessibility2)
        {
            var minAccessibility = (accessibility1 > accessibility2) ? accessibility2 : accessibility1;
            var maxAccessibility = (accessibility1 > accessibility2) ? accessibility1 : accessibility2;

            return ((minAccessibility == Accessibility.Protected) && (maxAccessibility == Accessibility.Internal))
                ? Accessibility.ProtectedOrInternal
                : maxAccessibility;
        }
    }
}
