// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IFindReferencesResultExtensions
    {
        public static IEnumerable<Location> GetDefinitionLocationsToShow(
            this ISymbol definition)
        {
            return definition.IsKind(SymbolKind.Namespace)
                ? SpecializedCollections.SingletonEnumerable(definition.Locations.First())
                : definition.Locations;
        }

        public static ImmutableArray<ReferencedSymbol> FilterToItemsToShow(
            this ImmutableArray<ReferencedSymbol> result, FindReferencesSearchOptions options)
        {
            return result.WhereAsArray(r => ShouldShow(r, options));
        }

        public static bool ShouldShow(
            this ReferencedSymbol referencedSymbol, FindReferencesSearchOptions options)
        {
            // If the reference has any locations then we will present it.
            if (referencedSymbol.Locations.Any())
            {
                return true;
            }

            return referencedSymbol.Definition.ShouldShowWithNoReferenceLocations(
                options, showMetadataSymbolsWithoutReferences: true);
        }

        public static bool ShouldShowWithNoReferenceLocations(
            this ISymbol definition, FindReferencesSearchOptions options, bool showMetadataSymbolsWithoutReferences)
        {
            // If the definition is implicit and we have no references, then we don't want to
            // clutter the UI with it.
            if (definition.IsImplicitlyDeclared)
            {
                return false;
            }

            // If we're associating property references with an accessor, then we don't want to show
            // a property if it is has no references.  Similarly, if we're associated associating
            // everything with the property, then we don't want to include accessors if there are no
            // references to them.
            if (options.AssociatePropertyReferencesWithSpecificAccessor)
            {
                if (definition.Kind == SymbolKind.Property)
                {
                    return false;
                }
            }
            else
            {
                if (definition.IsPropertyAccessor())
                {
                    return false;
                }
            }

            // Otherwise we still show the item even if there are no references to it.
            // And it's at least a source definition.
            if (definition.Locations.Any(static loc => loc.IsInSource))
            {
                return true;
            }

            if (showMetadataSymbolsWithoutReferences &&
                definition.Locations.Any(static loc => loc.IsInMetadata))
            {
                return true;
            }

            return false;
        }

        public static ImmutableArray<ReferencedSymbol> FilterToAliasMatches(
            this ImmutableArray<ReferencedSymbol> result,
            IAliasSymbol? aliasSymbol)
        {
            if (aliasSymbol == null)
            {
                return result;
            }

            var q = from r in result
                    let aliasLocations = r.Locations.Where(loc => SymbolEquivalenceComparer.Instance.Equals(loc.Alias, aliasSymbol)).ToImmutableArray()
                    where aliasLocations.Any()
                    select new ReferencedSymbol(r.Definition, aliasLocations);

            return q.ToImmutableArray();
        }

        public static ImmutableArray<ReferencedSymbol> FilterNonMatchingMethodNames(
            this ImmutableArray<ReferencedSymbol> result,
            Solution solution,
            ISymbol symbol)
        {
            return symbol.IsOrdinaryMethod()
                ? FilterNonMatchingMethodNamesWorker(result, solution, symbol)
                : result;
        }

        private static ImmutableArray<ReferencedSymbol> FilterNonMatchingMethodNamesWorker(
            ImmutableArray<ReferencedSymbol> references,
            Solution solution,
            ISymbol symbol)
        {
            using var _ = ArrayBuilder<ReferencedSymbol>.GetInstance(out var result);
            foreach (var reference in references)
            {
                var isCaseSensitive = solution.Services.GetLanguageServices(reference.Definition.Language).GetRequiredService<ISyntaxFactsService>().IsCaseSensitive;
                var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                if (reference.Definition.IsOrdinaryMethod() &&
                    !comparer.Equals(reference.Definition.Name, symbol.Name))
                {
                    continue;
                }

                result.Add(reference);
            }

            return result.ToImmutable();
        }
    }
}
