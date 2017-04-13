// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal class SerializableTaggedText
    {
        public string Tag;
        public string Text;

        public static SerializableTaggedText Dehydrate(TaggedText taggedText)
        {
            return new SerializableTaggedText { Tag = taggedText.Tag, Text = taggedText.Text };
        }

        internal static SerializableTaggedText[] Dehydrate(ImmutableArray<TaggedText> displayTaggedParts)
        {
            return displayTaggedParts.Select(Dehydrate).ToArray();
        }

        public TaggedText Rehydrate()
        {
            return new TaggedText(Tag, Text);
        }
    }

    #region NavigateTo

    internal class SerializableNavigateToSearchResult
    {
        public string AdditionalInformation;

        public string Kind;
        public NavigateToMatchKind MatchKind;
        public bool IsCaseSensitive;
        public string Name;
        public TextSpan[] NameMatchSpans;
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
                NameMatchSpans = result.NameMatchSpans.ToArray(),
                SecondarySort = result.SecondarySort,
                Summary = result.Summary,
                NavigableItem = SerializableNavigableItem.Dehydrate(result.NavigableItem)
            };
        }

        internal INavigateToSearchResult Rehydrate(Solution solution)
        {
            return new NavigateToSearchResult(
                AdditionalInformation, Kind, MatchKind, IsCaseSensitive,
                Name, NameMatchSpans.ToImmutableArray(),
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
        public DocumentId Document;
        public TextSpan SourceSpan;
        public Glyph Glyph;
        public SerializableTaggedText[] DisplayTaggedParts;

        public static SerializableNavigableItem Dehydrate(INavigableItem item)
        {
            return new SerializableNavigableItem
            {
                Glyph = item.Glyph,
                DisplayTaggedParts = SerializableTaggedText.Dehydrate(item.DisplayTaggedParts),
                Document = item.Document.Id,
                SourceSpan = item.SourceSpan
            };
        }

        private static SerializableNavigableItem[] Dehydrate(ImmutableArray<INavigableItem> childItems)
            => childItems.Select(Dehydrate).ToArray();

        public INavigableItem Rehydrate(Solution solution)
        {
            return new NavigableItem(
                solution.GetDocument(Document), SourceSpan,
                Glyph, DisplayTaggedParts.Select(p => p.Rehydrate()).ToImmutableArray());
        }
    }

    #endregion
}