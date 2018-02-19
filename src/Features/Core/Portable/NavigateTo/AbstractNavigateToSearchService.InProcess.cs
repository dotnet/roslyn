// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private static ConditionalWeakTable<Project, Tuple<string, ImmutableArray<SearchResult>>> s_lastProjectSearchCache =
            new ConditionalWeakTable<Project, Tuple<string, ImmutableArray<SearchResult>>>();

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken);
        }

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                document.Project, document, searchPattern, cancellationToken);
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> FindSearchResultsAsync(
            Project project, Document searchDocument, string pattern, CancellationToken cancellationToken)
        {
            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);
            var nameMatcher = PatternMatcher.CreatePatternMatcher(patternName, includeMatchedSpans: true, allowFuzzyMatching: true);

            var containerMatcherOpt = patternContainerOpt != null
                ? PatternMatcher.CreateDotSeparatedContainerMatcher(patternContainerOpt)
                : null;

            using (nameMatcher)
            using (containerMatcherOpt)
            {
                var nameMatches = ArrayBuilder<PatternMatch>.GetInstance();
                var containerMatches = ArrayBuilder<PatternMatch>.GetInstance();

                try
                {
                    // If we're searching a single document, then just do a full search of 
                    // that document (we're fast enough to not need to optimize that case).
                    //
                    // If, however, we are searching a project, then see if we could potentially
                    // use the last computed results we have for that project.  If so, it can
                    // be much faster to reuse and filter that result than to compute it from
                    // scratch.
#if true
                    var task = searchDocument != null
                        ? ComputeSearchResultsAsync(project, searchDocument, nameMatcher, containerMatcherOpt, nameMatches, containerMatches, cancellationToken)
                        : TryFilterPreviousSearchResultsAsync(project, searchDocument, pattern, nameMatcher, containerMatcherOpt, nameMatches, containerMatches, cancellationToken);
#else
                    var task = ComputeSearchResultsAsync(project, searchDocument, nameMatcher, containerMatcherOpt, nameMatches, containerMatches, cancellationToken);
#endif

                    var searchResults = await task.ConfigureAwait(false);
                    return ImmutableArray<INavigateToSearchResult>.CastUp(searchResults);
                }
                finally
                {
                    nameMatches.Free();
                    containerMatches.Free();
                }
            }
        }

        private static async Task<ImmutableArray<SearchResult>> TryFilterPreviousSearchResultsAsync(
            Project project, Document searchDocument, string pattern,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            // Searching an entire project.  See if we already performed that same
            // search with a substring of the current pattern.  if so, we can use
            // the previous result and just filter that down.  This is useful for
            // the common case where a user types some pattern, then keeps adding
            // to it.
            ImmutableArray<SearchResult> searchResults;
            if (s_lastProjectSearchCache.TryGetValue(project, out var previousResult) &&
                pattern.StartsWith(previousResult.Item1))
            {
                // We can reuse the previous results and just filter them. 
                searchResults = FilterPreviousResults(
                    previousResult.Item2,
                    nameMatcher, containerMatcherOpt,
                    nameMatches, containerMatches, cancellationToken);
            }
            else
            {
                // Didn't have previous results.  Or it was a very different pattern.
                // Can't reuse.
                searchResults = await ComputeSearchResultsAsync(
                    project, searchDocument,
                    nameMatcher, containerMatcherOpt,
                    nameMatches, containerMatches, cancellationToken).ConfigureAwait(false);
            }

            // Would like to use CWT.AddOrUpdate. But that is not available on the 
            // version of .Net that we're using.  So we need to take lock as we're
            // making multiple mutations.
            lock (s_lastProjectSearchCache)
            {
                s_lastProjectSearchCache.Remove(project);
                s_lastProjectSearchCache.Add(project, Tuple.Create(pattern, searchResults));
            }

            return searchResults;
        }

        private static ImmutableArray<SearchResult> FilterPreviousResults(
            ImmutableArray<SearchResult> previousResults,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SearchResult>.GetInstance();

            foreach (var previousResult in previousResults)
            {
                var document = previousResult.Document;
                var info = previousResult.DeclaredSymbolInfo;

                AddResultIfMatch(
                    document, info, nameMatcher, containerMatcherOpt, 
                    nameMatches, containerMatches, result, cancellationToken);
            }

            return result.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<SearchResult>> ComputeSearchResultsAsync(
            Project project, Document searchDocument,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SearchResult>.GetInstance();
            foreach (var document in project.Documents)
            {
                if (searchDocument != null && document != searchDocument)
                {
                    continue;
                }

                cancellationToken.ThrowIfCancellationRequested();
                var declarationInfo = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);

                foreach (var declaredSymbolInfo in declarationInfo.DeclaredSymbolInfos)
                {
                    AddResultIfMatch(
                        document, declaredSymbolInfo,
                        nameMatcher, containerMatcherOpt, 
                        nameMatches, containerMatches, 
                        result, cancellationToken);
                }
            }

            return result.ToImmutableAndFree();
        }

        private static void AddResultIfMatch(
            Document document, DeclaredSymbolInfo declaredSymbolInfo,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches, 
            ArrayBuilder<SearchResult> result, CancellationToken cancellationToken)
        {
            nameMatches.Clear();
            containerMatches.Clear();

            cancellationToken.ThrowIfCancellationRequested();
            if (nameMatcher.AddMatches(declaredSymbolInfo.Name, nameMatches) &&
                containerMatcherOpt?.AddMatches(declaredSymbolInfo.FullyQualifiedContainerName, containerMatches) != false)
            {
                result.Add(ConvertResult(
                    declaredSymbolInfo, document, nameMatches, containerMatches));
            }
        }

        private static SearchResult ConvertResult(
            DeclaredSymbolInfo declaredSymbolInfo, Document document,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches)
        {
            var matchKind = GetNavigateToMatchKind(nameMatches);

            // A match is considered to be case sensitive if all its constituent pattern matches are
            // case sensitive. 
            var isCaseSensitive = nameMatches.All(m => m.IsCaseSensitive) && containerMatches.All(m => m.IsCaseSensitive);
            var kind = GetItemKind(declaredSymbolInfo);
            var navigableItem = NavigableItemFactory.GetItemFromDeclaredSymbolInfo(declaredSymbolInfo, document);

            var matchedSpans = ArrayBuilder<TextSpan>.GetInstance();
            foreach (var match in nameMatches)
            {
                matchedSpans.AddRange(match.MatchedSpans);
            }

            return new SearchResult(
                document, declaredSymbolInfo, kind, matchKind, isCaseSensitive, navigableItem,
                matchedSpans.ToImmutableAndFree());
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
                case DeclaredSymbolInfoKind.ExtensionMethod:
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

        private static NavigateToMatchKind GetNavigateToMatchKind(ArrayBuilder<PatternMatch> nameMatches)
        {
            if (nameMatches.Any(r => r.Kind == PatternMatchKind.Exact))
            {
                return NavigateToMatchKind.Exact;
            }

            if (nameMatches.Any(r => r.Kind == PatternMatchKind.Prefix))
            {
                return NavigateToMatchKind.Prefix;
            }

            if (nameMatches.Any(r => r.Kind == PatternMatchKind.Substring))
            {
                return NavigateToMatchKind.Substring;
            }

            return NavigateToMatchKind.Regular;
        }
    }
}
