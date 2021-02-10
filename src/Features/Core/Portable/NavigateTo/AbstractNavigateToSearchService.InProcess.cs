// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
        private static ImmutableArray<(PatternMatchKind roslynKind, NavigateToMatchKind vsKind)> s_kindPairs =
            ImmutableArray.Create(
                (PatternMatchKind.Exact, NavigateToMatchKind.Exact),
                (PatternMatchKind.Prefix, NavigateToMatchKind.Prefix),
                (PatternMatchKind.NonLowercaseSubstring, NavigateToMatchKind.Substring),
                (PatternMatchKind.StartOfWordSubstring, NavigateToMatchKind.Substring),
                (PatternMatchKind.CamelCaseExact, NavigateToMatchKind.CamelCaseExact),
                (PatternMatchKind.CamelCasePrefix, NavigateToMatchKind.CamelCasePrefix),
                (PatternMatchKind.CamelCaseNonContiguousPrefix, NavigateToMatchKind.CamelCaseNonContiguousPrefix),
                (PatternMatchKind.CamelCaseSubstring, NavigateToMatchKind.CamelCaseSubstring),
                (PatternMatchKind.CamelCaseNonContiguousSubstring, NavigateToMatchKind.CamelCaseNonContiguousSubstring),
                (PatternMatchKind.Fuzzy, NavigateToMatchKind.Fuzzy),
                // Map our value to 'Fuzzy' as that's the lower value the platform supports.
                (PatternMatchKind.LowercaseSubstring, NavigateToMatchKind.Fuzzy));

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                project, priorityDocuments, searchDocument: null, pattern: searchPattern, kinds, cancellationToken: cancellationToken);
        }

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
        {
            return FindSearchResultsAsync(
                document.Project, priorityDocuments: ImmutableArray<Document>.Empty,
                document, searchPattern, kinds, cancellationToken);
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> FindSearchResultsAsync(
            Project project, ImmutableArray<Document> priorityDocuments, Document searchDocument,
            string pattern, IImmutableSet<string> kinds, CancellationToken cancellationToken)
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
                using var _1 = ArrayBuilder<PatternMatch>.GetInstance(out var nameMatches);
                using var _2 = ArrayBuilder<PatternMatch>.GetInstance(out var containerMatches);

                var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

                var searchResults = await ComputeSearchResultsAsync(
                    project, priorityDocuments, searchDocument, nameMatcher, containerMatcherOpt,
                    declaredSymbolInfoKindsSet, nameMatches, containerMatches, cancellationToken).ConfigureAwait(false);

                return ImmutableArray<INavigateToSearchResult>.CastUp(searchResults);
            }
        }

        private static async Task<ImmutableArray<SearchResult>> ComputeSearchResultsAsync(
            Project project, ImmutableArray<Document> priorityDocuments, Document searchDocument,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            DeclaredSymbolInfoKindSet kinds,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SearchResult>.GetInstance(out var result);

            // Prioritize the active documents if we have any.
            var highPriDocs = priorityDocuments.WhereAsArray(d => project.ContainsDocument(d.Id));

            var highPriDocsSet = highPriDocs.ToSet();
            var lowPriDocs = (await project.GetAllRegularAndSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false))
                              .Where(d => !highPriDocsSet.Contains(d));

            var orderedDocs = highPriDocs.AddRange(lowPriDocs);

            Debug.Assert(priorityDocuments.All(d => project.ContainsDocument(d.Id)), "Priority docs included doc not from project.");
            Debug.Assert(orderedDocs.Distinct().Length == orderedDocs.Length, "Ordered list contained a duplicate!");
            Debug.Assert(project.Documents.All(d => orderedDocs.Contains(d)), "At least one document from the project was missing from the ordered list!");

            foreach (var document in orderedDocs)
            {
                if (searchDocument != null && document != searchDocument)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                var declarationInfo = await document.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);

                foreach (var declaredSymbolInfo in declarationInfo.DeclaredSymbolInfos)
                {
                    await AddResultIfMatchAsync(
                        document, declaredSymbolInfo,
                        nameMatcher, containerMatcherOpt,
                        kinds,
                        nameMatches, containerMatches,
                        result, cancellationToken).ConfigureAwait(false);
                }
            }

            return result.ToImmutable();
        }

        private static async Task AddResultIfMatchAsync(
            Document document, DeclaredSymbolInfo declaredSymbolInfo,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            DeclaredSymbolInfoKindSet kinds,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            ArrayBuilder<SearchResult> result, CancellationToken cancellationToken)
        {
            nameMatches.Clear();
            containerMatches.Clear();

            cancellationToken.ThrowIfCancellationRequested();
            if (kinds.Contains(declaredSymbolInfo.Kind) &&
                nameMatcher.AddMatches(declaredSymbolInfo.Name, nameMatches) &&
                containerMatcherOpt?.AddMatches(declaredSymbolInfo.FullyQualifiedContainerName, containerMatches) != false)
            {
                result.Add(await ConvertResultAsync(
                    declaredSymbolInfo, document, nameMatches, containerMatches, cancellationToken).ConfigureAwait(false));
            }
        }

        private static async Task<SearchResult> ConvertResultAsync(
            DeclaredSymbolInfo declaredSymbolInfo, Document document,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            var matchKind = GetNavigateToMatchKind(nameMatches);

            // A match is considered to be case sensitive if all its constituent pattern matches are
            // case sensitive. 
            var isCaseSensitive = nameMatches.All(m => m.IsCaseSensitive) && containerMatches.All(m => m.IsCaseSensitive);
            var kind = GetItemKind(declaredSymbolInfo);
            var navigableItem = NavigableItemFactory.GetItemFromDeclaredSymbolInfo(declaredSymbolInfo, document);

            using var _ = ArrayBuilder<TextSpan>.GetInstance(out var matchedSpans);
            foreach (var match in nameMatches)
                matchedSpans.AddRange(match.MatchedSpans);

            // See if we have a match in a linked file.  If so, see if we have the same match in other projects that
            // this file is linked in.  If so, include the full set of projects the match is in so we can display that
            // well in the UI.
            var additionalMatchingProjects = await GetAdditionalProjectsWithMatchAsync(
                document, declaredSymbolInfo, cancellationToken).ConfigureAwait(false);
            return new SearchResult(
                document, declaredSymbolInfo, kind, matchKind, isCaseSensitive, navigableItem,
                matchedSpans.ToImmutable(), additionalMatchingProjects);
        }

        private static async Task<ImmutableArray<Project>> GetAdditionalProjectsWithMatchAsync(
            Document document, DeclaredSymbolInfo declaredSymbolInfo, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Project>.GetInstance(out var result);

            var solution = document.Project.Solution;
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = solution.GetRequiredDocument(linkedDocumentId);
                var index = await linkedDocument.GetSyntaxTreeIndexAsync(cancellationToken).ConfigureAwait(false);

                // See if the index for the other file also contains this same info.  If so, merge the results so the
                // user only sees them as a single hit in the UI.
                if (index.DeclaredSymbolInfoSet.Contains(declaredSymbolInfo))
                    result.Add(linkedDocument.Project);
            }

            result.RemoveDuplicates();
            return result.ToImmutable();
        }

        private static string GetItemKind(DeclaredSymbolInfo declaredSymbolInfo)
        {
            switch (declaredSymbolInfo.Kind)
            {
                case DeclaredSymbolInfoKind.Class:
                    return NavigateToItemKind.Class;
                case DeclaredSymbolInfoKind.Record:
                    return NavigateToItemKind.Record;
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
                    throw ExceptionUtilities.UnexpectedValue(declaredSymbolInfo.Kind);
            }
        }

        private static NavigateToMatchKind GetNavigateToMatchKind(ArrayBuilder<PatternMatch> nameMatches)
        {
            // work backwards through the match kinds.  That way our result is as bad as our worst match part.  For
            // example, say the user searches for `Console.Write` and we find `Console.Write` (exact, exact), and
            // `Console.WriteLine` (exact, prefix).  We don't want the latter hit to be considered an `exact` match, and
            // thus as good as `Console.Write`.

            for (var i = s_kindPairs.Length - 1; i >= 0; i--)
            {
                var (roslynKind, vsKind) = s_kindPairs[i];
                foreach (var match in nameMatches)
                {
                    if (match.Kind == roslynKind)
                        return vsKind;
                }
            }

            return NavigateToMatchKind.Regular;
        }

        private readonly struct DeclaredSymbolInfoKindSet
        {
            private readonly ImmutableArray<bool> _lookupTable;

            public DeclaredSymbolInfoKindSet(IEnumerable<string> navigateToItemKinds)
            {
                // The 'Contains' method implementation assumes that the DeclaredSymbolInfoKind type is unsigned.
                Debug.Assert(Enum.GetUnderlyingType(typeof(DeclaredSymbolInfoKind)) == typeof(byte));

                var lookupTable = new bool[Enum.GetValues(typeof(DeclaredSymbolInfoKind)).Length];
                foreach (var navigateToItemKind in navigateToItemKinds)
                {
                    switch (navigateToItemKind)
                    {
                        case NavigateToItemKind.Class:
                            lookupTable[(int)DeclaredSymbolInfoKind.Class] = true;
                            break;
                        case NavigateToItemKind.Record:
                            lookupTable[(int)DeclaredSymbolInfoKind.Record] = true;
                            break;
                        case NavigateToItemKind.Constant:
                            lookupTable[(int)DeclaredSymbolInfoKind.Constant] = true;
                            break;

                        case NavigateToItemKind.Delegate:
                            lookupTable[(int)DeclaredSymbolInfoKind.Delegate] = true;
                            break;

                        case NavigateToItemKind.Enum:
                            lookupTable[(int)DeclaredSymbolInfoKind.Enum] = true;
                            break;

                        case NavigateToItemKind.EnumItem:
                            lookupTable[(int)DeclaredSymbolInfoKind.EnumMember] = true;
                            break;

                        case NavigateToItemKind.Event:
                            lookupTable[(int)DeclaredSymbolInfoKind.Event] = true;
                            break;

                        case NavigateToItemKind.Field:
                            lookupTable[(int)DeclaredSymbolInfoKind.Field] = true;
                            break;

                        case NavigateToItemKind.Interface:
                            lookupTable[(int)DeclaredSymbolInfoKind.Interface] = true;
                            break;

                        case NavigateToItemKind.Method:
                            lookupTable[(int)DeclaredSymbolInfoKind.Constructor] = true;
                            lookupTable[(int)DeclaredSymbolInfoKind.ExtensionMethod] = true;
                            lookupTable[(int)DeclaredSymbolInfoKind.Method] = true;
                            break;

                        case NavigateToItemKind.Module:
                            lookupTable[(int)DeclaredSymbolInfoKind.Module] = true;
                            break;

                        case NavigateToItemKind.Property:
                            lookupTable[(int)DeclaredSymbolInfoKind.Indexer] = true;
                            lookupTable[(int)DeclaredSymbolInfoKind.Property] = true;
                            break;

                        case NavigateToItemKind.Structure:
                            lookupTable[(int)DeclaredSymbolInfoKind.Struct] = true;
                            break;

                        default:
                            // Not a recognized symbol info kind
                            break;
                    }
                }

                _lookupTable = ImmutableArray.CreateRange(lookupTable);
            }

            public bool Contains(DeclaredSymbolInfoKind item)
            {
                return (int)item < _lookupTable.Length
                    && _lookupTable[(int)item];
            }
        }
    }
}
