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
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Storage;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal abstract partial class AbstractNavigateToSearchService
{
    private static readonly ImmutableArray<(PatternMatchKind roslynKind, NavigateToMatchKind vsKind)> s_kindPairs =
        [
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

            // LowercaseSubstring is the weakest non-fuzzy PatternMatchKind (an all-lowercase pattern found
            // inside a candidate at a non-word-boundary, e.g. "line" in "Readline"). NavigateToMatchKind has
            // no dedicated bucket for it, so we map it to Fuzzy as the closest available quality tier.
            (PatternMatchKind.LowercaseSubstring, NavigateToMatchKind.Fuzzy),
        ];

    /// <summary>
    /// Determines the name and container from a search pattern, using regex-aware splitting when
    /// the pattern contains regex metacharacters. Also compiles a <see cref="RegexQuery"/> for
    /// pre-filtering when the pattern is a regex. Returns <see langword="null"/> if the pattern
    /// is detected as regex but is invalid or has no extractable literals for pre-filtering
    /// (e.g. <c>.*</c>), since we refuse to run a regex search that can't be narrowed down.
    /// </summary>
    private static SearchPatternInfo? ProcessSearchPattern(string searchPattern)
    {
        if (RegexPatternDetector.IsRegexPattern(searchPattern))
        {
            var sequence = VirtualCharSequence.Create(0, searchPattern);
            var tree = RegexParser.TryParse(sequence, RegexOptions.None);
            if (tree is not { Diagnostics: [] })
                return null;

            var (container, name) = RegexPatternDetector.SplitOnContainerDot(searchPattern, tree);

            // Reuse the already-parsed tree when the full pattern is the name (no split).
            // When a split occurred, the name is a substring that needs its own parse.
            var regexQuery = container is null
                ? RegexQueryCompiler.Compile(tree)
                : RegexQueryCompiler.Compile(name);

            // Compile returns null if the regex is invalid or has no extractable literals.
            // We only run regex search when the compiled query tree can genuinely filter
            // documents. After optimization, None never appears as a child of Any (it poisons
            // the disjunction) or All (it's pruned as vacuously true), and the compiler only
            // emits Literal nodes for strings of 2+ characters (which produce real bigram
            // checks). So a non-null result guarantees every Literal in the tree is reachable
            // and can reject documents — the pre-filter will never degenerate to "accept
            // everything."
            if (regexQuery is null)
                return null;

            return new SearchPatternInfo(name, container, regexQuery);
        }

        var (patternName, containerOpt) = PatternMatcher.GetNameAndContainer(searchPattern);
        return new SearchPatternInfo(patternName, containerOpt, RegexQuery: null);
    }

    private static async ValueTask SearchSingleDocumentAsync(
        Document document,
        SearchPatternInfo patternInfo,
        DeclaredSymbolInfoKindSet kinds,
        Action<RoslynNavigateToItem> onItemFound,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        // First, load the lightweight filter index to check if this document could possibly match.
        // This avoids loading the much larger TopLevelSyntaxTreeIndex for non-matching documents.
        var filterIndex = await NavigateToSearchIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
        if (!CouldContainMatch(filterIndex, patternInfo, out var matchKinds))
            return;

        // The filter passed — now load the full index with all declared symbols.
        var index = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
        using var _ = ArrayBuilder<(TopLevelSyntaxTreeIndex, ProjectId)>.GetInstance(out var linkedIndices);

        foreach (var linkedDocumentId in document.GetLinkedDocumentIds())
        {
            var linkedDocument = document.Project.Solution.GetRequiredDocument(linkedDocumentId);
            var linkedIndex = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(linkedDocument, cancellationToken).ConfigureAwait(false);
            linkedIndices.Add((linkedIndex, linkedDocumentId.ProjectId));
        }

        ProcessIndex(
            DocumentKey.ToDocumentKey(document), document, patternInfo, kinds,
            matchKinds, index, linkedIndices, onItemFound, cancellationToken);
    }

    private static bool CouldContainMatch(
        NavigateToSearchIndex filterIndex,
        SearchPatternInfo patternInfo,
        out PatternMatcherKind matchKinds)
    {
        if (patternInfo.RegexQuery is { } regexQuery)
            matchKinds = filterIndex.RegexQueryCheckPasses(regexQuery) ? PatternMatcherKind.Standard : PatternMatcherKind.None;
        else
            matchKinds = filterIndex.CouldContainNavigateToMatch(patternInfo.Name, patternInfo.Container);

        return matchKinds != PatternMatcherKind.None;
    }

    private static void ProcessIndex(
        DocumentKey documentKey,
        Document? document,
        SearchPatternInfo patternInfo,
        DeclaredSymbolInfoKindSet kinds,
        PatternMatcherKind matchKinds,
        TopLevelSyntaxTreeIndex index,
        ArrayBuilder<(TopLevelSyntaxTreeIndex, ProjectId)>? linkedIndices,
        Action<RoslynNavigateToItem> onItemFound,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        using var nameMatcher = PatternMatcher.CreateNameMatcher(
            patternInfo.Name, patternInfo.IsRegex, includeMatchedSpans: true, matchKinds);
        if (nameMatcher is null)
            return;

        using var containerMatcher = PatternMatcher.CreateContainerMatcher(
            patternInfo.Container, patternInfo.IsRegex, includeMatchedSpans: true);

        foreach (var declaredSymbolInfo in index.DeclaredSymbolInfos)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // Namespaces are never returned in nav-to as they're too common and have too many locations.
            if (declaredSymbolInfo.Kind == DeclaredSymbolInfoKind.Namespace)
                continue;

            using var nameMatches = TemporaryArray<PatternMatch>.Empty;
            using var containerMatches = TemporaryArray<PatternMatch>.Empty;

            if (kinds.Contains(declaredSymbolInfo.Kind) &&
                nameMatcher.AddMatches(declaredSymbolInfo.Name, ref nameMatches.AsRef()) &&
                containerMatcher?.AddMatches(declaredSymbolInfo.FullyQualifiedContainerName, ref containerMatches.AsRef()) != false)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // See if we have a match in a linked file.  If so, see if we have the same match in
                // other projects that this file is linked in.  If so, include the full set of projects
                // the match is in so we can display that well in the UI.
                //
                // We can only do this in the case where the solution is loaded and thus we can examine
                // the relationship between this document and the other documents linked to it.  In the
                // case where the solution isn't fully loaded and we're just reading in cached data, we
                // don't know what other files we're linked to and can't merge results in this fashion.
                var additionalMatchingProjects = GetAdditionalProjectsWithMatch(
                    document, declaredSymbolInfo, linkedIndices);

                var result = ConvertResult(
                    documentKey, document, declaredSymbolInfo, nameMatches, containerMatches, additionalMatchingProjects);
                onItemFound(result);
            }
        }
    }

    private static RoslynNavigateToItem ConvertResult(
        DocumentKey documentKey,
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

        using var allPatternMatches = TemporaryArray<PatternMatch>.Empty;
        allPatternMatches.AddRange(containerMatches);
        allPatternMatches.AddRange(nameMatches);

        // If we were not given a Document instance, then we're finding matches in cached data
        // and thus could be 'stale'.
        return new RoslynNavigateToItem(
            isStale: document == null,
            documentKey,
            additionalMatchingProjects,
            declaredSymbolInfo,
            kind,
            matchKind,
            isCaseSensitive,
            matchedSpans.ToImmutableAndClear(),
            allPatternMatches.ToImmutableAndClear());
    }

    private static ImmutableArray<ProjectId> GetAdditionalProjectsWithMatch(
        Document? document,
        DeclaredSymbolInfo declaredSymbolInfo,
        ArrayBuilder<(TopLevelSyntaxTreeIndex, ProjectId)>? linkedIndices)
    {
        if (document == null || linkedIndices is null || linkedIndices.Count == 0)
            return [];

        using var result = TemporaryArray<ProjectId>.Empty;

        foreach (var (index, projectId) in linkedIndices)
        {
            // See if the index for the other file also contains this same info.  If so, merge the results so the
            // user only sees them as a single hit in the UI.
            if (index.DeclaredSymbolInfoSet.Contains(declaredSymbolInfo))
                result.Add(projectId);
        }

        return result.ToImmutableAndClear();
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
            // Tracked by https://github.com/dotnet/roslyn/issues/82607
            // Consider having a separate NavigateToItemKind category for unions
            case DeclaredSymbolInfoKind.Union:
                return NavigateToItemKind.Structure;
            case DeclaredSymbolInfoKind.Operator:
                return NavigateToItemKind.OtherSymbol;
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

            var lookupTable = new bool[Enum.GetValues<DeclaredSymbolInfoKind>().Length];
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
                        lookupTable[(int)DeclaredSymbolInfoKind.Union] = true;
                        break;

                    default:
                        // Not a recognized symbol info kind
                        break;
                }
            }

            _lookupTable = [.. lookupTable];
        }

        public bool Contains(DeclaredSymbolInfoKind item)
        {
            return (int)item < _lookupTable.Length
                && _lookupTable[(int)item];
        }
    }
}
