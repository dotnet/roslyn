// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public async Task<IEnumerable<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var results = await NavigateToSymbolFinder.FindNavigableDeclaredSymbolInfos(project, searchPattern, cancellationToken).ConfigureAwait(false);
            return results.Select(r => ConvertResult(r));
        }

        private INavigateToSearchResult ConvertResult(ValueTuple<DeclaredSymbolInfo, Document, IEnumerable<PatternMatch>> result)
        {
            var declaredSymbolInfo = result.Item1;
            var document = result.Item2;
            var matches = result.Item3;
            var matchKind = GetNavigateToMatchKind(matches);

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

        private static MatchKind GetNavigateToMatchKind(IEnumerable<PatternMatch> matchResult)
        {
            // We may have matched the target string in multiple ways, but we'll answer with the
            // "most optimistic" answer
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

            return MatchKind.Regular;
        }
    }
}
