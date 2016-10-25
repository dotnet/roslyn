// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return FindNavigableDeclaredSymbolInfos(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken);
        }

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return FindNavigableDeclaredSymbolInfos(
                document.Project, document, searchPattern, cancellationToken);
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> FindNavigableDeclaredSymbolInfos(
            Project project, Document searchDocument, string pattern, CancellationToken cancellationToken)
        {
            var containsDots = pattern.IndexOf('.') >= 0;
            using (var patternMatcher = new PatternMatcher(pattern, allowFuzzyMatching: true))
            {
                var result = ArrayBuilder<INavigateToSearchResult>.GetInstance();
                foreach (var document in project.Documents)
                {
                    if (searchDocument != null && document != searchDocument)
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    var declarationInfo = await document.GetDeclarationInfoAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var declaredSymbolInfo in declarationInfo.DeclaredSymbolInfos)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var patternMatches = patternMatcher.GetMatches(
                            GetSearchName(declaredSymbolInfo),
                            declaredSymbolInfo.FullyQualifiedContainerName,
                            includeMatchSpans: false);

                        if (!patternMatches.IsEmpty)
                        {
                            result.Add(ConvertResult(containsDots, declaredSymbolInfo, document, patternMatches));
                        }
                    }
                }

                return result.ToImmutableAndFree();
            }
        }

        private static string GetSearchName(DeclaredSymbolInfo declaredSymbolInfo)
        {
            if (declaredSymbolInfo.Kind == DeclaredSymbolInfoKind.Indexer && declaredSymbolInfo.Name == WellKnownMemberNames.Indexer)
            {
                return "this";
            }
            else
            {
                return declaredSymbolInfo.Name;
            }
        }

        private static INavigateToSearchResult ConvertResult(
            bool containsDots, DeclaredSymbolInfo declaredSymbolInfo, Document document, 
            PatternMatches matches)
        {
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

        private static NavigateToMatchKind GetNavigateToMatchKind(
            bool containsDots, PatternMatches matchResult)
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
                var lastResult = matchResult.CandidateMatches.LastOrNullable();
                if (lastResult.HasValue)
                {
                    switch (lastResult.Value.Kind)
                    {
                        case PatternMatchKind.Exact:
                            return NavigateToMatchKind.Exact;
                        case PatternMatchKind.Prefix:
                            return NavigateToMatchKind.Prefix;
                        case PatternMatchKind.Substring:
                            return NavigateToMatchKind.Substring;
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
                    return NavigateToMatchKind.Exact;
                }

                if (matchResult.Any(r => r.Kind == PatternMatchKind.Prefix))
                {
                    return NavigateToMatchKind.Prefix;
                }

                if (matchResult.Any(r => r.Kind == PatternMatchKind.Substring))
                {
                    return NavigateToMatchKind.Substring;
                }
            }

            return NavigateToMatchKind.Regular;
        }
    }
}