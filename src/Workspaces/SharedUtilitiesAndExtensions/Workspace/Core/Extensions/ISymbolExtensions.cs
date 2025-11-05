// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
    {
        // Check if the symbol is partial (definition or implementation part)
        var isPartial = symbol switch
        {
            IMethodSymbol method => method.IsPartialDefinition || method.PartialDefinitionPart != null,
            IPropertySymbol property => property.IsPartialDefinition || property.PartialDefinitionPart != null,
            IEventSymbol @event => @event.IsPartialDefinition || @event.PartialDefinitionPart != null,
            _ => false
        };

        return DeclarationModifiers.None
            .WithIsStatic(symbol.IsStatic)
            .WithIsAbstract(symbol.IsAbstract)
            .WithIsUnsafe(symbol.RequiresUnsafeModifier())
            .WithIsVirtual(symbol.IsVirtual)
            .WithIsOverride(symbol.IsOverride)
            .WithIsSealed(symbol.IsSealed)
            .WithIsRequired(symbol.IsRequired())
            .WithPartial(isPartial);
    }
}
