// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
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

        private static async Task SearchProjectInCurrentProcessAsync(
            Project project, ImmutableArray<Document> priorityDocuments,
            Document? searchDocument, string pattern, IImmutableSet<string> kinds,
            Func<RoslynNavigateToItem, Task> onResultFound, CancellationToken cancellationToken)
        {
            // We're doing a real search over the fully loaded solution now.  No need to hold onto the cached map
            // of potentially stale indices.
            ClearCachedData();

            // If the user created a dotted pattern then we'll grab the last part of the name
            var (patternName, patternContainerOpt) = PatternMatcher.GetNameAndContainer(pattern);

            var declaredSymbolInfoKindsSet = new DeclaredSymbolInfoKindSet(kinds);

            // Prioritize the active documents if we have any.
            var highPriDocs = priorityDocuments.Where(d => project.ContainsDocument(d.Id)).ToSet();
            await ProcessDocumentsAsync(searchDocument, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onResultFound, highPriDocs, cancellationToken).ConfigureAwait(false);

            // Then process non-priority documents.
            var lowPriDocs = project.Documents.Where(d => !highPriDocs.Contains(d)).ToSet();
            await ProcessDocumentsAsync(searchDocument, patternName, patternContainerOpt, declaredSymbolInfoKindsSet, onResultFound, lowPriDocs, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ProcessDocumentsAsync(
            Document? searchDocument, string patternName, string? patternContainer, DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onResultFound, ISet<Document> documents, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            foreach (var document in documents)
            {
                if (searchDocument != null && searchDocument != document)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                tasks.Add(Task.Run(() =>
                    ProcessDocumentAsync(document, patternName, patternContainer, kinds, onResultFound, cancellationToken), cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private static async Task ProcessDocumentAsync(
            Document document, string patternName, string? patternContainer, DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onResultFound, CancellationToken cancellationToken)
        {
            var index = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);

            await ProcessIndexAsync(
                document.Id, document, patternName, patternContainer, kinds, onResultFound, index, cancellationToken).ConfigureAwait(false);
        }

        private static async Task ProcessIndexAsync(
            DocumentId documentId, Document? document,
            string patternName, string? patternContainer,
            DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onResultFound,
            TopLevelSyntaxTreeIndex index,
            CancellationToken cancellationToken)
        {
            var containerMatcher = patternContainer != null
                ? PatternMatcher.CreateDotSeparatedContainerMatcher(patternContainer)
                : null;

            using var nameMatcher = PatternMatcher.CreatePatternMatcher(patternName, includeMatchedSpans: true, allowFuzzyMatching: true);
            using var _1 = containerMatcher;

            foreach (var declaredSymbolInfo in index.DeclaredSymbolInfos)
            {
                // Namespaces are never returned in nav-to as they're too common and have too many locations.
                if (declaredSymbolInfo.Kind == DeclaredSymbolInfoKind.Namespace)
                    continue;

                await AddResultIfMatchAsync(
                    documentId, document,
                    declaredSymbolInfo,
                    nameMatcher, containerMatcher,
                    kinds,
                    onResultFound, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task AddResultIfMatchAsync(
            DocumentId documentId, Document? document,
            DeclaredSymbolInfo declaredSymbolInfo,
            PatternMatcher nameMatcher, PatternMatcher? containerMatcher,
            DeclaredSymbolInfoKindSet kinds,
            Func<RoslynNavigateToItem, Task> onResultFound, CancellationToken cancellationToken)
        {
            using var nameMatches = TemporaryArray<PatternMatch>.Empty;
            using var containerMatches = TemporaryArray<PatternMatch>.Empty;

            cancellationToken.ThrowIfCancellationRequested();
            if (kinds.Contains(declaredSymbolInfo.Kind) &&
                nameMatcher.AddMatches(declaredSymbolInfo.Name, ref nameMatches.AsRef()) &&
                containerMatcher?.AddMatches(declaredSymbolInfo.FullyQualifiedContainerName, ref containerMatches.AsRef()) != false)
            {
                // See if we have a match in a linked file.  If so, see if we have the same match in
                // other projects that this file is linked in.  If so, include the full set of projects
                // the match is in so we can display that well in the UI.
                //
                // We can only do this in the case where the solution is loaded and thus we can examine
                // the relationship between this document and the other documents linked to it.  In the
                // case where the solution isn't fully loaded and we're just reading in cached data, we
                // don't know what other files we're linked to and can't merge results in this fashion.
                var additionalMatchingProjects = await GetAdditionalProjectsWithMatchAsync(
                    document, declaredSymbolInfo, cancellationToken).ConfigureAwait(false);

                var result = ConvertResult(
                    documentId, document, declaredSymbolInfo, nameMatches, containerMatches, additionalMatchingProjects);
                await onResultFound(result).ConfigureAwait(false);
            }
        }

        private static RoslynNavigateToItem ConvertResult(
            DocumentId documentId,
            Document? document,
            DeclaredSymbolInfo declaredSymbolInfo,
            in TemporaryArray<PatternMatch> nameMatches,
            in TemporaryArray<PatternMatch> containerMatches,
            ImmutableArray<ProjectId> additionalMatchingProjects)
        {
            var matchKind = GetNavigateToMatchKind(nameMatches);

            // A match is considered to be case sensitive if all its constituent pattern matches are
            // case sensitive. 
            var isCaseSensitive = nameMatches.All(m => m.IsCaseSensitive) && containerMatches.All(m => m.IsCaseSensitive);
            var kind = GetItemKind(declaredSymbolInfo);

            using var matchedSpans = TemporaryArray<TextSpan>.Empty;
            foreach (var match in nameMatches)
                matchedSpans.AddRange(match.MatchedSpans);

            // If we were not given a Document instance, then we're finding matches in cached data
            // and thus could be 'stale'.
            return new RoslynNavigateToItem(
                isStale: document == null,
                documentId,
                additionalMatchingProjects,
                declaredSymbolInfo,
                kind,
                matchKind,
                isCaseSensitive,
                matchedSpans.ToImmutableAndClear());
        }

        private static async ValueTask<ImmutableArray<ProjectId>> GetAdditionalProjectsWithMatchAsync(
            Document? document, DeclaredSymbolInfo declaredSymbolInfo, CancellationToken cancellationToken)
        {
            if (document == null)
                return ImmutableArray<ProjectId>.Empty;

            using var _ = ArrayBuilder<ProjectId>.GetInstance(out var result);

            var solution = document.Project.Solution;
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = solution.GetRequiredDocument(linkedDocumentId);
                var index = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(linkedDocument, cancellationToken).ConfigureAwait(false);

                // See if the index for the other file also contains this same info.  If so, merge the results so the
                // user only sees them as a single hit in the UI.
                if (index.DeclaredSymbolInfoSet.Contains(declaredSymbolInfo))
                    result.Add(linkedDocument.Project.Id);
            }

            result.RemoveDuplicates();
            return result.ToImmutable();
        }

        private static string GetItemKind(DeclaredSymbolInfo declaredSymbolInfo)
        {
            switch (declaredSymbolInfo.Kind)
            {
                case DeclaredSymbolInfoKind.Class:
                case DeclaredSymbolInfoKind.Record:
                    return NavigateToItemKind.Class;
                case DeclaredSymbolInfoKind.RecordStruct:
                    return NavigateToItemKind.Structure;
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

        private static NavigateToMatchKind GetNavigateToMatchKind(in TemporaryArray<PatternMatch> nameMatches)
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
                            lookupTable[(int)DeclaredSymbolInfoKind.RecordStruct] = true;
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
