// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    [Export(typeof(INavigateToSearchResultProvider)), Shared]
    internal sealed partial class NavigateToSearchResultProvider : INavigateToSearchResultProvider
    {
        public async Task<IEnumerable<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var results = await NavigateToSymbolFinder.FindNavigableDeclaredSymbolInfos(project, searchPattern, cancellationToken).ConfigureAwait(false);
            var containsDots = searchPattern.IndexOf('.') >= 0;
            return results.Select(r => ConvertResult(containsDots, r));
        }

        private INavigateToSearchResult ConvertResult(bool containsDots, ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>> result)
        {
            var declaredSymbolInfo = result.Item1;
            var document = result.Item2;
            var matches = result.Item3;
            var matchKind = GetNavigateToMatchKind(containsDots, matches);

            // A match is considered to be case sensitive if all its constituent pattern matches are
            // case sensitive. 
            var isCaseSensitive = matches.All(m => m.IsCaseSensitive);
            var kind = GetItemKind(declaredSymbolInfo);
            var navigableItem = NavigableItemFactory.GetItemFromDeclaredSymbolInfo(declaredSymbolInfo, document);

            return new SearchResult(document, declaredSymbolInfo, kind, matchKind, isCaseSensitive, navigableItem);
        }

        private static string GetItemKind(DeclaredSymbolInfo declaredSymbolInfo)
        {
            switch (declaredSymbolInfo.Kind)
            {
                case DeclaredSymbolInfoKind.Class:
                    return NavigateToItemKind.Class;
                case DeclaredSymbolInfoKind.Constant:
                    return NavigateToItemKind.Constant;
                case DeclaredSymbolInfoKind.Delegate:
                    return NavigateToItemKind.Delegate;
                case DeclaredSymbolInfoKind.Enum:
                    return NavigateToItemKind.Enum;
                case DeclaredSymbolInfoKind.EnumMember:
                    return NavigateToItemKind.EnumItem;
                case DeclaredSymbolInfoKind.Event:
                    return NavigateToItemKind.Event;
                case DeclaredSymbolInfoKind.Field:
                    return NavigateToItemKind.Field;
                case DeclaredSymbolInfoKind.Interface:
                    return NavigateToItemKind.Interface;
                case DeclaredSymbolInfoKind.Constructor:
                case DeclaredSymbolInfoKind.Method:
                    return NavigateToItemKind.Method;
                case DeclaredSymbolInfoKind.Module:
                    return NavigateToItemKind.Module;
                case DeclaredSymbolInfoKind.Indexer:
                case DeclaredSymbolInfoKind.Property:
                    return NavigateToItemKind.Property;
                case DeclaredSymbolInfoKind.Struct:
                    return NavigateToItemKind.Structure;
                default:
                    return Contract.FailWithReturn<string>("Unknown declaration kind " + declaredSymbolInfo.Kind);
            }
        }

        private static MatchKind GetNavigateToMatchKind(bool containsDots, IEnumerable<PatternMatch> matchResult)
        {
            // NOTE(cyrusn): Unfortunately, the editor owns how sorting of NavigateToItems works,
            // and they only provide four buckets for sorting items before they sort by the name
            // of the items.  Because of this, we only have coarse granularity for bucketing things.
            //
            // So the question becomes: what do we do if we have multiple match results, and we
            // need to map to a single MatchKind.
            //
            // First, consider a main reason we have multiple match results.  And this happened
            // when the user types a dotted name (like "Microsoft.CodeAnalysis.ISymbol").  Such
            // a name would match actual entities: Microsoft.CodeAnalysis.ISymbol *and* 
            // Microsoft.CodeAnalysis.IAliasSymbol.  The first will be an [Exact, Exact, Exact] 
            // match, and the second will be an [Exact, Exact, CamelCase] match.  In this
            // case our belief is that the names will go from least specific to most specific. 
            // So, the left items may match lots of stuff, while the rightmost items will match
            // a smaller set of items.  As such, we use the last pattern match to try to decide
            // what type of editor MatchKind to map to.
            if (containsDots)
            {
                var lastResult = matchResult.LastOrNullable();
                if (lastResult.HasValue)
                {
                    switch (lastResult.Value.Kind)
                    {
                        case PatternMatchKind.Exact:
                            return MatchKind.Exact;
                        case PatternMatchKind.Prefix:
                            return MatchKind.Prefix;
                        case PatternMatchKind.Substring:
                            return MatchKind.Substring;
                    }
                }
            }
            else
            {
                // If it wasn't a dotted name, and we have multiple results, that's because they
                // had a something like a space separated pattern.  In that case, there's no
                // clear indication as to what is the most important part of the pattern.  So 
                // we make the result as good as any constituent part.
                if (matchResult.Any(r => r.Kind == PatternMatchKind.Exact))
                {
                    return MatchKind.Exact;
                }

                if (matchResult.Any(r => r.Kind == PatternMatchKind.Prefix))
                {
                    return MatchKind.Prefix;
                }

                if (matchResult.Any(r => r.Kind == PatternMatchKind.Substring))
                {
                    return MatchKind.Substring;
                }
            }

            return MatchKind.Regular;
        }
    }
}
