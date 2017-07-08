// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService
    {
        private class SearchResult : INavigateToSearchResult
        {
            public string AdditionalInformation => _lazyAdditionalInfo.Value;
            public string Name => DeclaredSymbolInfo.Name;
            public string Summary { get; }

            public string Kind { get; }
            public NavigateToMatchKind MatchKind { get; }
            public INavigableItem NavigableItem { get; }
            public string SecondarySort { get; }
            public bool IsCaseSensitive { get; }
            public ImmutableArray<TextSpan> NameMatchSpans { get; }

            public readonly Document Document;
            public readonly DeclaredSymbolInfo DeclaredSymbolInfo;

            private readonly Lazy<string> _lazyAdditionalInfo;

            public SearchResult(
                Document document, DeclaredSymbolInfo declaredSymbolInfo, string kind,
                NavigateToMatchKind matchKind, bool isCaseSensitive, INavigableItem navigableItem,
                ImmutableArray<TextSpan> nameMatchSpans)
            {
                Document = document;
                DeclaredSymbolInfo = declaredSymbolInfo;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                NavigableItem = navigableItem;
                NameMatchSpans = nameMatchSpans;
                SecondarySort = ConstructSecondarySortString(document, declaredSymbolInfo);

                _lazyAdditionalInfo = new Lazy<string>(() =>
                {
                    switch (declaredSymbolInfo.Kind)
                    {
                        case DeclaredSymbolInfoKind.Class:
                        case DeclaredSymbolInfoKind.Enum:
                        case DeclaredSymbolInfoKind.Interface:
                        case DeclaredSymbolInfoKind.Module:
                        case DeclaredSymbolInfoKind.Struct:
                            if (!declaredSymbolInfo.IsNestedType)
                            {
                                return string.Format(FeaturesResources.project_0, document.Project.Name);
                            }
                            break;
                    }

                    return string.Format(FeaturesResources.in_0_project_1, declaredSymbolInfo.ContainerDisplayName, document.Project.Name);
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
