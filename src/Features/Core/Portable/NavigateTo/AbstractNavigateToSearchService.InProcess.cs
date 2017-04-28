// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        public static Task<ImmutableArray<INavigateToSearchResult>> SearchProjectInCurrentProcessAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            return FindNavigableDeclaredSymbolInfosAsync(
                project, searchDocument: null, pattern: searchPattern, cancellationToken: cancellationToken);
        }

        public static Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentInCurrentProcessAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            return FindNavigableDeclaredSymbolInfosAsync(
                document.Project, document, searchPattern, cancellationToken);
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> FindNavigableDeclaredSymbolInfosAsync(
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
                    return await FindNavigableDeclaredSymbolInfosAsync(
                        project, searchDocument, nameMatcher, containerMatcherOpt,
                        nameMatches, containerMatches, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    nameMatches.Free();
                    containerMatches.Free();
                }
            }
        }

        private static async Task<ImmutableArray<INavigateToSearchResult>> FindNavigableDeclaredSymbolInfosAsync(
            Project project, Document searchDocument,
            PatternMatcher nameMatcher, PatternMatcher containerMatcherOpt,
            ArrayBuilder<PatternMatch> nameMatches, ArrayBuilder<PatternMatch> containerMatches,
            CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<INavigateToSearchResult>.GetInstance();
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
            }

            return result.ToImmutableAndFree();
        }

        private static INavigateToSearchResult ConvertResult(
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