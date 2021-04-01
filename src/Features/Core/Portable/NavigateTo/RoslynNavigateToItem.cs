// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        public readonly DeclaredSymbolInfo DeclaredSymbolInfo;

        /// <summary>
        /// Will be one of the values from <see cref="NavigateToItemKind"/>.
        /// </summary>
        [DataMember(Order = 4)]
        public readonly string Kind;

        [DataMember(Order = 5)]
        public readonly NavigateToMatchKind MatchKind;

        [DataMember(Order = 6)]
        public readonly bool IsCaseSensitive;

        [DataMember(Order = 7)]
        public readonly ImmutableArray<TextSpan> NameMatchSpans;

        public RoslynNavigateToItem(
            bool isStale,
            DocumentId documentId,
            string combinedProjectName,
            DeclaredSymbolInfo declaredSymbolInfo,
            string kind,
            NavigateToMatchKind matchKind,
            bool isCaseSensitive,
            ImmutableArray<TextSpan> nameMatchSpans)
        {
            IsStale = isStale;
            DocumentId = documentId;
            CombinedProjectName = combinedProjectName;
            DeclaredSymbolInfo = declaredSymbolInfo;
            Kind = kind;
            MatchKind = matchKind;
            IsCaseSensitive = isCaseSensitive;
            NameMatchSpans = nameMatchSpans;
        }

        public async Task<INavigateToSearchResult?> TryCreateSearchResultAsync(Solution solution, CancellationToken cancellationToken)
        {
            if (IsStale)
            {
                // may refer to a document that doesn't exist anymore.  Bail out gracefully in that case.
                var document = solution.GetDocument(DocumentId);
                if (document == null)
                    return null;

                return new NavigateToSearchResult(this, document);
            }
            else
            {
                var document = await solution.GetRequiredDocumentAsync(
                    DocumentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
                return new NavigateToSearchResult(this, document);
            }
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
                    // For partial types, state what file they're in so the user can disambiguate the results.
                    if (_item.DeclaredSymbolInfo.IsPartial)
                    {
                        return IsNonNestedNamedType()
                            ? string.Format(FeaturesResources._0_dash_1, _document.Name, _item.CombinedProjectName)
                            : string.Format(FeaturesResources.in_0_1_2, _item.DeclaredSymbolInfo.ContainerDisplayName, _document.Name, _item.CombinedProjectName);
                    }
                    else
                    {
                        return IsNonNestedNamedType()
                            ? string.Format(FeaturesResources.project_0, _item.CombinedProjectName)
                            : string.Format(FeaturesResources.in_0_project_1, _item.DeclaredSymbolInfo.ContainerDisplayName, _item.CombinedProjectName);
                    }
                }
            }

            private bool IsNonNestedNamedType()
                => !_item.DeclaredSymbolInfo.IsNestedType && IsNamedType();

            private bool IsNamedType()
            {
                switch (_item.DeclaredSymbolInfo.Kind)
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

            string INavigateToSearchResult.Kind => _item.Kind;

            NavigateToMatchKind INavigateToSearchResult.MatchKind => _item.MatchKind;

            bool INavigateToSearchResult.IsCaseSensitive => _item.IsCaseSensitive;

            string INavigateToSearchResult.Name => _item.DeclaredSymbolInfo.Name;

            ImmutableArray<TextSpan> INavigateToSearchResult.NameMatchSpans => _item.NameMatchSpans;

            string INavigateToSearchResult.SecondarySort
            {
                get
                {

                    // For partial types, we break up the file name into pieces.  i.e. If we have
                    // Outer.cs and Outer.Inner.cs  then we add "Outer" and "Outer Inner" to 
                    // the secondary sort string.  That way "Outer.cs" will be weighted above
                    // "Outer.Inner.cs"
                    var fileName = Path.GetFileNameWithoutExtension(_document.FilePath ?? "");

                    using var _ = ArrayBuilder<string>.GetInstance(out var parts);

                    parts.Add(_item.DeclaredSymbolInfo.ParameterCount.ToString("X4"));
                    parts.Add(_item.DeclaredSymbolInfo.TypeParameterCount.ToString("X4"));
                    parts.Add(_item.DeclaredSymbolInfo.Name);
                    parts.AddRange(fileName.Split(s_dotArray));

                    return string.Join(" ", parts);
                }
            }

            string? INavigateToSearchResult.Summary => null;

            INavigableItem INavigateToSearchResult.NavigableItem => this;

            #region INavigableItem

            Glyph INavigableItem.Glyph => GetGlyph(_item.DeclaredSymbolInfo.Kind, _item.DeclaredSymbolInfo.Accessibility);

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
                    TextTags.Text, _item.DeclaredSymbolInfo.Name + _item.DeclaredSymbolInfo.NameSuffix));

            bool INavigableItem.DisplayFileLocation => false;

            /// <summary>
            /// DeclaredSymbolInfos always come from some actual declaration in source.  So they're
            /// never implicitly declared.
            /// </summary>
            bool INavigableItem.IsImplicitlyDeclared => false;

            Document INavigableItem.Document => _document;

            TextSpan INavigableItem.SourceSpan => _item.DeclaredSymbolInfo.Span;

            ImmutableArray<INavigableItem> INavigableItem.ChildItems => ImmutableArray<INavigableItem>.Empty;

            #endregion
        }
    }
}
