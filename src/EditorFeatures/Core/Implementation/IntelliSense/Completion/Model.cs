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
        public ITextSnapshot TriggerSnapshot { get { return _disconnectedBufferGraph.SubjectBufferSnapshot; } }

        public Document TriggerDocument { get; }

        public CompletionList OriginalList { get; }
        public ImmutableArray<PresentationItem> TotalItems { get; }
        public ImmutableArray<PresentationItem> FilteredItems { get; }

        public PresentationItem SelectedItem { get; }

        public ImmutableArray<CompletionItemFilter> CompletionItemFilters { get; }
        public ImmutableDictionary<CompletionItemFilter, bool> FilterState { get; }
        public string FilterText { get; } = "";

        public bool IsHardSelection { get; }
        public bool IsUnique { get; }

        // The item the model will use to represent selecting and interacting with when in suggestion mode.
        // All models always have this property set.
        public PresentationItem DefaultSuggestionModeItem { get; }

        // The suggestion mode item, if any, provided by the completion service.
        public PresentationItem SuggestionModeItem { get; }
        public CompletionTrigger Trigger { get; }
        public bool UseSuggestionMode { get; }

        // When committing a completion item, the span replaced ends at this point.
        public ITrackingPoint CommitTrackingSpanEndPoint { get; }

        public bool DismissIfEmpty { get; }

        private Model(
            Document triggerDocument,
            DisconnectedBufferGraph disconnectedBufferGraph,
            CompletionList originalList,
            ImmutableArray<PresentationItem> totalItems,
            ImmutableArray<PresentationItem> filteredItems,
            PresentationItem selectedItem,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            ImmutableDictionary<CompletionItemFilter, bool> filterState,
            string filterText,
            bool isHardSelection,
            bool isUnique,
            bool useSuggestionMode,
            PresentationItem suggestionModeItem,
            PresentationItem defaultSuggestionModeItem,
            CompletionTrigger trigger,
            ITrackingPoint commitSpanEndPoint,
            bool dismissIfEmpty)
        {
            Contract.ThrowIfFalse(totalItems.Length != 0, "Must have at least one item.");

            this.TriggerDocument = triggerDocument;
            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.OriginalList = originalList;
            this.TotalItems = totalItems;
            this.FilteredItems = filteredItems;
            this.FilterState = filterState;
            this.SelectedItem = selectedItem;
            this.CompletionItemFilters = completionItemFilters;
            this.FilterText = filterText;
            this.IsHardSelection = isHardSelection;
            this.IsUnique = isUnique;
            this.UseSuggestionMode = useSuggestionMode;
            this.SuggestionModeItem = suggestionModeItem;
            this.DefaultSuggestionModeItem = defaultSuggestionModeItem;
            this.Trigger = trigger;
            this.CommitTrackingSpanEndPoint = commitSpanEndPoint;
            this.DismissIfEmpty = dismissIfEmpty;
        }

        public static Model CreateModel(
            Document triggerDocument,
            DisconnectedBufferGraph disconnectedBufferGraph,
            CompletionList originalList,
            CompletionItem selectedItem,
            bool isHardSelection,
            bool isUnique,
            bool useSuggestionMode,
            CompletionTrigger trigger,
            CompletionService completionService,
            Workspace workspace)
        {
            ImmutableArray<PresentationItem> totalItems;
            CompletionItem suggestionModeItem = originalList.SuggestionModeItem;
            PresentationItem suggestionModePresentationItem;
            PresentationItem defaultSuggestionModePresentationItem;

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

            if (completionService != null &&
                workspace != null &&
                workspace.Kind != WorkspaceKind.Interactive && // TODO (https://github.com/dotnet/roslyn/issues/5107): support in interactive
                workspace.Options.GetOption(InternalFeatureOnOffOptions.Snippets) &&
                trigger.Kind != CompletionTriggerKind.Snippets)
            {
                // In order to add snippet expansion notes to completion item descriptions, update
                // all of the provided CompletionItems to DescriptionModifyingCompletionItem which will proxy
                // requests to the original completion items and add the snippet expansion note to
                // the description if necessary. We won't do this if the list was triggered to show
                // snippet shortcuts.

                var totalItemsBuilder = ArrayBuilder<PresentationItem>.GetInstance();
                foreach (var item in originalList.Items)
                {
                    totalItemsBuilder.Add(new DescriptionModifyingPresentationItem(item, completionService));
                }

                totalItems = totalItemsBuilder.ToImmutableAndFree();
                defaultSuggestionModePresentationItem = new DescriptionModifyingPresentationItem(
                    CreateDefaultSuggestionModeItem(), completionService, isSuggestionModeItem: true);
                suggestionModePresentationItem = suggestionModeItem != null ? new DescriptionModifyingPresentationItem(suggestionModeItem, completionService, isSuggestionModeItem: true) : null;
            }
            else
            {
                totalItems = originalList.Items.Select(item => new SimplePresentationItem(item, completionService)).ToImmutableArray<PresentationItem>();
                defaultSuggestionModePresentationItem = new SimplePresentationItem(CreateDefaultSuggestionModeItem(), completionService, isSuggestionModeItem: true);
                suggestionModePresentationItem = suggestionModeItem != null ? new SimplePresentationItem(suggestionModeItem, completionService, isSuggestionModeItem: true) : null;
            }

            var selectedPresentationItem = totalItems.FirstOrDefault(it => it.Item == selectedItem);

            return new Model(
                triggerDocument,
                disconnectedBufferGraph,
                originalList,
                totalItems,
                totalItems,
                selectedPresentationItem,
                actualItemFilters,
                filterState,
                "",
                isHardSelection,
                isUnique,
                useSuggestionMode,
                suggestionModePresentationItem,
                defaultSuggestionModePresentationItem,
                trigger,
                GetDefaultTrackingSpanEnd(originalList.Span, disconnectedBufferGraph),
                originalList.Rules.DismissIfEmpty);
        }

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
        {
            return CompletionItem.Create(displayText: "");
        }

        public bool IsSoftSelection
        {
            get
            {
                return !this.IsHardSelection;
            }
        }

        private Model With(
            Optional<ImmutableArray<PresentationItem>> filteredItems = default(Optional<ImmutableArray<PresentationItem>>),
            Optional<PresentationItem> selectedItem = default(Optional<PresentationItem>),
            Optional<ImmutableDictionary<CompletionItemFilter, bool>> filterState = default(Optional<ImmutableDictionary<CompletionItemFilter, bool>>),
            Optional<string> filterText = default(Optional<string>),
            Optional<bool> isHardSelection = default(Optional<bool>),
            Optional<bool> isUnique = default(Optional<bool>),
            Optional<bool> useSuggestionMode = default(Optional<bool>),
            Optional<PresentationItem> suggestionModeItem = default(Optional<PresentationItem>),
            Optional<ITrackingPoint> commitTrackingSpanEndPoint = default(Optional<ITrackingPoint>))
        {
            var newFilteredItems = filteredItems.HasValue ? filteredItems.Value : FilteredItems;
            var newSelectedItem = selectedItem.HasValue ? selectedItem.Value : SelectedItem;
            var newFilterState = filterState.HasValue ? filterState.Value : FilterState;
            var newFilterText = filterText.HasValue ? filterText.Value : FilterText;
            var newIsHardSelection = isHardSelection.HasValue ? isHardSelection.Value : IsHardSelection;
            var newIsUnique = isUnique.HasValue ? isUnique.Value : IsUnique;
            var newUseSuggestionMode = useSuggestionMode.HasValue ? useSuggestionMode.Value : UseSuggestionMode;
            var newSuggestionModeItem = suggestionModeItem.HasValue ? suggestionModeItem.Value : SuggestionModeItem;
            var newCommitTrackingSpanEndPoint = commitTrackingSpanEndPoint.HasValue ? commitTrackingSpanEndPoint.Value : CommitTrackingSpanEndPoint;

            if (newFilteredItems == FilteredItems &&
                newSelectedItem == SelectedItem &&
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
                TriggerDocument, _disconnectedBufferGraph, OriginalList, TotalItems, newFilteredItems,
                newSelectedItem, CompletionItemFilters, newFilterState, newFilterText,
                newIsHardSelection, newIsUnique, newUseSuggestionMode, newSuggestionModeItem,
                DefaultSuggestionModeItem, Trigger, newCommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithFilteredItems(ImmutableArray<PresentationItem> filteredItems)
        {
            return With(filteredItems: filteredItems, selectedItem: filteredItems.FirstOrDefault());
        }

        public Model WithSelectedItem(PresentationItem selectedItem)
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

        public Model WithSuggestionModeItem(PresentationItem suggestionModeItem)
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
            var endPosition = endPoint.HasValue ? endPoint.Value : currentSpan.End;

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
            string currentSnapshotText;
            if (!textSpanToTextCache.TryGetValue(originalSpan, out currentSnapshotText))
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