// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class IFindReferencesResultExtensions
    {
        public static IEnumerable<ReferencedSymbol> FilterUnreferencedSyntheticDefinitions(
            this IEnumerable<ReferencedSymbol> result)
        {
            return result.Where(ShouldKeep);
        }

        private static bool ShouldKeep(ReferencedSymbol r)
        {
            if (r.Locations.Any())
            {
                return true;
            }

            if (r.Definition.IsImplicitlyDeclared)
            {
                return false;
            }

            if (r.Definition.IsPropertyAccessor())
            {
                return false;
            }

            return true;
        }

        public static IEnumerable<ReferencedSymbol> FilterToAliasMatches(
            this IEnumerable<ReferencedSymbol> result,
            IAliasSymbol aliasSymbolOpt)
        {
            if (aliasSymbolOpt == null)
            {
                return result;
            }

            return from r in result
                   let aliasLocations = r.Locations.Where(loc => SymbolEquivalenceComparer.Instance.Equals(loc.Alias, aliasSymbolOpt))
                   where aliasLocations.Any()
                   select new ReferencedSymbol(r.Definition, aliasLocations);
        }

        public static IEnumerable<ReferencedSymbol> FilterNonMatchingMethodNames(
            this IEnumerable<ReferencedSymbol> result,
            Solution solution,
            ISymbol symbol)
        {
            return symbol.IsOrdinaryMethod()
                ? FilterNonMatchingMethodNamesWorker(result, solution, symbol)
                : result;
        }

        private static IEnumerable<ReferencedSymbol> FilterNonMatchingMethodNamesWorker(
            IEnumerable<ReferencedSymbol> result,
            Solution solution,
            ISymbol symbol)
        {
            foreach (var reference in result)
            {
                var isCaseSensitive = solution.Workspace.Services.GetLanguageServices(reference.Definition.Language).GetService<ISyntaxFactsService>().IsCaseSensitive;
                var comparer = isCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
                if (reference.Definition.IsOrdinaryMethod() &&
                    !comparer.Equals(reference.Definition.Name, symbol.Name))
                {
                    continue;
                }

                yield return reference;
            }
        }
    }
}
