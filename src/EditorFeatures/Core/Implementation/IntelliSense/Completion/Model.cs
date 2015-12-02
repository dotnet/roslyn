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

        public IList<CompletionItem> TotalItems { get; }
        public IList<CompletionItem> FilteredItems { get; }

        public CompletionItem SelectedItem { get; }
        public bool IsHardSelection { get; }
        public bool IsUnique { get; }

        // The CompletionItem the model will use to represent selecting
        // and interacting with the builder. This CompletionItem includes
        // the language specific default tracking span for completion
        // as determined by CompletionUtilities for that language.
        // All models always have a DefaultBuilder set.
        public CompletionItem DefaultBuilder { get; }

        // The builder, if any, provided by the model's completionproviders.
        public CompletionItem Builder { get; }
        public CompletionTriggerInfo TriggerInfo { get; }
        public bool UseSuggestionCompletionMode { get; }

        // When committing a completion item, the span replaced ends at this point.
        public ITrackingPoint CommitTrackingSpanEndPoint { get; }

        public bool DismissIfEmpty { get; }

        private Model(
            DisconnectedBufferGraph disconnectedBufferGraph,
            IList<CompletionItem> totalItems,
            IList<CompletionItem> filteredItems,
            CompletionItem selectedItem,
            bool isHardSelection,
            bool isUnique,
            bool useSuggestionCompletionMode,
            CompletionItem builder,
            CompletionItem defaultBuilder,
            CompletionTriggerInfo triggerInfo,
            ITrackingPoint commitSpanEndPoint,
            bool dismissIfEmpty)
        {
            Contract.ThrowIfNull(selectedItem);
            Contract.ThrowIfFalse(totalItems.Count != 0, "Must have at least one item.");
            Contract.ThrowIfFalse(filteredItems.Count != 0, "Must have at least one filtered item.");
            Contract.ThrowIfFalse(filteredItems.Contains(selectedItem) || defaultBuilder == selectedItem, "Selected item must be in filtered items.");

            _disconnectedBufferGraph = disconnectedBufferGraph;
            this.TotalItems = totalItems;
            this.FilteredItems = filteredItems;
            this.SelectedItem = selectedItem;
            this.IsHardSelection = isHardSelection;
            this.IsUnique = isUnique;
            this.UseSuggestionCompletionMode = useSuggestionCompletionMode;
            this.Builder = builder;
            this.DefaultBuilder = defaultBuilder;
            this.TriggerInfo = triggerInfo;
            this.CommitTrackingSpanEndPoint = commitSpanEndPoint;
            this.DismissIfEmpty = dismissIfEmpty;
        }

        public static Model CreateModel(
            DisconnectedBufferGraph disconnectedBufferGraph,
            TextSpan defaultTrackingSpanInSubjectBuffer,
            ImmutableArray<CompletionItem> totalItems,
            CompletionItem selectedItem,
            bool isHardSelection,
            bool isUnique,
            bool useSuggestionCompletionMode,
            CompletionItem builder,
            CompletionTriggerInfo triggerInfo,
            ICompletionService completionService,
            Workspace workspace)
        {
            var updatedTotalItems = totalItems;
            CompletionItem updatedSelectedItem = selectedItem;
            CompletionItem updatedBuilder = builder;
            CompletionItem updatedDefaultBuilder = GetDefaultBuilder(defaultTrackingSpanInSubjectBuffer);

            if (completionService != null &&
                workspace != null &&
                workspace.Kind != WorkspaceKind.Interactive && // TODO (https://github.com/dotnet/roslyn/issues/5107): support in interactive
                workspace.Options.GetOption(InternalFeatureOnOffOptions.Snippets) &&
                triggerInfo.TriggerReason != CompletionTriggerReason.Snippets)
            {
                // In order to add snippet expansion notes to completion item descriptions, update
                // all of the provided CompletionItems to DescriptionModifyingCompletionItem which will proxy
                // requests to the original completion items and add the snippet expansion note to
                // the description if necessary. We won't do this if the list was triggered to show
                // snippet shortcuts.

                var updatedTotalItemsBuilder = ImmutableArray.CreateBuilder<CompletionItem>();
                updatedSelectedItem = null;

                foreach (var item in totalItems)
                {
                    var updatedItem = new DescriptionModifyingCompletionItem(item, completionService, workspace);
                    updatedTotalItemsBuilder.Add(updatedItem);

                    if (item == selectedItem)
                    {
                        updatedSelectedItem = updatedItem;
                    }
                }

                updatedTotalItems = updatedTotalItemsBuilder.AsImmutable();

                updatedBuilder = null;
                if (builder != null)
                {
                    updatedBuilder = new DescriptionModifyingCompletionItem(builder, completionService, workspace);
                }

                updatedDefaultBuilder = new DescriptionModifyingCompletionItem(
                    GetDefaultBuilder(defaultTrackingSpanInSubjectBuffer),
                    completionService,
                    workspace);
            }

            return new Model(
                disconnectedBufferGraph,
                updatedTotalItems,
                updatedTotalItems,
                updatedSelectedItem,
                isHardSelection,
                isUnique,
                useSuggestionCompletionMode,
                updatedBuilder,
                updatedDefaultBuilder,
                triggerInfo,
                GetDefaultTrackingSpanEnd(defaultTrackingSpanInSubjectBuffer, disconnectedBufferGraph),
                completionService.DismissIfEmpty);
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

        private static CompletionItem GetDefaultBuilder(TextSpan defaultTrackingSpanInSubjectBuffer)
        {
            return new CompletionItem(null, "", defaultTrackingSpanInSubjectBuffer, isBuilder: true);
        }

        public bool IsSoftSelection
        {
            get
            {
                return !this.IsHardSelection;
            }
        }

        public Model WithFilteredItems(IList<CompletionItem> filteredItems)
        {
            return new Model(_disconnectedBufferGraph, TotalItems, filteredItems,
                filteredItems.First(), IsHardSelection, IsUnique, UseSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithSelectedItem(CompletionItem selectedItem)
        {
            return selectedItem == this.SelectedItem
                ? this
                : new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                    selectedItem, IsHardSelection, IsUnique, UseSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithHardSelection(bool isHardSelection)
        {
            return isHardSelection == this.IsHardSelection
                ? this
                : new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                    SelectedItem, isHardSelection, IsUnique, UseSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithIsUnique(bool isUnique)
        {
            return isUnique == this.IsUnique
                ? this
                : new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                    SelectedItem, IsHardSelection, isUnique, UseSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithBuilder(CompletionItem builder)
        {
            return builder == this.Builder
                ? this
                 : new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                    SelectedItem, IsHardSelection, IsUnique, UseSuggestionCompletionMode, builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        public Model WithUseSuggestionCompletionMode(bool useSuggestionCompletionMode)
        {
            return useSuggestionCompletionMode == this.UseSuggestionCompletionMode
                ? this
                 : new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                    SelectedItem, IsHardSelection, IsUnique, useSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, CommitTrackingSpanEndPoint, DismissIfEmpty);
        }

        internal Model WithTrackingSpanEnd(ITrackingPoint trackingSpanEnd)
        {
            return new Model(_disconnectedBufferGraph, TotalItems, FilteredItems,
                SelectedItem, IsHardSelection, IsUnique, UseSuggestionCompletionMode, Builder, DefaultBuilder, TriggerInfo, trackingSpanEnd, DismissIfEmpty);
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
                var viewSpan = GetSubjectBufferFilterSpanInViewBuffer(originalSpan);
                currentSnapshotText = GetCurrentTextInSnapshot(viewSpan, textSnapshot, endPoint);
                textSpanToTextCache[originalSpan] = currentSnapshotText;
            }

            return currentSnapshotText;
        }

        internal ViewTextSpan GetSubjectBufferFilterSpanInViewBuffer(TextSpan filterSpan)
        {
            return _disconnectedBufferGraph.GetSubjectBufferTextSpanInViewBuffer(filterSpan);
        }
    }
}
