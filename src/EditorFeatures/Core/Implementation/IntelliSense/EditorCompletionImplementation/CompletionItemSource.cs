// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.Text.Editor;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal class CompletionItemSource : IAsyncCompletionSource
    {
        internal const string RoslynItem = nameof(RoslynItem);
        internal const string TriggerBuffer = nameof(TriggerBuffer);
        internal const string MatchPriority = nameof(MatchPriority);
        internal const string SelectionBehavior = nameof(SelectionBehavior);
        internal const string InsertionText = "InsertionText";

        public async Task<EditorCompletion.CompletionContext> GetCompletionContextAsync(
            EditorCompletion.InitialTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableSpan,
            CancellationToken token)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();

            // TODO: Confirm adherence to VS threading requirements https://github.com/dotnet/roslyn/issues/27418
            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                GetRoslynTrigger(trigger)).ConfigureAwait(false);

            if (completionList == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var filterCache = new Dictionary<string, EditorCompletion.CompletionFilter>();

            var items = completionList.Items.SelectAsArray(roslynItem =>
            {
                token.ThrowIfCancellationRequested();
                var item = Convert(document, roslynItem, completionService, filterCache);
                item.Properties.AddProperty(TriggerBuffer, triggerLocation.Snapshot.TextBuffer);
                return item;
            }).Where(i => !string.IsNullOrEmpty(i.DisplayText)).ToImmutableArray();

            var suggestionItemOptions = completionList.SuggestionModeItem != null
                    ? new EditorCompletion.SuggestionItemOptions(
                        completionList.SuggestionModeItem.DisplayText,
                        (completionList.SuggestionModeItem.Properties.TryGetValue("Description", out var description)
                            ? description
                            : "_")) // TODO: Allow Empty String https://github.com/dotnet/roslyn/issues/27419
                    : null;
            return new EditorCompletion.CompletionContext(
                items,
                suggestionItemOptions,
                suggestionItemOptions == null ? EditorCompletion.InitialSelectionHint.RegularSelection : EditorCompletion.InitialSelectionHint.SoftSelection);
        }

        private static RoslynTrigger GetRoslynTrigger(EditorCompletion.InitialTrigger trigger)
        {
            switch (trigger.Reason)
            {
                case EditorCompletion.InitialTriggerReason.Invoke:
                case EditorCompletion.InitialTriggerReason.InvokeAndCommitIfUnique:
                    return RoslynTrigger.Invoke;
                case EditorCompletion.InitialTriggerReason.Insertion:
                    return RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                case EditorCompletion.InitialTriggerReason.Deletion:
                    return RoslynTrigger.CreateDeletionTrigger(trigger.Character);
                case EditorCompletion.InitialTriggerReason.Snippets:
                    // TODO: Exclusive snippet mode isn't currently supported https://github.com/dotnet/roslyn/issues/27423
                    return default;
                default:
                    return default;
            }
        }

        private EditorCompletion.CompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            CompletionService completionService,
            Dictionary<string, EditorCompletion.CompletionFilter> filterCache)
        {
            var imageId = roslynItem.Tags.GetGlyph().GetImageId();
            var filters = GetFilters(roslynItem, filterCache);

            if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var attributeImages = ImmutableArray<ImageElement>.Empty;
            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            if (supportedPlatforms != null)
            {
                var warningImage = Glyph.CompletionWarning.GetImageId();
                attributeImages = ImmutableArray.Create(
                    new ImageElement(
                        warningImage,
                        "Temporary Automation Name")); // TODO: Get automation names, here and below https://github.com/dotnet/roslyn/issues/27430
            }

            var item = new EditorCompletion.CompletionItem(
                roslynItem.DisplayText,
                this,
                new ImageElement(new ImageId(imageId.Guid, imageId.Id), "Temporary Automation Name"),
                filters,
                suffix: string.Empty,
                insertionText,
                roslynItem.SortText,
                roslynItem.FilterText,
                attributeImages);

            item.Properties.AddProperty(RoslynItem, roslynItem);
            item.Properties.AddProperty(MatchPriority, roslynItem.Rules.MatchPriority);
            item.Properties.AddProperty(SelectionBehavior, roslynItem.Rules.SelectionBehavior);
            return item;
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
                        var imageId = filter.Tags.GetGlyph().GetImageId();
                        var itemFilter = new EditorCompletion.CompletionFilter(
                            filter.DisplayText,
                            filter.AccessKey.ToString(),
                            new ImageElement(new ImageId(imageId.Guid, imageId.Id), "Temporary Automation Name"));
                        filterCache[filter.DisplayText] = itemFilter;
                        listBuilder.Add(itemFilter);
                    }
                }
            }

            return listBuilder.ToImmutableAndFree();
        }

        public async Task<object> GetDescriptionAsync(EditorCompletion.CompletionItem item, CancellationToken cancellationToken)
        {
            // TODO: Should string.Empty returns here be null to avoid an empty popup? https://github.com/dotnet/roslyn/issues/27420

            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(RoslynItem, out var roslynItem) ||
                !item.Properties.TryGetProperty<ITextBuffer>(TriggerBuffer, out var triggerBuffer))
            {
                return string.Empty;
            }

            if (!Workspace.TryGetWorkspace(triggerBuffer.AsTextContainer(), out var workspace))
            {
                return string.Empty;
            }

            var documentId = workspace.GetDocumentIdInCurrentContext(triggerBuffer.AsTextContainer());
            if (documentId == null)
            {
                return string.Empty;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                return string.Empty;
            }

            var service = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();
            var description = await service.GetProvider(roslynItem).GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            return new ClassifiedTextElement(description.TaggedParts.Select(p => new ClassifiedTextRun(p.Tag.ToClassificationTypeName(), p.Text)));
        }

        private CompletionServiceWithProviders GetCompletionService(ITextSnapshot snapshot)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var workspace = document.Project.Solution.Workspace;
            return (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();
        }

        public Task HandleViewClosedAsync(ITextView view) => Task.CompletedTask;

        public bool TryGetApplicableToSpan(char typeChar, SnapshotPoint triggerLocation, out SnapshotSpan applicableSpan, CancellationToken cancellationToken)
        {
            var document = triggerLocation.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                applicableSpan = default;
                return false;
            }

            var service = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();
            if (service == null)
            {
                applicableSpan = default;
                return false;
            }

            // TODO: Use GetTextSynchronously https://github.com/dotnet/roslyn/issues/27425
            if (!document.TryGetText(out var sourceText))
            {
                applicableSpan = default;
                return false;
            }

            // TODO: TypeChar of 0 means Invoke or InvokeAndCommitIfUnique. An API update will make this better. https://github.com/dotnet/roslyn/issues/27426
            if (typeChar != 0 && !service.ShouldTriggerCompletion(sourceText, triggerLocation.Position, RoslynTrigger.CreateInsertionTrigger(typeChar)))
            {
                applicableSpan = default;
                return false;
            }

            // TODO: Check CompletionOptions.TriggerOnTyping  https://github.com/dotnet/roslyn/issues/27427
            // TODO: Check CompletionOptions.TriggerOnDeletion

            applicableSpan = new SnapshotSpan(triggerLocation.Snapshot, service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan());
            return true;
        }
    }
}
