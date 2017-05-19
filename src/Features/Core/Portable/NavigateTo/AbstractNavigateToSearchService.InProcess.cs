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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private static ConditionalWeakTable<Project, Tuple<string, ImmutableArray<SearchResult>>> s_lastProjectSearchCache =
            new ConditionalWeakTable<Project, Tuple<string, ImmutableArray<SearchResult>>>();

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken);
        }

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                document.Project, document, searchPattern, cancellationToken);
        }

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
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
                    var task = searchDocument != null
                        ? ComputeSearchResultsAsync(project, searchDocument, nameMatcher, containerMatcherOpt, nameMatches, containerMatches, cancellationToken)
                        : TryFilterPreviousSearchResultsAsync(project, searchDocument, pattern, nameMatcher, containerMatcherOpt, nameMatches, containerMatches, cancellationToken);

                    // The return value from the previous calls is only valid if cancellation was not requested.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ImmutableArray<INavigateToSearchResult>.Empty;
                    }

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

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
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

            // The return value from the previous calls is only valid if cancellation was not requested.
            if (cancellationToken.IsCancellationRequested)
            {
                return ImmutableArray<SearchResult>.Empty;
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

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        private static ImmutableArray<SearchResult> FilterPreviousResults(
            ImmutableArray<SearchResult> previousResults,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SearchResult>.GetInstance();

            foreach (var previousResult in previousResults)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return ImmutableArray<SearchResult>.Empty;
                }

                var document = previousResult.Document;
                var info = previousResult.DeclaredSymbolInfo;

                AddResultIfMatch(
                    document, info, nameMatcher, containerMatcherOpt, 
                    nameMatches, containerMatches, result);
            }

            return result.ToImmutableAndFree();
        }

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
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

                if (cancellationToken.IsCancellationRequested)
                {
                    return ImmutableArray<SearchResult>.Empty;
                }

                var declarationInfo = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);

                foreach (var declaredSymbolInfo in declarationInfo.DeclaredSymbolInfos)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return ImmutableArray<SearchResult>.Empty;
                    }

                    AddResultIfMatch(
                        document, declaredSymbolInfo,
                        nameMatcher, containerMatcherOpt, 
                        nameMatches, containerMatches, 
                        result);
                }
            }

            return result.ToImmutableAndFree();
        }

        private static void AddResultIfMatch(
            Document document, DeclaredSymbolInfo declaredSymbolInfo,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches, 
            ArrayBuilder<SearchResult> result)
        {
            nameMatches.Clear();
            containerMatches.Clear();

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

            return new SearchResult(
                document, declaredSymbolInfo, kind, matchKind, isCaseSensitive, navigableItem,
                nameMatches.SelectMany(m => m.MatchedSpans).ToImmutableArray());
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
