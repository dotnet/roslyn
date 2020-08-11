// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    public static class IPropertySymbolExtensions
    {
        /// <summary>
        /// Check if a property is an auto-property.
        /// TODO: Remove this helper when https://github.com/dotnet/roslyn/issues/46682 is handled.
        /// </summary>
        public static bool IsAutoProperty(this IPropertySymbol propertySymbol)
            => !propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>().Any(f => propertySymbol.Equals(f.AssociatedSymbol));
    }
}
