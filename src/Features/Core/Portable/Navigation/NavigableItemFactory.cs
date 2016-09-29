﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.LanguageServices;
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
            ImmutableArray<TaggedText>? displayTaggedParts)
        {
            var locations = GetPreferredSourceLocations(solution, symbol);
            return locations.Select(loc => GetItemFromSymbolLocation(
                solution, symbol, loc, displayTaggedParts));
        }

        public static IEnumerable<Location> GetPreferredSourceLocations(
            Solution solution, ISymbol symbol)
        {
            // Prefer non-generated source locations over generated ones.

            var sourceLocations = GetPreferredSourceLocations(symbol);

            var generatedCodeRecognitionService = solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            var candidateLocationGroups = from c in sourceLocations
                                          let doc = solution.GetDocument(c.SourceTree)
                                          where doc != null
                                          group c by generatedCodeRecognitionService.IsGeneratedCode(doc);

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

        public static IEnumerable<INavigableItem> GetPreferredNavigableItems(Solution solution, IEnumerable<INavigableItem> navigableItems)
        {
            var generatedCodeRecognitionService = solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            navigableItems = navigableItems.Where(n => n.Document != null);
            var hasNonGeneratedCodeItem = navigableItems.Any(n => !generatedCodeRecognitionService.IsGeneratedCode(n.Document));
            return hasNonGeneratedCodeItem
                ? navigableItems.Where(n => !generatedCodeRecognitionService.IsGeneratedCode(n.Document))
                : navigableItems.Where(n => generatedCodeRecognitionService.IsGeneratedCode(n.Document));
        }

        public static ImmutableArray<TaggedText> GetSymbolDisplayTaggedParts(Project project, ISymbol symbol)
        {
            var symbolDisplayService = project.LanguageServices.GetRequiredService<ISymbolDisplayService>();
            return symbolDisplayService.ToDisplayParts(symbol, GetSymbolDisplayFormat(symbol)).ToTaggedText();
        }

        private static SymbolDisplayFormat GetSymbolDisplayFormat(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return s_shortFormatWithModifiers;

                case SymbolKind.Method:
                    return symbol.IsStaticConstructor() ? s_shortFormatWithModifiers : s_shortFormat;

                default:
                    return s_shortFormat;
            }
        }

        private static readonly SymbolDisplayFormat s_shortFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeExplicitInterface |
                    SymbolDisplayMemberOptions.IncludeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat s_shortFormatWithModifiers =
            s_shortFormat.WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeModifiers |
                SymbolDisplayMemberOptions.IncludeExplicitInterface |
                SymbolDisplayMemberOptions.IncludeParameters);
    }
}
