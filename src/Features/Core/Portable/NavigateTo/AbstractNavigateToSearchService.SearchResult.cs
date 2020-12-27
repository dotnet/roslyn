// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
            private static readonly char[] s_dotArray = { '.' };

            public string Name { get; }
            public string AdditionalInformation { get; }
            public string Kind { get; }
            public NavigateToMatchKind MatchKind { get; }
            public INavigableItem NavigableItem { get; }
            public bool IsCaseSensitive { get; }
            public ImmutableArray<TextSpan> NameMatchSpans { get; }
            public string SecondarySort { get; }

            public string? Summary => null;

            public SearchResult(
                Document document,
                DeclaredSymbolInfo declaredSymbolInfo,
                string kind,
                NavigateToMatchKind matchKind,
                bool isCaseSensitive,
                INavigableItem navigableItem,
                ImmutableArray<TextSpan> nameMatchSpans)
            {
                Name = declaredSymbolInfo.Name;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                NavigableItem = navigableItem;
                NameMatchSpans = nameMatchSpans;
                AdditionalInformation = ComputeAdditionalInfo(document, declaredSymbolInfo);
                SecondarySort = ConstructSecondarySortString(document, declaredSymbolInfo);
            }

            private static string ComputeAdditionalInfo(Document document, DeclaredSymbolInfo info)
            {
                // For partial types, state what file they're in so the user can disambiguate the results.
                if (info.IsPartial)
                {
                    return IsNonNestedNamedType(info)
                        ? string.Format(FeaturesResources._0_1, document.Name, document.Project.Name)
                        : string.Format(FeaturesResources.in_0_1_2, info.ContainerDisplayName, document.Name, document.Project.Name);
                }
                else
                {
                    return IsNonNestedNamedType(info)
                        ? string.Format(FeaturesResources.project_0, document.Project.Name)
                        : string.Format(FeaturesResources.in_0_project_1, info.ContainerDisplayName, document.Project.Name);
                }
            }

            private static bool IsNonNestedNamedType(DeclaredSymbolInfo info)
                => !info.IsNestedType && IsNamedType(info);

            private static bool IsNamedType(DeclaredSymbolInfo info)
            {
                switch (info.Kind)
                {
                    case DeclaredSymbolInfoKind.Class:
                    case DeclaredSymbolInfoKind.Record:
                    case DeclaredSymbolInfoKind.Enum:
                    case DeclaredSymbolInfoKind.Interface:
                    case DeclaredSymbolInfoKind.Module:
                    case DeclaredSymbolInfoKind.Struct:
                        return true;
                    default:
                        return false;
                }
            }

            private static string ConstructSecondarySortString(Document document, DeclaredSymbolInfo declaredSymbolInfo)
            {
                using var _ = ArrayBuilder<string>.GetInstance(out var parts);

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
        }
    }
}
