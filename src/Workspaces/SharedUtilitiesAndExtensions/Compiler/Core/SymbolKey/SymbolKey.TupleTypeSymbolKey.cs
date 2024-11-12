// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class TupleTypeSymbolKey : AbstractSymbolKey<INamedTypeSymbol>
    {
        public static readonly TupleTypeSymbolKey Instance = new();

        public sealed override void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
        {
            Debug.Assert(symbol.IsTupleType);

            var isError = symbol.TupleUnderlyingType!.TypeKind == TypeKind.Error;

            using var _1 = ArrayBuilder<string?>.GetInstance(out var friendlyNames);
            using var _2 = ArrayBuilder<Location>.GetInstance(out var locations);

            foreach (var element in symbol.TupleElements)
            {
                friendlyNames.Add(element.IsImplicitlyDeclared ? null : element.Name);
                locations.Add(element.Locations.FirstOrDefault() ?? Location.None);
            }

            visitor.WriteBoolean(isError);
            visitor.WriteStringArray(friendlyNames.ToImmutable());
            visitor.WriteLocationArray(locations.ToImmutable());

            if (isError)
            {
                using var _3 = ArrayBuilder<ISymbol>.GetInstance(out var elementTypes);

                foreach (var element in symbol.TupleElements)
                    elementTypes.Add(element.Type);

                visitor.WriteSymbolKeyArray(elementTypes.ToImmutable());
            }
            else
            {
                visitor.WriteSymbolKey(symbol.TupleUnderlyingType);
            }
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, INamedTypeSymbol? contextualSymbol, out string? failureReason)
        {
            contextualSymbol = contextualSymbol is { IsTupleType: true } ? contextualSymbol : null;
            var isError = reader.ReadBoolean();

            return isError
                ? ResolveErrorTuple(reader, contextualSymbol, out failureReason)
                : ResolveNormalTuple(reader, contextualSymbol, out failureReason);
        }

        private static SymbolKeyResolution ResolveNormalTuple(
            SymbolKeyReader reader, INamedTypeSymbol? contextualSymbol, out string? failureReason)
        {
            using var elementNames = reader.ReadStringArray();
            var elementLocations = ReadElementLocations(reader, out var elementLocationsFailureReason);
            var underlyingTypeResolution = reader.ReadSymbolKey(
                contextualSymbol?.TupleUnderlyingType,
                out var underlyingTypeFailureReason);

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

        private static SymbolKeyResolution ResolveErrorTuple(
            SymbolKeyReader reader, INamedTypeSymbol? contextualType, out string? failureReason)
        {
            using var elementNames = reader.ReadStringArray();
            var elementLocations = ReadElementLocations(reader, out var elementLocationsFailureReason);
            using var elementTypes = reader.ReadSymbolKeyArray<INamedTypeSymbol, ITypeSymbol>(
                contextualType,
                static (contextualType, i) => SafeGet(contextualType.TupleElements, i)?.Type,
                out var elementTypesFailureReason);

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
