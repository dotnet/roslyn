// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class IPropertySymbolExtensions
    {
        /// <summary>
        /// Check if a property is an auto-property.
        /// TODO: Remove this helper when https://github.com/dotnet/roslyn/issues/46682 is handled.
        /// </summary>
        public static bool IsAutoProperty(this IPropertySymbol propertySymbol)
            => propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(f => f.IsImplicitlyDeclared && propertySymbol.Equals(f.AssociatedSymbol));

        public static ImmutableArray<IPropertySymbol> GetOriginalDefinitions(this IPropertySymbol propertySymbol)
        {
            ImmutableArray<IPropertySymbol>.Builder originalDefinitionsBuilder = ImmutableArray.CreateBuilder<IPropertySymbol>();

            if (propertySymbol.IsOverride && (propertySymbol.OverriddenProperty != null))
            {
                originalDefinitionsBuilder.Add(propertySymbol.OverriddenProperty);
            }

            if (!propertySymbol.ExplicitInterfaceImplementations.IsEmpty)
            {
                originalDefinitionsBuilder.AddRange(propertySymbol.ExplicitInterfaceImplementations);
            }

            var typeSymbol = propertySymbol.ContainingType;
            var methodSymbolName = propertySymbol.Name;

            originalDefinitionsBuilder.AddRange(typeSymbol.AllInterfaces
                .SelectMany(m => m.GetMembers(methodSymbolName))
                .OfType<IPropertySymbol>()
                .Where(m => propertySymbol.Parameters.Length == m.Parameters.Length
                            && propertySymbol.IsIndexer == m.IsIndexer
                            && typeSymbol.FindImplementationForInterfaceMember(m) != null));

            return originalDefinitionsBuilder.ToImmutable();
        }
    }
}
