// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo
{
    internal sealed partial class NavigateToSearchResultProvider
    {
        private class SearchResult : INavigateToSearchResult
        {
            public string AdditionalInformation => _lazyAdditionalInfo.Value;
            public string Name => _declaredSymbolInfo.Name;
            public string Summary => _lazySummary.Value;

            public string Kind { get; }
            public MatchKind MatchKind { get; }
            public INavigableItem NavigableItem { get; }
            public string SecondarySort { get; }
            public bool IsCaseSensitive { get; }

            private readonly Document _document;
            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<string> _lazyAdditionalInfo;
            private readonly Lazy<string> _lazySummary;

            public SearchResult(Document document, DeclaredSymbolInfo declaredSymbolInfo, string kind, MatchKind matchKind, bool isCaseSensitive, INavigableItem navigableItem)
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
                            return EditorFeaturesResources.Project + document.Project.Name;
                        default:
                            return EditorFeaturesResources.Type + declaredSymbolInfo.ContainerDisplayName;
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
