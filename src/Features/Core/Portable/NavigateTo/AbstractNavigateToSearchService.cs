// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var client = await GetRemoteHostClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await SearchDocumentInCurrentProcessAsync(
                    document, searchPattern, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchDocumentInRemoteProcessAsync(
                    client, document, searchPattern, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInRemoteProcessAsync(
            RemoteHostClient client, Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false))
            {
                var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                    nameof(IRemoteNavigateToSearchService.SearchDocumentAsync),
                    SerializableDocumentId.Dehydrate(document),
                    searchPattern).ConfigureAwait(false);

                return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
            }
        }

        public async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var client = await GetRemoteHostClientAsync(project, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return await SearchProjectInCurrentProcessAsync(
                    project, searchPattern, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await SearchProjectInRemoteProcessAsync(
                    client, project, searchPattern, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInRemoteProcessAsync(
            RemoteHostClient client, Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var solution = project.Solution;

            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(
                solution, cancellationToken).ConfigureAwait(false))
            {
                var serializableResults = await session.InvokeAsync<SerializableNavigateToSearchResult[]>(
                    nameof(IRemoteNavigateToSearchService.SearchProjectAsync),
                    SerializableProjectId.Dehydrate(project.Id),
                    searchPattern).ConfigureAwait(false);

                return serializableResults.Select(r => r.Rehydrate(solution)).ToImmutableArray();
            }
        }

        private static Task<RemoteHostClient> GetRemoteHostClientAsync(
            Project project, CancellationToken cancellationToken)
        {
            var clientService = project.Solution.Workspace.Services.GetService<IRemoteHostClientService>();
            return clientService.GetRemoteHostClientAsync(cancellationToken);
        }


        public static async Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var results = await FindNavigableDeclaredSymbolInfos(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken).ConfigureAwait(false);
            return ProcessResult(searchPattern, results);
        }

        public static async Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var results = await FindNavigableDeclaredSymbolInfos(
                document.Project, document, searchPattern, cancellationToken).ConfigureAwait(false);
            return ProcessResult(searchPattern, results);
        }

        private static ImmutableArray<INavigateToSearchResult> ProcessResult(
            string searchPattern,
            ImmutableArray<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>> results)
        {
            var containsDots = searchPattern.IndexOf('.') >= 0;
            return results.SelectAsArray(r => ConvertResult(containsDots, r));
        }

        private static async Task<ImmutableArray<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>> FindNavigableDeclaredSymbolInfos(
            Project project, Document searchDocument, string pattern, CancellationToken cancellationToken)
        {
            using (var patternMatcher = new PatternMatcher(pattern, allowFuzzyMatching: true))
            {
                var result = ArrayBuilder<ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>>>.GetInstance();
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

                        if (patternMatches != null)
                        {
                            result.Add(ValueTuple.Create(declaredSymbolInfo, document, patternMatches));
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
            bool containsDots, ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>> result)
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

        private static NavigateToMatchKind GetNavigateToMatchKind(bool containsDots, IEnumerable<PatternMatch> matchResult)
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

        private class SearchResult : INavigateToSearchResult
        {
            public string AdditionalInformation => _lazyAdditionalInfo.Value;
            public string Name => _declaredSymbolInfo.Name;
            public string Summary => _lazySummary.Value;

            public string Kind { get; }
            public NavigateToMatchKind MatchKind { get; }
            public INavigableItem NavigableItem { get; }
            public string SecondarySort { get; }
            public bool IsCaseSensitive { get; }

            private readonly Document _document;
            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<string> _lazyAdditionalInfo;
            private readonly Lazy<string> _lazySummary;

            public SearchResult(
                Document document, DeclaredSymbolInfo declaredSymbolInfo, string kind,
                NavigateToMatchKind matchKind, bool isCaseSensitive, INavigableItem navigableItem)
            {
                _document = document;
                _declaredSymbolInfo = declaredSymbolInfo;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                NavigableItem = navigableItem;
                SecondarySort = ConstructSecondarySortString(declaredSymbolInfo);

                var declaredNavigableItem = navigableItem as NavigableItemFactory.DeclaredSymbolNavigableItem;
                Debug.Assert(declaredNavigableItem != null);

                _lazySummary = new Lazy<string>(() => declaredNavigableItem.Symbol?.GetDocumentationComment()?.SummaryText);
                _lazyAdditionalInfo = new Lazy<string>(() =>
                {
                    switch (declaredSymbolInfo.Kind)
                    {
                        case DeclaredSymbolInfoKind.Class:
                        case DeclaredSymbolInfoKind.Enum:
                        case DeclaredSymbolInfoKind.Interface:
                        case DeclaredSymbolInfoKind.Module:
                        case DeclaredSymbolInfoKind.Struct:
                            return FeaturesResources.project_space + document.Project.Name;
                        default:
                            return FeaturesResources.type_space + declaredSymbolInfo.ContainerDisplayName;
                    }
                });
            }

            private static string ConstructSecondarySortString(DeclaredSymbolInfo declaredSymbolInfo)
            {
                var secondarySortString = string.Concat(
                    declaredSymbolInfo.ParameterCount.ToString("X4"),
                    declaredSymbolInfo.TypeParameterCount.ToString("X4"),
                    declaredSymbolInfo.Name);
                return secondarySortString;
            }
        }
    }
}