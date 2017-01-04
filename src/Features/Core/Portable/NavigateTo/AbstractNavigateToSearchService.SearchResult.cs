// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
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
            public ImmutableArray<TextSpan> NameMatchSpans { get; }

            private readonly Document _document;
            private readonly DeclaredSymbolInfo _declaredSymbolInfo;
            private readonly Lazy<string> _lazyAdditionalInfo;
            private readonly Lazy<string> _lazySummary;

            public SearchResult(
                Document document, DeclaredSymbolInfo declaredSymbolInfo, string kind,
                NavigateToMatchKind matchKind, bool isCaseSensitive, INavigableItem navigableItem,
                ImmutableArray<TextSpan> nameMatchSpans)
            {
                _document = document;
                _declaredSymbolInfo = declaredSymbolInfo;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                NavigableItem = navigableItem;
                NameMatchSpans = nameMatchSpans;
                SecondarySort = ConstructSecondarySortString(document, declaredSymbolInfo);

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

            private static readonly char[] s_dotArray = { '.' };

            private static string ConstructSecondarySortString(
                Document document,
                DeclaredSymbolInfo declaredSymbolInfo)
            {
                var parts = ArrayBuilder<string>.GetInstance();
                try
                {
                    parts.Add(declaredSymbolInfo.ParameterCount.ToString("X4"));
                    parts.Add(declaredSymbolInfo.TypeParameterCount.ToString("X4"));
                    parts.Add(declaredSymbolInfo.Name);

                    // For partial types, we break up the file name into pieces.  i.e. If we have
                    // Outer.cs and Outer.Inner.cs  then we add "Outer" and "Outer Inner" to 
                    // the secondary sort string.  That way "Outer.cs" will be weighted above
                    // "Outer.Inner.cs"
                    var fileName = Path.GetFileNameWithoutExtension(document.FilePath ?? "");
                    parts.AddRange(fileName.Split(s_dotArray));

                    return string.Join(" ", parts);
                }
                finally
                {
                    parts.Free();
                }
            }
        }
    }
}