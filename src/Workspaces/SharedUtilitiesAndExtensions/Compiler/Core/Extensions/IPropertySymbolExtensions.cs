// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IPropertySymbolExtensions
{
    public static IFieldSymbol? GetBackingFieldIfAny(this IPropertySymbol property)
        => property.ContainingType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => property.Equals(f.AssociatedSymbol));
}
