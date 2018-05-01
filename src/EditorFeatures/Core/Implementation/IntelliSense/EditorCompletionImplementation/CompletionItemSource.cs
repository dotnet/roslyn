// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
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
            EditorCompletion.CompletionTrigger trigger,
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
            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                GetRoslynTrigger(trigger)).ConfigureAwait(false);

            var doNotShowList = false;
            if (completionList == null)
            {
                doNotShowList = true;
                completionList = await completionService.GetCompletionsAsync(
                    document,
                    triggerLocation,
                    RoslynTrigger.Invoke).ConfigureAwait(false);
            }

            if (completionList == null)
            {                
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var filterCache = new Dictionary<string, CompletionFilter>();

            var items = completionList.Items.SelectAsArray(roslynItem =>
            {
                var item = Convert(document, roslynItem, completionService, filterCache);
                item.Properties.AddProperty(TriggerBuffer, triggerLocation.Snapshot.TextBuffer);
                return item;
            });

            return new EditorCompletion.CompletionContext(
                items,
                doNotShowList ? InitialSelectionOption.NoSelection : InitialSelectionOption.RegularSelection,
                completionList.SuggestionModeItem != null
                    ? new SuggestionItemOptions(
                        completionList.SuggestionModeItem.DisplayText,
                        (completionList.SuggestionModeItem.Properties.TryGetValue("Description", out var description)
                            ? description
                            : string.Empty))
                    : null);

            //return new EditorCompletion.CompletionContext(
            //    items,
            //    useSoftSelection: false,
            //    completionList.SuggestionModeItem != null,
            //    completionList.SuggestionModeItem?.DisplayText);
        }

        private static RoslynTrigger GetRoslynTrigger(EditorCompletion.CompletionTrigger trigger)
        {
            RoslynTrigger roslynTrigger = default;
            switch (trigger.Reason)
            {
                case CompletionTriggerReason.Invoke:
                case CompletionTriggerReason.InvokeAndCommitIfUnique:
                    roslynTrigger = RoslynTrigger.Invoke;
                    break;
                case CompletionTriggerReason.Insertion:
                    roslynTrigger = RoslynTrigger.CreateInsertionTrigger(trigger.Character);
                    break;
                case CompletionTriggerReason.Deletion:
                    roslynTrigger = RoslynTrigger.CreateDeletionTrigger(trigger.Character);
                    break;
                case CompletionTriggerReason.Snippets:
                    break;
            }

            return roslynTrigger;
        }

        private EditorCompletion.CompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem,
            CompletionService completionService,
            Dictionary<string, CompletionFilter> filterCache)
        {
            var imageId = roslynItem.Tags.GetGlyph().GetImageId();
            var filters = GetFilters(roslynItem, filterCache);

            if (!roslynItem.Properties.TryGetValue(InsertionText, out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var attributeImages = ImmutableArray<AccessibleImageId>.Empty;
            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            if (supportedPlatforms != null)
            {
                var warningImage = Glyph.CompletionWarning.GetImageId();
                attributeImages = ImmutableArray.Create(
                    new AccessibleImageId(
                        warningImage.Guid,
                        warningImage.Id,
                        "Temporary Automation Name")); // TODO: Get automation names, here and below
            }

            var item = new EditorCompletion.CompletionItem(
                roslynItem.DisplayText,
                this,
                new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name"),
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

        private ImmutableArray<CompletionFilter> GetFilters(RoslynCompletionItem item, Dictionary<string, CompletionFilter> filterCache)
        {
            var result = new List<CompletionFilter>();
            foreach (var filter in CompletionItemFilter.AllFilters)
            {
                if (filter.Matches(item))
                {
                    if (filterCache.ContainsKey(filter.DisplayText))
                    {
                        result.Add(filterCache[filter.DisplayText]);
                    }
                    else
                    {
                        var imageId = filter.Tags.GetGlyph().GetImageId();
                        var itemFilter = new CompletionFilter(
                            filter.DisplayText, 
                            filter.AccessKey.ToString(), 
                            new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name"));
                        filterCache[filter.DisplayText] = itemFilter;
                        result.Add(itemFilter);
                    }
                }
            }

            return result.ToImmutableArray();
        }

        public async Task<object> GetDescriptionAsync(EditorCompletion.CompletionItem item, CancellationToken cancellationToken)
        {
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(RoslynItem, out var roslynItem) ||
                !item.Properties.TryGetProperty<ITextBuffer>(TriggerBuffer, out var triggerBuffer))
            {
                return string.Empty;
            }

            Workspace.TryGetWorkspace(triggerBuffer.AsTextContainer(), out var workspace);


            var documentId = workspace.GetDocumentIdInCurrentContext(triggerBuffer.AsTextContainer());
            var document = workspace.CurrentSolution.GetDocument(documentId);
            var service = document.GetLanguageService<CompletionService>() as CompletionServiceWithProviders;
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
            return (CompletionServiceWithProviders)workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<CompletionService>();
        }

        public Task HandleViewClosedAsync(ITextView view) => Task.CompletedTask;

        public bool TryGetApplicableSpan(char typeChar, SnapshotPoint triggerLocation, out SnapshotSpan applicableSpan)
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

            if (!document.TryGetText(out var sourceText))
            {
                applicableSpan = default;
                return false;
            }

            // TODO: TypeChar of 0 means Invoke or InvokeAndCommitIfUnique. An API update will make this better.
            if (typeChar != 0 && !service.ShouldTriggerCompletion(sourceText, triggerLocation.Position, RoslynTrigger.CreateInsertionTrigger(typeChar)))
            {
                applicableSpan = default;
                return false;
            }

            // TODO: Check CompletionOptions.TriggerOnTyping
            // TODO: Check CompletionOptions.TriggerOnDeletion

            applicableSpan = new SnapshotSpan(triggerLocation.Snapshot, service.GetDefaultCompletionListSpan(sourceText, triggerLocation.Position).ToSpan());
            return true;
        }
    }
}
