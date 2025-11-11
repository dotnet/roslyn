// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    public static bool IsPartial(this ISymbol symbol)
    {
        var isPartial = symbol switch
        {
            IMethodSymbol method => method.PartialDefinitionPart != null || method.PartialImplementationPart != null,
            IPropertySymbol property => property.PartialDefinitionPart != null || property.PartialImplementationPart != null,
            _ => false
        };

        if (isPartial)
            return true;

#if !ROSLYN_4_12_OR_LOWER
        return symbol is IEventSymbol @event &&
            (@event.PartialDefinitionPart != null || @event.PartialImplementationPart != null);
#else
        return false;
#endif
    }

    public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
    {
        return DeclarationModifiers.None
            .WithIsStatic(symbol.IsStatic)
            .WithIsAbstract(symbol.IsAbstract)
            .WithIsUnsafe(symbol.RequiresUnsafeModifier())
            .WithIsVirtual(symbol.IsVirtual)
            .WithIsOverride(symbol.IsOverride)
            .WithIsSealed(symbol.IsSealed)
            .WithIsRequired(symbol.IsRequired())
            .WithPartial(symbol.IsPartial());
    }

#if !ROSLYN_4_12_OR_LOWER
    public static ISymbol? ReduceExtensionMember(this ISymbol? member, ITypeSymbol? receiverType)
    {
        if (member is null || receiverType is null)
            return null;

        return member switch
        {
            IPropertySymbol propertySymbol => propertySymbol.ReduceExtensionMember(receiverType),
            IMethodSymbol method => method.ReduceExtensionMember(receiverType),
            _ => null,
        };
    }
#endif
}
