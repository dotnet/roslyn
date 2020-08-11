// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class AnonymousTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
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

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                using var propertyTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out var propertyTypesFailureReason);
#pragma warning disable IDE0007 // Use implicit type
                using PooledArrayBuilder<string> propertyNames = reader.ReadStringArray()!;
#pragma warning restore IDE0007 // Use implicit type
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
                        propertyTypes.ToImmutable(), propertyNames.ToImmutable(),
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
