// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        public static bool IsIsCompletedFromAwaiterPattern(
            [NotNullWhen(true)] this IPropertySymbol? property,
            [NotNullWhen(true)] INamedTypeSymbol? inotifyCompletionType,
            [NotNullWhen(true)] INamedTypeSymbol? icriticalNotifyCompletionType)
        {
            if (property is null
                || !property.Name.Equals("IsCompleted", StringComparison.Ordinal)
                || property.Type?.SpecialType != SpecialType.System_Boolean)
            {
                return false;
            }

            var containingType = property.ContainingType?.OriginalDefinition;
            return containingType.DerivesFrom(inotifyCompletionType)
                || containingType.DerivesFrom(icriticalNotifyCompletionType);
        }
    }
}
