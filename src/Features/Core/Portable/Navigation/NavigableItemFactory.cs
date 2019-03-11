// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal static partial class NavigableItemFactory
    {
        public static INavigableItem GetItemFromSymbolLocation(
            Solution solution, ISymbol symbol, Location location,
            ImmutableArray<TaggedText>? displayTaggedParts)
        {
            return new SymbolLocationNavigableItem(
                solution, symbol, location, displayTaggedParts);
        }

        public static INavigableItem GetItemFromDeclaredSymbolInfo(DeclaredSymbolInfo declaredSymbolInfo, Document document)
        {
            return new DeclaredSymbolNavigableItem(document, declaredSymbolInfo);
        }

        public static IEnumerable<INavigableItem> GetItemsFromPreferredSourceLocations(
            Solution solution,
            ISymbol symbol,
            ImmutableArray<TaggedText>? displayTaggedParts,
            CancellationToken cancellationToken)
        {
            var locations = GetPreferredSourceLocations(solution, symbol, cancellationToken);
            return locations.Select(loc => GetItemFromSymbolLocation(
                solution, symbol, loc, displayTaggedParts));
        }

        public static IEnumerable<Location> GetPreferredSourceLocations(
            Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            // Prefer non-generated source locations over generated ones.

            var sourceLocations = GetPreferredSourceLocations(symbol);

            var candidateLocationGroups = from c in sourceLocations
                                          let doc = solution.GetDocument(c.SourceTree)
                                          where doc != null
                                          group c by doc.IsGeneratedCode(cancellationToken);

            var generatedSourceLocations = candidateLocationGroups.SingleOrDefault(g => g.Key) ?? SpecializedCollections.EmptyEnumerable<Location>();
            var nonGeneratedSourceLocations = candidateLocationGroups.SingleOrDefault(g => !g.Key) ?? SpecializedCollections.EmptyEnumerable<Location>();

            return nonGeneratedSourceLocations.Any() ? nonGeneratedSourceLocations : generatedSourceLocations;
        }

        private static IEnumerable<Location> GetPreferredSourceLocations(ISymbol symbol)
        {
            var locations = symbol.Locations;

            // First return visible source locations if we have them.  Else, go to the non-visible 
            // source locations.  
            var visibleSourceLocations = locations.Where(loc => loc.IsVisibleSourceLocation());
            return visibleSourceLocations.Any()
                ? visibleSourceLocations
                : locations.Where(loc => loc.IsInSource);
        }

        public static IEnumerable<INavigableItem> GetPreferredNavigableItems(
            Solution solution,
            IEnumerable<INavigableItem> navigableItems,
            CancellationToken cancellationToken)
        {
            navigableItems = navigableItems.Where(n => n.Document != null);
            var hasNonGeneratedCodeItem = navigableItems.Any(n => !n.Document.IsGeneratedCode(cancellationToken));
            return hasNonGeneratedCodeItem
                ? navigableItems.Where(n => !n.Document.IsGeneratedCode(cancellationToken))
                : navigableItems.Where(n => n.Document.IsGeneratedCode(cancellationToken));
        }
    }
}
