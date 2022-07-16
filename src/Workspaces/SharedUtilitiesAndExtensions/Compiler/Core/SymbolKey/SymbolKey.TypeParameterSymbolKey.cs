// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private sealed class TypeParameterSymbolKey : AbstractSymbolKey<ITypeParameterSymbol>
        {
            public static readonly TypeParameterSymbolKey Instance = new();

            public sealed override void Create(ITypeParameterSymbol symbol, SymbolKeyWriter visitor)
            {
                if (symbol.TypeParameterKind == TypeParameterKind.Cref)
                {
                    visitor.WriteBoolean(true);
                    visitor.WriteLocation(symbol.Locations[0]);
                }
                else
                {
                    visitor.WriteBoolean(false);
                    visitor.WriteString(symbol.MetadataName);
                    visitor.WriteSymbolKey(symbol.ContainingSymbol);
                }
            }

            protected sealed override SymbolKeyResolution Resolve(
                SymbolKeyReader reader, ITypeParameterSymbol? contextualSymbol, out string? failureReason)
            {
                var isCref = reader.ReadBoolean();

                if (isCref)
                {
                    var location = reader.ReadLocation(out var locationFailureReason)!;
                    if (locationFailureReason != null)
                    {
                        failureReason = $"({nameof(TypeParameterSymbolKey)} {nameof(location)} failed -> {locationFailureReason})";
                        return default;
                    }

                    var resolution = reader.ResolveLocation(location);

                    failureReason = null;
                    return resolution.GetValueOrDefault();
                }
                else
                {
                    var metadataName = reader.ReadString();
                    var containingSymbolResolution = reader.ReadSymbolKey(contextualSymbol?.ContainingSymbol, out var containingSymbolFailureReason);

                    if (containingSymbolFailureReason != null)
                    {
                        failureReason = $"({nameof(TypeParameterSymbolKey)} {nameof(containingSymbolResolution)} failed -> {containingSymbolFailureReason})";
                        return default;
                    }

                    using var result = PooledArrayBuilder<ITypeParameterSymbol>.GetInstance();
                    foreach (var containingSymbol in containingSymbolResolution)
                    {
                        foreach (var typeParam in containingSymbol.GetTypeParameters())
                        {
                            if (typeParam.MetadataName == metadataName)
                            {
                                result.AddIfNotNull(typeParam);
                            }
                        }
                    }

                    return CreateResolution(result, $"({nameof(TypeParameterSymbolKey)} '{metadataName}' not found)", out failureReason);
                }
            }
        }
    }
}
