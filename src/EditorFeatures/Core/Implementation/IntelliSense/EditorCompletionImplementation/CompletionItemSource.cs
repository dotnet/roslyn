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
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal class CompletionItemSource : IAsyncCompletionItemSource
    {
        private ImmutableArray<char> CommitChars => ImmutableArray.Create(
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');
        private const string RoslynItem = nameof(RoslynItem);
        private const string TriggerSnapshot = nameof(TriggerSnapshot);

        public async Task<EditorCompletion.CompletionContext> GetCompletionContextAsync(
            EditorCompletion.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableSpan,
            CancellationToken token)
        {
            var snapshot = applicableSpan.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();

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

            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                roslynTrigger).ConfigureAwait(false);

            if (completionList == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var imageIdService = document.Project.Solution.Workspace.Services.GetService<IImageIdService>();

            Dictionary<string, CompletionFilter> filterCache = new Dictionary<string, CompletionFilter>();

            var service = GetCompletionService(applicableSpan.Snapshot.TextBuffer.CurrentSnapshot) as CompletionServiceWithProviders;

            var items = completionList.Items.SelectAsArray(roslynItem =>
            {
                var needsCustomCommit = service.GetProvider(roslynItem) is IFeaturesCustomCommitCompletionProvider;

                var item = Convert(document, roslynItem, imageIdService, completionService, filterCache, needsCustomCommit);
                item.Properties.AddProperty(TriggerSnapshot, applicableSpan.Snapshot);
                return item;
            });

            return new EditorCompletion.CompletionContext(
                items,
                useSoftSelection: false, 
                useSuggestionMode: completionList.SuggestionModeItem != null,
                suggestionModeDescription: completionList.SuggestionModeItem?.DisplayText);
        }

        private EditorCompletion.CompletionItem Convert(
            Document document,
            RoslynCompletionItem roslynItem, 
            IImageIdService imageService, 
            CompletionService completionService, 
            Dictionary<string, CompletionFilter> filterCache,
            bool needsCustomCommit)
        {
            var imageId = imageService.GetImageId(roslynItem.Tags.GetGlyph());
            var filters = GetFilters(roslynItem, imageService, filterCache);

            if (!roslynItem.Properties.TryGetValue("InsertionText", out var insertionText))
            {
                insertionText = roslynItem.DisplayText;
            }

            var attributeImages = ImmutableArray<AccessibleImageId>.Empty;
            var supportedPlatforms = SymbolCompletionItem.GetSupportedPlatforms(roslynItem, document.Project.Solution.Workspace);
            if (supportedPlatforms != null && false) // TODO, this makes the completion list invisible
            {
                var warningImage = imageService.GetImageId(Glyph.CompletionWarning);

                attributeImages = SpecializedCollections.SingletonEnumerable(new AccessibleImageId(warningImage.Guid, warningImage.Id, "Temporary Automation Name")).ToImmutableArray();
            }

            var item = new EditorCompletion.CompletionItem(
                roslynItem.DisplayText,
                this,
                new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name"), // TODO
                filters,
                suffix: string.Empty,
                needsCustomCommit,
                insertText: insertionText,
                roslynItem.SortText,
                roslynItem.FilterText,
                attributeImages);

            item.Properties.AddProperty(RoslynItem, roslynItem);
            item.Properties.AddProperty("MatchPriority", roslynItem.Rules.MatchPriority);
            item.Properties.AddProperty("SelectionBehavior", roslynItem.Rules.SelectionBehavior);
            return item;
        }

        private ImmutableArray<CompletionFilter> GetFilters(RoslynCompletionItem item, IImageIdService imageService, Dictionary<string, CompletionFilter> filterCache)
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
                        var imageId = imageService.GetImageId(filter.Tags.GetGlyph());
                        var itemFilter = new CompletionFilter(
                            filter.DisplayText, 
                            filter.AccessKey.ToString(), 
                            new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name")); // TODO
                        filterCache[filter.DisplayText] = itemFilter;
                        result.Add(itemFilter);
                    }
                }
            }

            return result.ToImmutableArray();
        }

        public async Task<object> GetDescriptionAsync(EditorCompletion.CompletionItem item, CancellationToken cancellationToken)
        {
            item.Properties.TryGetProperty<RoslynCompletionItem>("RoslynItem", out var roslynItem);
            item.Properties.TryGetProperty<ITextSnapshot>("TriggerSnapshot", out var triggerSnapshot);

            Workspace.TryGetWorkspace(triggerSnapshot.TextBuffer.AsTextContainer(), out var workspace);

            var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentIdInCurrentContext(triggerSnapshot.TextBuffer.AsTextContainer()));
            var service = document.GetLanguageService<CompletionService>() as CompletionServiceWithProviders;
            var provider = service.GetProvider(roslynItem);

            var description = await provider.GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            // TODO: Snippet invocation part?

            return new ClassifiedTextElement(description.TaggedParts.Select(p => new ClassifiedTextRun(p.Tag.ToClassificationTypeName(), p.Text)));
        }

        public CustomCommitBehavior CustomCommit(
            ITextView view, 
            ITextBuffer buffer, 
            EditorCompletion.CompletionItem item, 
            ITrackingSpan applicableSpan, 
            char commitCharacter, 
            CancellationToken token)
        {
            var service = GetCompletionService(buffer.CurrentSnapshot) as CompletionServiceWithProviders;

            var roslynItem = item.Properties.GetProperty<RoslynCompletionItem>(RoslynItem); // We're using custom data we deposited in GetCompletionContextAsync
            var triggerSnapshot = item.Properties.GetProperty<ITextSnapshot>(TriggerSnapshot);

            using (var edit = buffer.CreateEdit())
            {
                var provider = service.GetProvider(roslynItem);
                Workspace.TryGetWorkspace(triggerSnapshot.TextBuffer.AsTextContainer(), out var workspace);
                var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentIdInCurrentContext(triggerSnapshot.TextBuffer.AsTextContainer()));

                // TODO: We actually want the document from the initial snapshot, not the CurrentSnapshot.
                edit.Delete(applicableSpan.GetSpan(buffer.CurrentSnapshot));

                var change = ((IFeaturesCustomCommitCompletionProvider)provider).GetChangeAsync(document, roslynItem, commitCharacter, CancellationToken.None).WaitAndGetResult(token);
                edit.Replace(change.TextChange.Span.ToSpan(), change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(buffer.CurrentSnapshot, change.NewPosition.Value));
                }
            }

            return CustomCommitBehavior.SuppressFurtherCommandHandlers;
        }

        public ImmutableArray<char> GetPotentialCommitCharacters()
        {
            return CommitChars;
        }

        public bool ShouldCommitCompletion(char typedChar, SnapshotPoint location)
        {
            return CommitChars.Contains(typedChar);
        }

        public SnapshotSpan? ShouldTriggerCompletion(char edit, SnapshotPoint location)
        {
            var text = SourceText.From(location.Snapshot.GetText());
            var service = GetCompletionService(location.Snapshot);
            if (service == null)
            {
                return null;
            }

            // TODO: Edit of 0 means Invoke or InvokeAndCommitIfUnique
            if (edit != 0 && !service.ShouldTriggerCompletion(text, location.Position, RoslynTrigger.CreateInsertionTrigger(edit)))
            {
                return null;
            }

            return new SnapshotSpan(location.Snapshot, service.GetDefaultCompletionListSpan(text, location.Position).ToSpan());
        }

        private CompletionService GetCompletionService(ITextSnapshot snapshot)
        {
            Document document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return null;
            }

            var workspace = document.Project.Solution.Workspace;
            return workspace.Services.GetLanguageServices(LanguageNames.CSharp).GetService<CompletionService>();
        }

        public Task HandleViewClosedAsync(ITextView view) => Task.CompletedTask;
    }
}
