// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private sealed class AnonymousTypeSymbolKey : AbstractSymbolKey<INamedTypeSymbol>
        {
            public static readonly AnonymousTypeSymbolKey Instance = new();

            public sealed override void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsAnonymousType);

                var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToImmutableArray();
                var propertyTypes = properties.SelectAsArray(p => p.Type);
                var propertyNames = properties.SelectAsArray(p => p.Name);
                var propertyIsReadOnly = properties.SelectAsArray(p => p.SetMethod == null);
                var propertyLocations = properties.SelectAsArray(p => p.Locations.FirstOrDefault());

                visitor.WriteSymbolKeyArray(propertyTypes);
                visitor.WriteStringArray(propertyNames);
                visitor.WriteBooleanArray(propertyIsReadOnly);
                visitor.WriteLocationArray(propertyLocations);
            }

            protected sealed override SymbolKeyResolution Resolve(
                SymbolKeyReader reader, INamedTypeSymbol? contextualSymbol, out string? failureReason)
            {
                contextualSymbol = contextualSymbol is { IsAnonymousType: true } ? contextualSymbol : null;

                var contextualProperties = contextualSymbol?.GetMembers().OfType<IPropertySymbol>().ToImmutableArray() ?? ImmutableArray<IPropertySymbol>.Empty;

                using var propertyTypes = reader.ReadSymbolKeyArray<INamedTypeSymbol, ITypeSymbol>(
                    contextualSymbol,
                    getContextualSymbol: (contextualSymbol, i) => SafeGet(contextualProperties, i)?.Type,
                    out var propertyTypesFailureReason);

                using var propertyNames = reader.ReadStringArray();
                using var propertyIsReadOnly = reader.ReadBooleanArray();

                var propertyLocations = ReadPropertyLocations(reader, out var propertyLocationsFailureReason);

                if (propertyTypesFailureReason != null)
                {
                    failureReason = $"({nameof(AnonymousTypeSymbolKey)} {nameof(propertyTypes)} failed -> {propertyTypesFailureReason})";
                    return default;
                }

                if (propertyLocationsFailureReason != null)
                {
                    failureReason = $"({nameof(AnonymousTypeSymbolKey)} {nameof(propertyLocations)} failed -> {propertyLocationsFailureReason})";
                    return default;
                }

                if (!propertyTypes.IsDefault)
                {
                    var anonymousType = reader.Compilation.CreateAnonymousTypeSymbol(
                        propertyTypes.ToImmutable(), propertyNames.ToImmutable()!,
                        propertyIsReadOnly.ToImmutable(), propertyLocations);
                    failureReason = null;
                    return new SymbolKeyResolution(anonymousType);
                }

                failureReason = null;
                return new SymbolKeyResolution(reader.Compilation.ObjectType);
            }

            private static ImmutableArray<Location> ReadPropertyLocations(SymbolKeyReader reader, out string? failureReason)
            {
                using var propertyLocations = reader.ReadLocationArray(out failureReason);
                if (failureReason != null)
                    return default;

                // Compiler API requires that all the locations are non-null, or that there is a default
                // immutable array passed in.
                if (propertyLocations.Builder.All(loc => loc == null))
                    return default;

                return propertyLocations.ToImmutable()!;
            }
        }
    }
}
