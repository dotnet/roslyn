// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        /// <summary>
        /// Contains all the logic to search a project (in parallel) for Navigate-To results.
        /// </summary>
        private class NavigateToProjectSearcher : IDisposable
        {
            private readonly Project _project;
            private readonly Document _searchDocument;
            private readonly string _pattern;
            private readonly CancellationToken _cancellationToken;

            private readonly bool _containsDots;
            private readonly PatternMatcher _patternMatcher;

            // Delay creating of the compilation until necessary.  But once we create it,
            // cache for the remainder of the search so that it doesn't get cleaned up.
            private Compilation _compilation;

            public NavigateToProjectSearcher(Project project, Document searchDocument, string pattern, CancellationToken cancellationToken)
            {
                _project = project;
                _searchDocument = searchDocument;
                _pattern = pattern;
                _cancellationToken = cancellationToken;
                _compilation = null;

                _containsDots = pattern.IndexOf('.') >= 0;
                _patternMatcher = new PatternMatcher(pattern, allowFuzzyMatching: true);
            }

            public void Dispose()
            {
                _patternMatcher.Dispose();
            }

            internal async Task<ImmutableArray<INavigateToSearchResult>> DoAsync()
            {
                // Search all documents in parallel.  Each search gets a dedicated 'temp array' 
                // that it can place its results into without having to worry about locking.  
                // Once all the tasks are complete, we'll aggregate all the 'temp arrays' into
                // the final result.
                var documentSearchTasks = ArrayBuilder<Task>.GetInstance();
                var tempArrays = ArrayBuilder<ArrayBuilder<INavigateToSearchResult>>.GetInstance();

                try
                {
                    // Spawn off a task per document to search.
                    SpawnDocumentSearchTasks(documentSearchTasks, tempArrays);

                    // Wait for all the tasks to finish.
                    await Task.WhenAll(documentSearchTasks).ConfigureAwait(false);
                    _cancellationToken.ThrowIfCancellationRequested();

                    // Collect all the results into the final result.
                    var result = ArrayBuilder<INavigateToSearchResult>.GetInstance();
                    foreach (var tempArray in tempArrays)
                    {
                        result.AddRange(tempArray);
                    }

                    return result.ToImmutableAndFree();
                }
                finally
                {
                    foreach (var tempArray in tempArrays)
                    {
                        tempArray.Free();
                    }

                    tempArrays.Free();
                    documentSearchTasks.Free();
                }
            }

            private void SpawnDocumentSearchTasks(ArrayBuilder<Task> tasks, ArrayBuilder<ArrayBuilder<INavigateToSearchResult>> tempArrays)
            {
                foreach (var document in _project.Documents)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (_searchDocument != null && document != _searchDocument)
                    {
                        continue;
                    }

                    var tempArray = ArrayBuilder<INavigateToSearchResult>.GetInstance();
                    tempArrays.Add(tempArray);

                    // Kick off a fresh task so searching can happen in parallel.
                    tasks.Add(Task.Run(() => SearchDocumentAsync(document, tempArray), _cancellationToken));
                }
            }

            private async Task SearchDocumentAsync(
                Document document, ArrayBuilder<INavigateToSearchResult> results)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Delay creating a semantic model until necessary.  But once we create it,
                // cache for the remainder of the search so that it doesn't get cleaned up.
                SemanticModel semanticModel = null;

                var declarationInfo = await document.GetSyntaxTreeIndexAsync(_cancellationToken).ConfigureAwait(false);
                foreach (var declaredSymbolInfo in declarationInfo.DeclaredSymbolInfos)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        continue;
                    }

                    var patternMatches = _patternMatcher.GetMatches(
                        GetSearchName(declaredSymbolInfo),
                        declaredSymbolInfo.FullyQualifiedContainerName,
                        includeMatchSpans: true);

                    if (!patternMatches.IsEmpty)
                    {
                        semanticModel = semanticModel ?? await document.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);

                        // Now that we've created a semantic model, also hold onto the compilation
                        // so it doesn't get GC'ed.  This way if another document search needs a
                        // semantic model, it won't have to recreation the compilation as well.
                        _compilation = semanticModel.Compilation;

                        var converted = ConvertResult(
                            document, semanticModel, declaredSymbolInfo, patternMatches);

                        if (converted != null)
                        {
                            results.Add(converted);
                        }
                    }
                }
            }

            private string GetSearchName(DeclaredSymbolInfo declaredSymbolInfo)
            {
                return declaredSymbolInfo.Kind == DeclaredSymbolInfoKind.Indexer && declaredSymbolInfo.Name == WellKnownMemberNames.Indexer
                    ? "this"
                    : declaredSymbolInfo.Name;
            }

            private INavigateToSearchResult ConvertResult(
                Document document, SemanticModel semanticModel,
                DeclaredSymbolInfo declaredSymbolInfo, PatternMatches matches)
            {
                var symbol = declaredSymbolInfo.TryResolve(semanticModel, _cancellationToken);
                if (symbol == null)
                {
                    return null;
                }

                var matchKind = GetNavigateToMatchKind(matches);

                // A match is considered to be case sensitive if all its constituent pattern matches are
                // case sensitive. 
                var isCaseSensitive = matches.All(m => m.IsCaseSensitive);
                var kind = GetItemKind(declaredSymbolInfo);

                var navigableItem = new NavigableItem(
                    document, declaredSymbolInfo.Span,
                    symbol.GetGlyph(), GetSymbolDisplayTaggedParts(document, symbol));

                var additionalInfo = GetAdditionalInfo(declaredSymbolInfo, document);

                return new SearchResult(
                    document, declaredSymbolInfo, kind, matchKind,
                    isCaseSensitive, navigableItem, additionalInfo,
                    matches.CandidateMatches.SelectMany(m => m.MatchedSpans).ToImmutableArray());
            }

            private static string GetAdditionalInfo(DeclaredSymbolInfo declaredSymbolInfo, Document document)
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
            }

            private static ImmutableArray<TaggedText> GetSymbolDisplayTaggedParts(
                Document document, ISymbol symbol)
            {
                var symbolDisplayService = document.GetLanguageService<ISymbolDisplayService>();
                return symbolDisplayService.ToDisplayParts(symbol, GetSymbolDisplayFormat(symbol)).ToTaggedText();
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

            private NavigateToMatchKind GetNavigateToMatchKind(PatternMatches matchResult)
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
                if (_containsDots)
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
}