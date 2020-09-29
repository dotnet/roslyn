// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Linq;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class TupleTypeSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.IsTupleType);

                var isError = symbol.TupleUnderlyingType!.TypeKind == TypeKind.Error;

                var friendlyNames = ArrayBuilder<string?>.GetInstance();
                var locations = ArrayBuilder<Location>.GetInstance();

                foreach (var element in symbol.TupleElements)
                {
                    friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                    locations.Add(element.Locations.FirstOrDefault() ?? Location.None);
                }

                visitor.WriteBoolean(isError);
                visitor.WriteStringArray(friendlyNames.ToImmutableAndFree());
                visitor.WriteLocationArray(locations.ToImmutableAndFree());

                if (isError)
                {
                    var elementTypes = ArrayBuilder<ISymbol>.GetInstance();

                    foreach (var element in symbol.TupleElements)
                    {
                        elementTypes.Add(element.Type);
                    }

                    visitor.WriteSymbolKeyArray(elementTypes.ToImmutableAndFree());
                }
                else
                {
                    visitor.WriteSymbolKey(symbol.TupleUnderlyingType);
                }
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var isError = reader.ReadBoolean();

                return isError ? ResolveErrorTuple(reader, out failureReason) : ResolveNormalTuple(reader, out failureReason);
            }

            private static SymbolKeyResolution ResolveNormalTuple(SymbolKeyReader reader, out string? failureReason)
            {
                using var elementNames = reader.ReadStringArray();
                var elementLocations = ReadElementLocations(reader, out var elementLocationsFailureReason);
                var underlyingTypeResolution = reader.ReadSymbolKey(out var underlyingTypeFailureReason);

                if (underlyingTypeFailureReason != null)
                {
                    failureReason = $"({nameof(TupleTypeSymbolKey)} {nameof(underlyingTypeResolution)} failed -> {underlyingTypeFailureReason})";
                    return default;
                }

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();

                var elementNamesArray = elementNames.ToImmutable();
                foreach (var namedType in underlyingTypeResolution.OfType<INamedTypeSymbol>())
                {
                    // Suppression on elementLocations due to https://github.com/dotnet/roslyn/issues/46527
                    result.AddIfNotNull(reader.Compilation.CreateTupleTypeSymbol(
                        namedType, elementNamesArray, elementLocations!));
                }

                return CreateResolution(result, $"({nameof(TupleTypeSymbolKey)} failed)", out failureReason);
            }

            private static SymbolKeyResolution ResolveErrorTuple(SymbolKeyReader reader, out string? failureReason)
            {
                using var elementNames = reader.ReadStringArray();
                var elementLocations = ReadElementLocations(reader, out var elementLocationsFailureReason);
                using var elementTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out var elementTypesFailureReason);

                if (elementLocationsFailureReason != null)
                {
                    failureReason = $"({nameof(TupleTypeSymbolKey)} {nameof(elementLocations)} failed -> {elementLocationsFailureReason})";
                    return default;
                }

                if (elementTypesFailureReason != null)
                {
                    failureReason = $"({nameof(TupleTypeSymbolKey)} {nameof(elementTypes)} failed -> {elementTypesFailureReason})";
                    return default;
                }

                if (elementTypes.IsDefault)
                {
                    failureReason = $"({nameof(TupleTypeSymbolKey)} {nameof(elementTypes)} failed)";
                    return default;
                }

                // Suppression on elementLocations due to https://github.com/dotnet/roslyn/issues/46527
                var result = reader.Compilation.CreateTupleTypeSymbol(
                    elementTypes.ToImmutable(), elementNames.ToImmutable(), elementLocations!);
                failureReason = null;
                return new SymbolKeyResolution(result);
            }

            private static ImmutableArray<Location> ReadElementLocations(SymbolKeyReader reader, out string? failureReason)
            {
                using var elementLocations = reader.ReadLocationArray(out failureReason);
                if (failureReason != null)
                    return default;

                // Compiler API requires that all the locations are non-null, or that there is a default
                // immutable array passed in.
                if (elementLocations.Builder.All(loc => loc == null))
                    return default;

                return elementLocations.ToImmutable()!;
            }
        }
    }
}
