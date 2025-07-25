// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable RS1024 // Use 'SymbolEqualityComparer' when comparing symbols (https://github.com/dotnet/roslyn/issues/78583)

using System.Linq;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class IPropertySymbolExtensions
{
    extension(IPropertySymbol property)
    {
        public IFieldSymbol? GetBackingFieldIfAny()
        => property.ContainingType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => property.Equals(f.AssociatedSymbol));
    }
}
