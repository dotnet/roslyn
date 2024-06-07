// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Lightup;

namespace Microsoft.CodeAnalysis;

internal static class IPropertySymbolExtensions
{
    private static readonly Func<IPropertySymbol, IPropertySymbol?> s_partialDefinitionPart
        = LightupHelpers.CreatePropertyAccessor<IPropertySymbol, IPropertySymbol?>(typeof(IPropertySymbol), nameof(PartialDefinitionPart), defaultValue: null);

    private static readonly Func<IPropertySymbol, IPropertySymbol?> s_partialImplementationPart
        = LightupHelpers.CreatePropertyAccessor<IPropertySymbol, IPropertySymbol?>(typeof(IPropertySymbol), nameof(PartialImplementationPart), defaultValue: null);

    public static IPropertySymbol? PartialDefinitionPart(this IPropertySymbol symbol)
        => s_partialDefinitionPart(symbol);

    public static IPropertySymbol? PartialImplementationPart(this IPropertySymbol symbol)
        => s_partialImplementationPart(symbol);
}
