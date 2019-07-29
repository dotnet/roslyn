// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Editor.Shared.Options;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal class Model
    {
        private readonly DisconnectedBufferGraph _disconnectedBufferGraph;
        public ITextSnapshot TriggerSnapshot => _disconnectedBufferGraph.SubjectBufferSnapshot;

        public Document TriggerDocument { get; }

        public CompletionList OriginalList { get; }
        public ImmutableArray<CompletionItem> FilteredItems { get; }

        /// <summary>
        /// The currently selected item. Note that this can be null
        /// in VS 15+ if the user uses completion list filters to
        /// hide all the items in the list.
        /// </summary>
        public CompletionItem SelectedItemOpt { get; }

        public ImmutableArray<CompletionItemFilter> CompletionItemFilters { get; }
        public ImmutableDictionary<CompletionItemFilter, bool> FilterState { get; }
        public string FilterText { get; } = "";

        public bool IsHardSelection { get; }
        public bool IsUnique { get; }

        /// <summary>
        /// SuggestionMode item is the "builder" we would display in intellisense.  It's
        /// always non-null, but will only be shown if <see cref="UseSuggestionMode"/> is true.
        /// If it provided by some <see cref="CompletionContext.SuggestionModeItem"/> then we
        /// will use that.  Otherwise, we'll have a simple empty-default item that we'll use.
        /// </summary>
        public CompletionItem SuggestionModeItem { get; }
        public bool UseSuggestionMode { get; }

        public CompletionTrigger Trigger { get; }

        // When committing a completion item, the span replaced ends at this point.
        public ITrackingPoint CommitTrackingSpanEndPoint { get; }

        public bool DismissIfEmpty { get; }

        private Model(
            Document triggerDocument,
            DisconnectedBufferGraph disconnectedBufferGraph,
            CompletionList originalList,
            ImmutableArray<CompletionItem> filteredItems,
            CompletionItem selectedItem,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            ImmutableDictionary<CompletionItemFilter, bool> filterState,
            string filterText,
            bool isHardSelection,
            bool isUnique,
            bool useSuggestionMode,
            CompletionItem suggestionModeItem,
            CompletionTrigger trigger,
            ITrackingPoint commitSpanEndPoint,
            bool dismissIfEmpty)
        {
            Contract.ThrowIfFalse(originalList.Items.Length != 0, "Must have at least one item.");

            this.TriggerDocument = triggerDocument;
            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.OriginalList = originalList;
            this.FilteredItems = filteredItems;
            this.FilterState = filterState;
            this.SelectedItemOpt = selectedItem;
            this.CompletionItemFilters = completionItemFilters;
            this.FilterText = filterText;
            this.IsHardSelection = isHardSelection;
            this.IsUnique = isUnique;
            this.Trigger = trigger;
            this.CommitTrackingSpanEndPoint = commitSpanEndPoint;
            this.DismissIfEmpty = dismissIfEmpty;

            this.UseSuggestionMode = useSuggestionMode;
            this.SuggestionModeItem = suggestionModeItem ?? CreateDefaultSuggestionModeItem();
        }

        public static Model CreateModel(
            Document triggerDocument,
            DisconnectedBufferGraph disconnectedBufferGraph,
            CompletionList originalList,
            bool useSuggestionMode,
            CompletionTrigger trigger)
        {
            var selectedItem = originalList.Items.First();
            var isHardSelection = false;
            var isUnique = false;

            // Get the set of actual filters used by all the completion items 
            // that are in the list.
            var actualFiltersSeen = new HashSet<CompletionItemFilter>();
            foreach (var item in originalList.Items)
            {
                foreach (var filter in CompletionItemFilter.AllFilters)
                {
                    if (filter.Matches(item))
                    {
                        actualFiltersSeen.Add(filter);
                    }
                }
            }

            // The set of filters we'll want to show the user are the filters that are actually
            // used by our completion items.  i.e. there's no reason to show the "field" filter
            // if none of completion items is actually a field.
            var actualItemFilters = CompletionItemFilter.AllFilters.Where(actualFiltersSeen.Contains)
                                                                   .ToImmutableArray();

            // By default we do not filter anything out.
            ImmutableDictionary<CompletionItemFilter, bool> filterState = null;

            return new Model(
                triggerDocument,
                disconnectedBufferGraph,
                originalList,
                originalList.Items,
                selectedItem,
                actualItemFilters,
                filterState,
                "",
                isHardSelection,
                isUnique,
                useSuggestionMode,
                originalList.SuggestionModeItem,
                trigger,
                GetDefaultTrackingSpanEnd(originalList.Span, disconnectedBufferGraph),
                originalList.Rules.DismissIfEmpty);
        }

        public ImmutableArray<CompletionItem> TotalItems => this.OriginalList.Items;

        private static ITrackingPoint GetDefaultTrackingSpanEnd(
            TextSpan defaultTrackingSpanInSubjectBuffer,
            DisconnectedBufferGraph disconnectedBufferGraph)
        {
            var viewSpan = disconnectedBufferGraph.GetSubjectBufferTextSpanInViewBuffer(defaultTrackingSpanInSubjectBuffer);
            return disconnectedBufferGraph.ViewSnapshot.Version.CreateTrackingPoint(
                viewSpan.TextSpan.End,
                PointTrackingMode.Positive);
        }

        private static CompletionItem CreateDefaultSuggestionModeItem()
            => CompletionItem.Create(displayText: "");

        public bool IsSoftSelection
        {
            get
            {
                return !this.IsHardSelection;
            }
        }

        private Model With(
            Optional<ImmutableArray<CompletionItem>> filteredItems = default,
            Optional<CompletionItem> selectedItem = default,
            Optional<ImmutableDictionary<CompletionItemFilter, bool>> filterState = default,
            Optional<string> filterText = default,
            Optional<bool> isHardSelection = default,
            Optional<bool> isUnique = default,
            Optional<bool> useSuggestionMode = default,
            Optional<CompletionItem> suggestionModeItem = default,
            Optional<ITrackingPoint> commitTrackingSpanEndPoint = default)
        {
            var newFilteredItems = filteredItems.HasValue ? filteredItems.Value : FilteredItems;
            var newSelectedItem = selectedItem.HasValue ? selectedItem.Value : SelectedItemOpt;
            var newFilterState = filterState.HasValue ? filterState.Value : FilterState;
            var newFilterText = filterText.HasValue ? filterText.Value : FilterText;
            var newIsHardSelection = isHardSelection.HasValue ? isHardSelection.Value : IsHardSelection;
            var newIsUnique = isUnique.HasValue ? isUnique.Value : IsUnique;
            var newUseSuggestionMode = useSuggestionMode.HasValue ? useSuggestionMode.Value : UseSuggestionMode;
            var newSuggestionModeItem = suggestionModeItem.HasValue ? suggestionModeItem.Value : SuggestionModeItem;
            var newCommitTrackingSpanEndPoint = commitTrackingSpanEndPoint.HasValue ? commitTrackingSpanEndPoint.Value : CommitTrackingSpanEndPoint;

            if (newFilteredItems == FilteredItems &&
                newSelectedItem == SelectedItemOpt &&
                newFilterState == FilterState &&
                newFilterText == FilterText &&
                newIsHardSelection == IsHardSelection &&
                newIsUnique == IsUnique &&
                newUseSuggestionMode == UseSuggestionMode &&
                newSuggestionModeItem == SuggestionModeItem &&
                newCommitTrackingSpanEndPoint == CommitTrackingSpanEndPoint)
            {
                return this;
            }

            return new Model(
                TriggerDocument, _disconnectedBufferGraph, OriginalList, newFilteredItems,
                newSelectedItem, CompletionItemFilters, newFilterState, newFilterText,
                newIsHardSelection, newIsUnique, newUseSuggestionMode, newSuggestionModeItem,
                Trigger, newCommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithFilteredItems(ImmutableArray<CompletionItem> filteredItems)
        {
            return With(filteredItems: filteredItems, selectedItem: filteredItems.FirstOrDefault());
        }

        public Model WithSelectedItem(CompletionItem selectedItem)
        {
            return With(selectedItem: selectedItem);
        }

        public Model WithHardSelection(bool isHardSelection)
        {
            return With(isHardSelection: isHardSelection);
        }

        public Model WithIsUnique(bool isUnique)
        {
            return With(isUnique: isUnique);
        }

        public Model WithSuggestionModeItem(CompletionItem suggestionModeItem)
        {
            return With(suggestionModeItem: suggestionModeItem);
        }

        public Model WithUseSuggestionMode(bool useSuggestionMode)
        {
            return With(useSuggestionMode: useSuggestionMode);
        }

        internal Model WithTrackingSpanEnd(ITrackingPoint trackingSpanEnd)
        {
            return With(commitTrackingSpanEndPoint: new Optional<ITrackingPoint>(trackingSpanEnd));
        }

        internal Model WithFilterState(ImmutableDictionary<CompletionItemFilter, bool> filterState)
        {
            return With(filterState: filterState);
        }

        internal Model WithFilterText(string filterText)
        {
            return With(filterText: filterText);
        }

        internal SnapshotSpan GetCurrentSpanInSnapshot(ViewTextSpan originalSpan, ITextSnapshot textSnapshot)
        {
            var start = _disconnectedBufferGraph.ViewSnapshot.CreateTrackingPoint(originalSpan.TextSpan.Start, PointTrackingMode.Negative).GetPosition(textSnapshot);
            var end = Math.Max(start, this.CommitTrackingSpanEndPoint.GetPosition(textSnapshot));
            return new SnapshotSpan(
                textSnapshot, Span.FromBounds(start, end));
        }

        internal string GetCurrentTextInSnapshot(
            ViewTextSpan originalSpan,
            ITextSnapshot textSnapshot,
            int? endPoint = null)
        {
            var currentSpan = GetCurrentSpanInSnapshot(originalSpan, textSnapshot);

            var startPosition = currentSpan.Start;
            var endPosition = endPoint ?? currentSpan.End;

            // TODO(cyrusn): What to do if the span is empty, or the end comes before the start.
            // Can that even happen?  Not sure, so we'll just be resilient just in case.
            return startPosition <= endPosition
                ? textSnapshot.GetText(Span.FromBounds(startPosition, endPosition))
                : string.Empty;
        }

        internal string GetCurrentTextInSnapshot(
            TextSpan originalSpan,
            ITextSnapshot textSnapshot,
            Dictionary<TextSpan, string> textSpanToTextCache,
            int? endPoint = null)
        {
            if (!textSpanToTextCache.TryGetValue(originalSpan, out var currentSnapshotText))
            {
                var viewSpan = GetViewBufferSpan(originalSpan);
                currentSnapshotText = GetCurrentTextInSnapshot(viewSpan, textSnapshot, endPoint);
                textSpanToTextCache[originalSpan] = currentSnapshotText;
            }

            return currentSnapshotText;
        }

        internal ViewTextSpan GetViewBufferSpan(TextSpan subjectBufferSpan)
        {
            return _disconnectedBufferGraph.GetSubjectBufferTextSpanInViewBuffer(subjectBufferSpan);
        }
    }
}
