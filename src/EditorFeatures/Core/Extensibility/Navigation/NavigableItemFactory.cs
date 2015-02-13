﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal static partial class NavigableItemFactory
    {
        public static INavigableItem GetItemFromSymbolLocation(Solution solution, ISymbol symbol, Location location)
        {
            return new SymbolLocationNavigableItem(solution, symbol, location);
        }

        public static INavigableItem GetItemFromDeclaredSymbolInfo(DeclaredSymbolInfo declaredSymbolInfo, Document document)
        {
            return new DeclaredSymbolNavigableItem(document, declaredSymbolInfo);
        }

        public static IEnumerable<INavigableItem> GetItemsfromPreferredSourceLocations(Solution solution, ISymbol symbol)
        {
            var locations = GetPreferredSourceLocations(solution, symbol);
            return locations.Select(loc => GetItemFromSymbolLocation(solution, symbol, loc));
        }

        public static IEnumerable<Location> GetPreferredSourceLocations(Solution solution, ISymbol symbol)
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
