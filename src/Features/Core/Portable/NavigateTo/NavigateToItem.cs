// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    /// <summary>
    /// Data about a navigate to match.  Only intended for use by C# and VB.  Carries enough rich information to
    /// rehydrate everything needed quickly on either the host or remote side.
    /// </summary>
    [DataContract]
    internal readonly struct RoslynNavigateToItem
    {
        [DataMember(Order = 0)]
        public readonly bool IsStale;

        [DataMember(Order = 1)]
        public readonly DocumentId DocumentId;

        [DataMember(Order = 2)]
        public readonly string CombinedProjectName;

        [DataMember(Order = 3)]
        public readonly string DeclaredSymbolInfoName;

        [DataMember(Order = 4)]
        public readonly string DeclaredSymbolInfoNameSuffix;

        [DataMember(Order = 5)]
        public readonly string DeclaredSymbolInfoContainerDisplayName;

        [DataMember(Order = 6)]
        public readonly DeclaredSymbolInfoKind DeclaredSymbolInfoKind;

        [DataMember(Order = 7)]
        public readonly Accessibility DeclaredSymbolInfoAccessibility;

        [DataMember(Order = 8)]
        public readonly TextSpan DeclaredSymbolInfoSourceSpan;

        [DataMember(Order = 9)]
        public readonly bool DeclaredSymbolInfoIsPartial;

        [DataMember(Order = 10)]
        public readonly bool DeclaredSymbolInfoIsNonNestedNamedType;

        [DataMember(Order = 11)]
        public readonly string Kind;

        [DataMember(Order = 12)]
        public readonly NavigateToMatchKind MatchKind;

        [DataMember(Order = 13)]
        public readonly bool IsCaseSensitive;

        [DataMember(Order = 14)]
        public readonly ImmutableArray<TextSpan> NameMatchSpans;

        [DataMember(Order = 15)]
        public readonly string SecondarySort;

        public RoslynNavigateToItem(
            bool isStale,
            DocumentId documentId,
            string combinedProjectName,
            string declaredSymbolInfoName,
            string declaredSymbolInfoNameSuffix,
            string declaredSymbolInfoContainerDisplayName,
            DeclaredSymbolInfoKind declaredSymbolInfoKind,
            Accessibility declaredSymbolInfoAccessibility,
            TextSpan declaredSymbolInfoSourceSpan,
            bool declaredSymbolInfoIsPartial,
            bool declaredSymbolInfoIsNonNestedNamedType,
            string kind,
            NavigateToMatchKind matchKind,
            bool isCaseSensitive,
            ImmutableArray<TextSpan> nameMatchSpans,
            string secondarySort)
        {
            IsStale = isStale;
            DocumentId = documentId;
            CombinedProjectName = combinedProjectName;
            DeclaredSymbolInfoName = declaredSymbolInfoName;
            DeclaredSymbolInfoNameSuffix = declaredSymbolInfoNameSuffix;
            DeclaredSymbolInfoContainerDisplayName = declaredSymbolInfoContainerDisplayName;
            DeclaredSymbolInfoKind = declaredSymbolInfoKind;
            DeclaredSymbolInfoAccessibility = declaredSymbolInfoAccessibility;
            DeclaredSymbolInfoSourceSpan = declaredSymbolInfoSourceSpan;
            DeclaredSymbolInfoIsPartial = declaredSymbolInfoIsPartial;
            DeclaredSymbolInfoIsNonNestedNamedType = declaredSymbolInfoIsNonNestedNamedType;
            Kind = kind;
            MatchKind = matchKind;
            IsCaseSensitive = isCaseSensitive;
            NameMatchSpans = nameMatchSpans;
            SecondarySort = secondarySort;
        }

        public bool TryCreateSearchResult(Solution solution, [NotNullWhen(true)] out INavigateToSearchResult? result)
        {
            var document = solution.GetDocument(DocumentId);
            if (document == null)
            {
                var project = solution.GetProject(DocumentId.ProjectId);
                if (project != null)
                    document = project.TryGetSourceGeneratedDocumentForAlreadyGeneratedId(DocumentId);
            }
            Contract.ThrowIfTrue(document == null && !IsStale, "We should always be able to map back a non-stale result to the solution");

            if (document == null)
            {
                result = null;
                return false;
            }

            result = new NavigateToSearchResult(this, document);
            return true;
        }

        private class NavigateToSearchResult : INavigateToSearchResult, INavigableItem
        {
            private static readonly char[] s_dotArray = { '.' };

            private readonly RoslynNavigateToItem _item;
            private readonly Document _document;

            public NavigateToSearchResult(RoslynNavigateToItem item, Document document)
            {
                _item = item;
                _document = document;
            }

            string INavigateToSearchResult.AdditionalInformation
            {
                get
                {
                    //private static string ComputeAdditionalInfo(string documentName, string combinedProjectName, DeclaredSymbolInfo info)
                    {
                        // For partial types, state what file they're in so the user can disambiguate the results.
                        if (_item.DeclaredSymbolInfoIsPartial)
                        {
                            return _item.DeclaredSymbolInfoIsNonNestedNamedType
                                ? string.Format(FeaturesResources._0_dash_1, _document.Name, _item.CombinedProjectName)
                                : string.Format(FeaturesResources.in_0_1_2, _item.DeclaredSymbolInfoContainerDisplayName, _document.Name, _item.CombinedProjectName);
                        }
                        else
                        {
                            return _item.DeclaredSymbolInfoIsNonNestedNamedType
                                ? string.Format(FeaturesResources.project_0, _item.CombinedProjectName)
                                : string.Format(FeaturesResources.in_0_project_1, _item.DeclaredSymbolInfoContainerDisplayName, _item.CombinedProjectName);
                        }
                    }
                }
            }

            string INavigateToSearchResult.Kind => _item.Kind;

            NavigateToMatchKind INavigateToSearchResult.MatchKind => _item.MatchKind;

            bool INavigateToSearchResult.IsCaseSensitive => _item.IsCaseSensitive;

            string INavigateToSearchResult.Name => _item.DeclaredSymbolInfoName;

            ImmutableArray<TextSpan> INavigateToSearchResult.NameMatchSpans => _item.NameMatchSpans;

            string INavigateToSearchResult.SecondarySort
            {
                get
                {

                    // For partial types, we break up the file name into pieces.  i.e. If we have
                    // Outer.cs and Outer.Inner.cs  then we add "Outer" and "Outer Inner" to 
                    // the secondary sort string.  That way "Outer.cs" will be weighted above
                    // "Outer.Inner.cs"
                    using var _ = ArrayBuilder<string>.GetInstance(out var parts);
                    parts.Add(_item.SecondarySort);

                    var fileName = Path.GetFileNameWithoutExtension(_document.FilePath ?? "");
                    parts.AddRange(fileName.Split(s_dotArray));

                    return string.Join(" ", parts);
                }
            }

            string? INavigateToSearchResult.Summary => null;

            INavigableItem INavigateToSearchResult.NavigableItem => this;

            #region INavigableItem

            Glyph INavigableItem.Glyph => GetGlyph(_item.DeclaredSymbolInfoKind, _item.DeclaredSymbolInfoAccessibility);

            private static Glyph GetPublicGlyph(DeclaredSymbolInfoKind kind)
                => kind switch
                {
                    DeclaredSymbolInfoKind.Class => Glyph.ClassPublic,
                    DeclaredSymbolInfoKind.Constant => Glyph.ConstantPublic,
                    DeclaredSymbolInfoKind.Constructor => Glyph.MethodPublic,
                    DeclaredSymbolInfoKind.Delegate => Glyph.DelegatePublic,
                    DeclaredSymbolInfoKind.Enum => Glyph.EnumPublic,
                    DeclaredSymbolInfoKind.EnumMember => Glyph.EnumMemberPublic,
                    DeclaredSymbolInfoKind.Event => Glyph.EventPublic,
                    DeclaredSymbolInfoKind.ExtensionMethod => Glyph.ExtensionMethodPublic,
                    DeclaredSymbolInfoKind.Field => Glyph.FieldPublic,
                    DeclaredSymbolInfoKind.Indexer => Glyph.PropertyPublic,
                    DeclaredSymbolInfoKind.Interface => Glyph.InterfacePublic,
                    DeclaredSymbolInfoKind.Method => Glyph.MethodPublic,
                    DeclaredSymbolInfoKind.Module => Glyph.ModulePublic,
                    DeclaredSymbolInfoKind.Property => Glyph.PropertyPublic,
                    DeclaredSymbolInfoKind.Struct => Glyph.StructurePublic,
                    _ => Glyph.ClassPublic,
                };

            private static Glyph GetGlyph(DeclaredSymbolInfoKind kind, Accessibility accessibility)
            {
                // Glyphs are stored in this order:
                //  ClassPublic,
                //  ClassProtected,
                //  ClassPrivate,
                //  ClassInternal,

                var rawGlyph = GetPublicGlyph(kind);

                switch (accessibility)
                {
                    case Accessibility.Private:
                        rawGlyph += (Glyph.ClassPrivate - Glyph.ClassPublic);
                        break;
                    case Accessibility.Internal:
                        rawGlyph += (Glyph.ClassInternal - Glyph.ClassPublic);
                        break;
                    case Accessibility.Protected:
                    case Accessibility.ProtectedOrInternal:
                    case Accessibility.ProtectedAndInternal:
                        rawGlyph += (Glyph.ClassProtected - Glyph.ClassPublic);
                        break;
                }

                return rawGlyph;
            }

            ImmutableArray<TaggedText> INavigableItem.DisplayTaggedParts
                => ImmutableArray.Create(new TaggedText(
                    TextTags.Text, _item.DeclaredSymbolInfoName + _item.DeclaredSymbolInfoNameSuffix));

            bool INavigableItem.DisplayFileLocation => false;

            /// <summary>
            /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
            /// never implicitly declared.
            /// </summary>
            bool INavigableItem.IsImplicitlyDeclared => false;

            Document INavigableItem.Document => _document;

            TextSpan INavigableItem.SourceSpan => _item.DeclaredSymbolInfoSourceSpan;

            ImmutableArray<INavigableItem> INavigableItem.ChildItems => ImmutableArray<INavigableItem>.Empty;

            #endregion
        }
    }
}
