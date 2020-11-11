// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    #region NavigateTo

    [DataContract]
    internal readonly struct SerializableNavigateToSearchResult
    {
        [DataMember(Order = 0)]
        public readonly string AdditionalInformation;

        [DataMember(Order = 1)]
        public readonly string Kind;

        [DataMember(Order = 2)]
        public readonly NavigateToMatchKind MatchKind;

        [DataMember(Order = 3)]
        public readonly bool IsCaseSensitive;

        [DataMember(Order = 4)]
        public readonly string Name;

        [DataMember(Order = 5)]
        public readonly ImmutableArray<TextSpan> NameMatchSpans;

        [DataMember(Order = 6)]
        public readonly string SecondarySort;

        [DataMember(Order = 7)]
        public readonly string Summary;

        [DataMember(Order = 8)]
        public readonly SerializableNavigableItem NavigableItem;

        public SerializableNavigateToSearchResult(
            string additionalInformation,
            string kind,
            NavigateToMatchKind matchKind,
            bool isCaseSensitive,
            string name,
            ImmutableArray<TextSpan> nameMatchSpans,
            string secondarySort,
            string summary,
            SerializableNavigableItem navigableItem)
        {
            AdditionalInformation = additionalInformation;
            Kind = kind;
            MatchKind = matchKind;
            IsCaseSensitive = isCaseSensitive;
            Name = name;
            NameMatchSpans = nameMatchSpans;
            SecondarySort = secondarySort;
            Summary = summary;
            NavigableItem = navigableItem;
        }

        internal static SerializableNavigateToSearchResult Dehydrate(INavigateToSearchResult result)
            => new(result.AdditionalInformation,
                   result.Kind,
                   result.MatchKind,
                   result.IsCaseSensitive,
                   result.Name,
                   result.NameMatchSpans,
                   result.SecondarySort,
                   result.Summary,
                   SerializableNavigableItem.Dehydrate(result.NavigableItem));

        internal INavigateToSearchResult Rehydrate(Solution solution)
        {
            return new NavigateToSearchResult(
                AdditionalInformation, Kind, MatchKind, IsCaseSensitive,
                Name, NameMatchSpans,
                SecondarySort, Summary, NavigableItem.Rehydrate(solution));
        }

        private class NavigateToSearchResult : INavigateToSearchResult
        {
            public string AdditionalInformation { get; }
            public string Kind { get; }
            public NavigateToMatchKind MatchKind { get; }
            public bool IsCaseSensitive { get; }
            public string Name { get; }
            public ImmutableArray<TextSpan> NameMatchSpans { get; }
            public string SecondarySort { get; }
            public string Summary { get; }

            public INavigableItem NavigableItem { get; }

            public NavigateToSearchResult(
                string additionalInformation, string kind, NavigateToMatchKind matchKind,
                bool isCaseSensitive, string name, ImmutableArray<TextSpan> nameMatchSpans,
                string secondarySort, string summary, INavigableItem navigableItem)
            {
                AdditionalInformation = additionalInformation;
                Kind = kind;
                MatchKind = matchKind;
                IsCaseSensitive = isCaseSensitive;
                Name = name;
                NameMatchSpans = nameMatchSpans;
                SecondarySort = secondarySort;
                Summary = summary;
                NavigableItem = navigableItem;
            }
        }
    }

    /// <summary>
    /// Note: this is intentionally a class, not struct, to avoid hitting .NET Framework loader bug
    /// that fails to load a struct S declaring a field of type ImmutableArray of S.
    /// </summary>
    [DataContract]
    internal sealed class SerializableNavigableItem
    {
        [DataMember(Order = 0)]
        public readonly Glyph Glyph;

        [DataMember(Order = 1)]
        public readonly ImmutableArray<TaggedText> DisplayTaggedParts;

        [DataMember(Order = 2)]
        public readonly bool DisplayFileLocation;

        [DataMember(Order = 3)]
        public readonly bool IsImplicitlyDeclared;

        [DataMember(Order = 4)]
        public readonly DocumentId Document;

        [DataMember(Order = 5)]
        public readonly TextSpan SourceSpan;

        [DataMember(Order = 6)]
        public readonly ImmutableArray<SerializableNavigableItem> ChildItems;

        public SerializableNavigableItem(
            Glyph glyph,
            ImmutableArray<TaggedText> displayTaggedParts,
            bool displayFileLocation,
            bool isImplicitlyDeclared,
            DocumentId document,
            TextSpan sourceSpan,
            ImmutableArray<SerializableNavigableItem> childItems)
        {
            Glyph = glyph;
            DisplayTaggedParts = displayTaggedParts;
            DisplayFileLocation = displayFileLocation;
            IsImplicitlyDeclared = isImplicitlyDeclared;
            Document = document;
            SourceSpan = sourceSpan;
            ChildItems = childItems;
        }

        public static SerializableNavigableItem Dehydrate(INavigableItem item)
            => new(item.Glyph,
                   item.DisplayTaggedParts,
                   item.DisplayFileLocation,
                   item.IsImplicitlyDeclared,
                   item.Document.Id,
                   item.SourceSpan,
                   item.ChildItems.SelectAsArray(Dehydrate));

        public INavigableItem Rehydrate(Solution solution)
        {
            var childItems = ChildItems == null
                ? ImmutableArray<INavigableItem>.Empty
                : ChildItems.SelectAsArray(c => c.Rehydrate(solution));
            return new NavigableItem(
                Glyph, DisplayTaggedParts,
                DisplayFileLocation, IsImplicitlyDeclared,
                solution.GetDocument(Document),
                SourceSpan,
                childItems);
        }

        private class NavigableItem : INavigableItem
        {
            public Glyph Glyph { get; }
            public ImmutableArray<TaggedText> DisplayTaggedParts { get; }
            public bool DisplayFileLocation { get; }
            public bool IsImplicitlyDeclared { get; }

            public Document Document { get; }
            public TextSpan SourceSpan { get; }

            public ImmutableArray<INavigableItem> ChildItems { get; }

            public NavigableItem(
                Glyph glyph, ImmutableArray<TaggedText> displayTaggedParts,
                bool displayFileLocation, bool isImplicitlyDeclared, Document document, TextSpan sourceSpan, ImmutableArray<INavigableItem> childItems)
            {
                Glyph = glyph;
                DisplayTaggedParts = displayTaggedParts;
                DisplayFileLocation = displayFileLocation;
                IsImplicitlyDeclared = isImplicitlyDeclared;
                Document = document;
                SourceSpan = sourceSpan;
                ChildItems = childItems;
            }
        }
    }

    #endregion
}
