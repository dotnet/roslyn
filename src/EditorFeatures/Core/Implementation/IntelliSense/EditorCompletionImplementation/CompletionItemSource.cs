// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal class CompletionItemSource : IAsyncCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string TriggerBuffer = nameof(TriggerBuffer);
        internal const string InsertionText = nameof(InsertionText);
        internal const string MustSetSelection = nameof(MustSetSelection);
        internal const string Description = nameof(Description);

        private static ImmutableArray<ImageElement> s_WarningImageAttributeImagesArray;

        public EditorCompletion.CompletionStartData InitializeCompletion(EditorCompletion.CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return EditorCompletion.CompletionStartData.DoesNotParticipateInCompletion;
            }

            var service = document.GetLanguageService<CompletionService>();
            if (service == null)
            {
                return EditorCompletion.CompletionStartData.DoesNotParticipateInCompletion;
            }

            var sourceText = document.GetTextSynchronously(cancellationToken);

            // TODO: Check CompletionOptions.TriggerOnTyping  https://github.com/dotnet/roslyn/issues/27427
            if (!(trigger.Reason == EditorCompletion.CompletionTriggerReason.Invoke ||
                trigger.Reason == EditorCompletion.CompletionTriggerReason.InvokeAndCommitIfUnique)
                && !service.ShouldTriggerCompletion(sourceText, triggerLocation.Position, GetRoslynTrigger(trigger, triggerLocation)))
            {
                return EditorCompletion.CompletionStartData.DoesNotParticipateInCompletion;
            }

            return new EditorCompletion.CompletionStartData(
                participation: EditorCompletion.CompletionParticipation.ProvidesItems,
                applicableToSpan: new SnapshotSpan(triggerLocation.Snapshot, service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan()));
        }

        public async Task<EditorCompletion.CompletionContext> GetCompletionContextAsync(
            IAsyncCompletionSession session, 
            EditorCompletion.CompletionTrigger trigger, 
            SnapshotPoint triggerLocation, 
            SnapshotSpan applicableToSpan, 
            CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();

            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                GetRoslynTrigger(trigger, triggerLocation)).ConfigureAwait(false);

            if (completionList == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var filterCache = new Dictionary<string, EditorCompletion.CompletionFilter>();

            var itemsBuilder = ImmutableArray.CreateBuilder<EditorCompletion.CompletionItem>();
            foreach (var roslynItem in completionList.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var item = Convert(document, roslynItem, completionService, filterCache);
                item.Properties.AddProperty(TriggerBuffer, triggerLocation.Snapshot.TextBuffer);
                if (!string.IsNullOrEmpty(item.DisplayText))
                {
                    itemsBuilder.Add(item);
                }
            }

            var items = itemsBuilder.ToImmutable();

            var suggestionItemOptions = completionList.SuggestionModeItem != null
                    ? new EditorCompletion.SuggestionItemOptions(
                        completionList.SuggestionModeItem.DisplayText,
                        (completionList.SuggestionModeItem.Properties.TryGetValue(Description, out var description)
                            ? description
                            : string.Empty))
                    : null;

            session.Properties.AddProperty(MustSetSelection, suggestionItemOptions != null);

            return new EditorCompletion.CompletionContext(
                items,
                suggestionItemOptions,
                suggestionItemOptions == null ? EditorCompletion.InitialSelectionHint.RegularSelection : EditorCompletion.InitialSelectionHint.SoftSelection);
        }

        public async Task<object> GetDescriptionAsync(IAsyncCompletionSession session, EditorCompletion.CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(RoslynItem, out var roslynItem) ||
                !item.Properties.TryGetProperty<ITextBuffer>(TriggerBuffer, out var triggerBuffer))
            {
                return null;
            }

            var document = triggerBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var service = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();
            var description = await service.GetProvider(roslynItem).GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            return new ClassifiedTextElement(description.TaggedParts.Select(p => new ClassifiedTextRun(p.Tag.ToClassificationTypeName(), p.Text)));
        }

        private static RoslynTrigger GetRoslynTrigger(EditorCompletion.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            switch (trigger.Reason)
            {
                case EditorCompletion.CompletionTriggerReason.Invoke:
                case EditorCompletion.CompletionTriggerReason.InvokeAndCommitIfUnique:
                    return RoslynTrigger.Invoke;
                case EditorCompletion.CompletionTriggerReason.Insertion:
                    return RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                case EditorCompletion.CompletionTriggerReason.Deletion:
                    return RoslynTrigger.CreateDeletionTrigger(trigger.Character);
                case EditorCompletion.CompletionTriggerReason.Backspace:
                    var snapshotBeforeEdit = trigger.ViewSnapshotBeforeTrigger;
                    char characterRemoved;
                    if (triggerLocation.Position >= 0 && triggerLocation.Position < snapshotBeforeEdit.Length)
                    {
                        // If multiple characters were removed (selection), this finds the first character. 
                        // Maybe it should be re-considered to find the last removed character.
                        characterRemoved = snapshotBeforeEdit[triggerLocation.Position];
                    }
                    else
                    {
                        characterRemoved = (char)0;
                    }

                    return RoslynTrigger.CreateDeletionTrigger(characterRemoved);
                case EditorCompletion.CompletionTriggerReason.SnippetsMode:
                    return new RoslynTrigger(CompletionTriggerKind.Snippets);
            }

            throw ExceptionUtilities.UnexpectedValue(trigger.Reason);
        }

        private EditorCompletion.CompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            CompletionService completionService,
            Dictionary<string, EditorCompletion.CompletionFilter> filterCache)
        {
            var imageId = roslynItem.Tags.GetFirstGlyph().GetImageId();
            var filters = GetFilters(roslynItem, filterCache);

            if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            var attributeImages = supportedPlatforms != null ? GetWarningImageAttributeImagesArray() : ImmutableArray<ImageElement>.Empty;

            var item = new EditorCompletion.CompletionItem(
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

        private static ImmutableArray<ImageElement> GetWarningImageAttributeImagesArray()
        {
            if (s_WarningImageAttributeImagesArray == null)
            {
                var warningImage = Glyph.CompletionWarning.GetImageId();
                s_WarningImageAttributeImagesArray = ImmutableArray.Create(
                    new ImageElement(
                        warningImage,
                        EditorFeaturesResources.Warning));
            }

            return s_WarningImageAttributeImagesArray;
        }

        private ImmutableArray<EditorCompletion.CompletionFilter> GetFilters(RoslynCompletionItem item, Dictionary<string, EditorCompletion.CompletionFilter> filterCache)
        {
            var listBuilder = new ArrayBuilder<EditorCompletion.CompletionFilter>();
            foreach (var filter in CompletionItemFilter.AllFilters)
            {
                if (filter.Matches(item))
                {
                    if (filterCache.TryGetValue(filter.DisplayText, out var applicableFilter))
                    {
                        listBuilder.Add(applicableFilter);
                    }
                    else
                    {
                        var imageId = filter.Tags.GetFirstGlyph().GetImageId();
                        var itemFilter = new EditorCompletion.CompletionFilter(
                            filter.DisplayText,
                            filter.AccessKey.ToString(),
                            new ImageElement(new ImageId(imageId.Guid, imageId.Id), EditorFeaturesResources.Filter));
                        filterCache[filter.DisplayText] = itemFilter;
                        listBuilder.Add(itemFilter);
                    }
                }
            }

            return listBuilder.ToImmutableAndFree();
        }
    }
}
