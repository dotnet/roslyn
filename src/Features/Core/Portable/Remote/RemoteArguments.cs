// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    #region NavigateTo

    internal class SerializableNavigateToSearchResult
    {
        public string AdditionalInformation;

        public string Kind;
        public NavigateToMatchKind MatchKind;
        public bool IsCaseSensitive;
        public string Name;
        public IList<TextSpan> NameMatchSpans;
        public string SecondarySort;
        public string Summary;

        public SerializableNavigableItem NavigableItem;

        internal static SerializableNavigateToSearchResult Dehydrate(INavigateToSearchResult result)
        {
            return new SerializableNavigateToSearchResult
            {
                AdditionalInformation = result.AdditionalInformation,
                Kind = result.Kind,
                MatchKind = result.MatchKind,
                IsCaseSensitive = result.IsCaseSensitive,
                Name = result.Name,
                NameMatchSpans = result.NameMatchSpans,
                SecondarySort = result.SecondarySort,
                Summary = result.Summary,
                NavigableItem = SerializableNavigableItem.Dehydrate(result.NavigableItem)
            };
        }

        internal INavigateToSearchResult Rehydrate(Solution solution)
        {
            return new NavigateToSearchResult(
                AdditionalInformation, Kind, MatchKind, IsCaseSensitive,
                Name, NameMatchSpans.ToImmutableArrayOrEmpty(),
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

    internal class SerializableNavigableItem
    {
        public Glyph Glyph;

        public IList<TaggedText> DisplayTaggedParts;

        public bool DisplayFileLocation;

        public bool IsImplicitlyDeclared;

        public DocumentId Document;
        public TextSpan SourceSpan;

        public IList<SerializableNavigableItem> ChildItems;

        public static SerializableNavigableItem Dehydrate(INavigableItem item)
        {
            return new SerializableNavigableItem
            {
                Glyph = item.Glyph,
                DisplayTaggedParts = item.DisplayTaggedParts,
                DisplayFileLocation = item.DisplayFileLocation,
                IsImplicitlyDeclared = item.IsImplicitlyDeclared,
                Document = item.Document.Id,
                SourceSpan = item.SourceSpan,
                ChildItems = item.ChildItems.SelectAsArray(Dehydrate)
            };
        }

        public INavigableItem Rehydrate(Solution solution)
        {
            var childItems = ChildItems == null
                ? ImmutableArray<INavigableItem>.Empty
                : ChildItems.SelectAsArray(c => c.Rehydrate(solution));
            return new NavigableItem(
                Glyph, DisplayTaggedParts.ToImmutableArrayOrEmpty(),
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
