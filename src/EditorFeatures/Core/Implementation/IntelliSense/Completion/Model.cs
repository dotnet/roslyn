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

        public CompletionList OriginalList { get; }
        public ImmutableArray<PresentationItem> TotalItems { get; }
        public ImmutableArray<PresentationItem> FilteredItems { get; }

        public PresentationItem SelectedItem { get; }

        public ImmutableArray<CompletionItemFilter> CompletionItemFilters { get; }
        public ImmutableDictionary<CompletionItemFilter, bool> FilterState { get; }
        public IReadOnlyDictionary<CompletionItem, string> CompletionItemToFilterText { get; }

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
            DisconnectedBufferGraph disconnectedBufferGraph,
            CompletionList originalList,
            ImmutableArray<PresentationItem> totalItems,
            ImmutableArray<PresentationItem> filteredItems,
            PresentationItem selectedItem,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            ImmutableDictionary<CompletionItemFilter, bool> filterState,
            IReadOnlyDictionary<CompletionItem, string> completionItemToFilterText,
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

            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.OriginalList = originalList;
            this.TotalItems = totalItems;
            this.FilteredItems = filteredItems;
            this.FilterState = filterState;
            this.SelectedItem = selectedItem;
            this.CompletionItemFilters = completionItemFilters;
            this.CompletionItemToFilterText = completionItemToFilterText;
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

                var totalItemsBuilder = ImmutableArray.CreateBuilder<PresentationItem>();
                foreach (var item in originalList.Items)
                {
                    totalItemsBuilder.Add(new DescriptionModifyingPresentationItem(item, completionService));
                }

                totalItems = totalItemsBuilder.AsImmutable();
                defaultSuggestionModePresentationItem = new DescriptionModifyingPresentationItem(CreateDefaultSuggestionModeItem(originalList.DefaultSpan), completionService, isSuggestionModeItem: true);
                suggestionModePresentationItem = suggestionModeItem != null ? new DescriptionModifyingPresentationItem(suggestionModeItem, completionService, isSuggestionModeItem: true) : null;
            }
            else
            {
                totalItems = originalList.Items.Select(item => new SimplePresentationItem(item, completionService)).ToImmutableArray<PresentationItem>();
                defaultSuggestionModePresentationItem = new SimplePresentationItem(CreateDefaultSuggestionModeItem(originalList.DefaultSpan), completionService, isSuggestionModeItem: true);
                suggestionModePresentationItem = suggestionModeItem != null ? new SimplePresentationItem(suggestionModeItem, completionService, isSuggestionModeItem: true) : null;
            }

            var selectedPresentationItem = totalItems.FirstOrDefault(it => it.Item == selectedItem);

            var completionItemToFilterText= new Dictionary<CompletionItem, string>();

            return new Model(
                disconnectedBufferGraph,
                originalList,
                totalItems,
                totalItems,
                selectedPresentationItem,
                actualItemFilters,
                filterState,
                completionItemToFilterText,
                isHardSelection,
                isUnique,
                useSuggestionMode,
                suggestionModePresentationItem,
                defaultSuggestionModePresentationItem,
                trigger,
                GetDefaultTrackingSpanEnd(originalList.DefaultSpan, disconnectedBufferGraph),
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

        private static CompletionItem CreateDefaultSuggestionModeItem(TextSpan defaultTrackingSpanInSubjectBuffer)
        {
            return CompletionItem.Create(displayText: "", span: defaultTrackingSpanInSubjectBuffer);
        }

        public bool IsSoftSelection
        {
            get
            {
                return !this.IsHardSelection;
            }
        }

        public Model WithFilteredItems(ImmutableArray<PresentationItem> filteredItems)
        {
            return new Model(_disconnectedBufferGraph, OriginalList, TotalItems, filteredItems,
                filteredItems.FirstOrDefault(), CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, 
                IsUnique, UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, 
                Trigger, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithSelectedItem(PresentationItem selectedItem)
        {
            return selectedItem == this.SelectedItem
                ? this
                : new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                     selectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, IsUnique, 
                     UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger, 
                     CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithHardSelection(bool isHardSelection)
        {
            return isHardSelection == this.IsHardSelection
                ? this
                : new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                    SelectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, isHardSelection, IsUnique,
                    UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                    CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithIsUnique(bool isUnique)
        {
            return isUnique == this.IsUnique
                ? this
                : new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                    SelectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, isUnique,
                    UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                    CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithSuggestionModeItem(PresentationItem suggestionModeItem)
        {
            return suggestionModeItem == this.SuggestionModeItem
                ? this
                 : new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                    SelectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, IsUnique, 
                    UseSuggestionMode, suggestionModeItem, DefaultSuggestionModeItem, Trigger,
                    CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithUseSuggestionCompletionMode(bool useSuggestionCompletionMode)
        {
            return useSuggestionCompletionMode == this.UseSuggestionMode
                ? this
                : new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                    SelectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, IsUnique,
                    useSuggestionCompletionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                    CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        internal Model WithTrackingSpanEnd(ITrackingPoint trackingSpanEnd)
        {
            return new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                SelectedItem, CompletionItemFilters, FilterState, CompletionItemToFilterText, IsHardSelection, IsUnique, 
                UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                trackingSpanEnd, DismissIfEmpty);
        }

        internal Model WithFilterState(ImmutableDictionary<CompletionItemFilter, bool> filterState)
        {
            return new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                SelectedItem, CompletionItemFilters, filterState, CompletionItemToFilterText, IsHardSelection, IsUnique,
                UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        internal Model WithCompletionItemToFilterText(IReadOnlyDictionary<CompletionItem, string> completionItemToFilterText)
        {
            return new Model(_disconnectedBufferGraph, OriginalList, TotalItems, FilteredItems,
                SelectedItem, CompletionItemFilters, FilterState, completionItemToFilterText, IsHardSelection, IsUnique,
                UseSuggestionMode, SuggestionModeItem, DefaultSuggestionModeItem, Trigger,
                CommitTrackingSpanEndPoint, DismissIfEmpty);
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
