// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using AsyncCompletionData = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.AsyncCompletion
{
    internal class Source : IAsyncCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string TriggerSnapshot = nameof(TriggerSnapshot);
        internal const string InsertionText = nameof(InsertionText);
        internal const string HasSuggestionItemOptions = nameof(HasSuggestionItemOptions);
        internal const string Description = nameof(Description);

        private static readonly ImmutableArray<ImageElement> s_WarningImageAttributeImagesArray = 
            ImmutableArray.Create(new ImageElement(Glyph.CompletionWarning.GetImageId(), EditorFeaturesResources.Warning_image_element_automation_name));

        public AsyncCompletionData.CompletionStartData InitializeCompletion(AsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
            }

            var service = document.GetLanguageService<CompletionService>();
            if (service == null)
            {
                return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
            }

            var sourceText = document.GetTextSynchronously(cancellationToken);

            // TODO: Check CompletionOptions.TriggerOnTyping  https://github.com/dotnet/roslyn/issues/27427
            if (trigger.Reason != AsyncCompletionData.CompletionTriggerReason.Invoke &&
                trigger.Reason != AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique && 
                !service.ShouldTriggerCompletion(sourceText, triggerLocation.Position, GetRoslynTrigger(trigger, triggerLocation)))
            {
                return AsyncCompletionData.CompletionStartData.DoesNotParticipateInCompletion;
            }

            return new AsyncCompletionData.CompletionStartData(
                participation: AsyncCompletionData.CompletionParticipation.ProvidesItems,
                applicableToSpan: new SnapshotSpan(triggerLocation.Snapshot, service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan()));
        }

        public async Task<AsyncCompletionData.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session, 
            AsyncCompletionData.CompletionTrigger trigger, 
            SnapshotPoint triggerLocation, 
            SnapshotSpan applicableToSpan, 
            CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new AsyncCompletionData.CompletionContext(ImmutableArray<VSCompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();

            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                GetRoslynTrigger(trigger, triggerLocation)).ConfigureAwait(false);

            if (completionList == null)
            {
                return new AsyncCompletionData.CompletionContext(ImmutableArray<VSCompletionItem>.Empty);
            }

            var filterCache = new Dictionary<string, AsyncCompletionData.CompletionFilter>();

            var itemsBuilder = new ArrayBuilder<VSCompletionItem>();
            foreach (var roslynItem in completionList.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = Convert(document, roslynItem, completionService, filterCache);

                // Have to store the snapshot to reuse it in some projections related scenarios
                // where data and session in further calls are able to provide other snapshots.
                item.Properties.AddProperty(TriggerSnapshot, triggerLocation.Snapshot);
                itemsBuilder.Add(item);
            }

            var items = itemsBuilder.ToImmutableAndFree();

            var suggestionItemOptions = completionList.SuggestionModeItem != null
                    ? new AsyncCompletionData.SuggestionItemOptions(
                        completionList.SuggestionModeItem.DisplayText,
                        (completionList.SuggestionModeItem.Properties.TryGetValue(Description, out var description)
                            ? description
                            : string.Empty))
                    : null;

            // This is a code supporting legacy completion scenarios:
            // If there are suggestionItemOptions, then later HandleNormalFiltering should set selection to SoftSelection.
            session.Properties.AddProperty(HasSuggestionItemOptions, suggestionItemOptions != null);

            return new AsyncCompletionData.CompletionContext(
                items,
                suggestionItemOptions,
                suggestionItemOptions == null ? AsyncCompletionData.InitialSelectionHint.RegularSelection : AsyncCompletionData.InitialSelectionHint.SoftSelection);
        }

        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, VSCompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(RoslynItem, out var roslynItem) ||
                !item.Properties.TryGetProperty<ITextSnapshot>(TriggerSnapshot, out var triggerSnapshot))
            {
                return null;
            }

            var document = triggerSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var service = document.GetLanguageService<CompletionService>();
            var description = await service.GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            return new ClassifiedTextElement(description.TaggedParts.Select(p => new ClassifiedTextRun(p.Tag.ToClassificationTypeName(), p.Text)));
        }

        private static RoslynTrigger GetRoslynTrigger(AsyncCompletionData.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            switch (trigger.Reason)
            {
                case AsyncCompletionData.CompletionTriggerReason.Invoke:
                case AsyncCompletionData.CompletionTriggerReason.InvokeAndCommitIfUnique:
                    return RoslynTrigger.Invoke;
                case AsyncCompletionData.CompletionTriggerReason.Insertion:
                    return RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                case AsyncCompletionData.CompletionTriggerReason.Deletion:
                    return RoslynTrigger.CreateDeletionTrigger(trigger.Character);
                case AsyncCompletionData.CompletionTriggerReason.Backspace:
                    var snapshotBeforeEdit = trigger.ViewSnapshotBeforeTrigger;
                    char characterRemoved;
                    if (triggerLocation.Position >= 0 && triggerLocation.Position < snapshotBeforeEdit.Length)
                    {
                        // If multiple characters were removed (selection), this finds the first character from the left. 
                        characterRemoved = snapshotBeforeEdit[triggerLocation.Position];
                    }
                    else
                    {
                        characterRemoved = (char)0;
                    }

                    return RoslynTrigger.CreateDeletionTrigger(characterRemoved);
                case AsyncCompletionData.CompletionTriggerReason.SnippetsMode:
                    return new RoslynTrigger(CompletionTriggerKind.Snippets);
            }

            FatalError.ReportWithoutCrash(ExceptionUtilities.UnexpectedValue(trigger.Reason));
            return RoslynTrigger.Invoke;
        }

        private VSCompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            CompletionService completionService,
            Dictionary<string, AsyncCompletionData.CompletionFilter> filterCache)
        {
            var imageId = roslynItem.Tags.GetFirstGlyph().GetImageId();
            var filters = GetFilters(roslynItem, filterCache);
            
            // roslynItem generated by providers can contain an insertionText in a property bag.
            // Try using it first.
            if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            var attributeImages = supportedPlatforms != null ? s_WarningImageAttributeImagesArray : ImmutableArray<ImageElement>.Empty;

            var item = new VSCompletionItem(
                displayText: roslynItem.DisplayText,
                source: this,
                icon: new ImageElement(new ImageId(imageId.Guid, imageId.Id), roslynItem.DisplayText),
                filters: filters,
                suffix: string.Empty,
                insertText: insertionText,
                sortText: roslynItem.SortText,
                filterText: roslynItem.FilterText,
                attributeIcons: attributeImages);

            item.Properties.AddProperty(RoslynItem, roslynItem);
            return item;
        }

        private ImmutableArray<AsyncCompletionData.CompletionFilter> GetFilters(RoslynCompletionItem item, Dictionary<string, AsyncCompletionData.CompletionFilter> filterCache)
        {
            var listBuilder = new ArrayBuilder<AsyncCompletionData.CompletionFilter>();
            foreach (var filter in CompletionItemFilter.AllFilters)
            {
                if (filter.Matches(item))
                {
                    if (!filterCache.TryGetValue(filter.DisplayText, out var itemFilter))
                    {
                        var imageId = filter.Tags.GetFirstGlyph().GetImageId();
                        itemFilter = new AsyncCompletionData.CompletionFilter(
                            filter.DisplayText,
                            filter.AccessKey.ToString(),
                            new ImageElement(new ImageId(imageId.Guid, imageId.Id), EditorFeaturesResources.Filter_image_element_automation_name));
                        filterCache[filter.DisplayText] = itemFilter;
                    }

                    listBuilder.Add(itemFilter);
                }
            }

            return listBuilder.ToImmutableAndFree();
        }
    }
}
