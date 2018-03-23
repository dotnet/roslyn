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

using Roslyn.Utilities;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal class CSharpCompletionItemSource : AbstractCompletionItemSource
    {
        internal override bool PassEnterThroughToBuffer() => false;
    }

    internal class VisualBasicCompletionItemSource : AbstractCompletionItemSource
    {
        internal override bool PassEnterThroughToBuffer() => true;
    }

    internal abstract class AbstractCompletionItemSource : IAsyncCompletionSource
    {
        internal abstract bool PassEnterThroughToBuffer();

        private ImmutableArray<char> CommitChars => ImmutableArray.Create(
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');

        private const string RoslynItem = nameof(RoslynItem);
        private const string TriggerBuffer = nameof(TriggerBuffer);
        private const string MatchPriority = nameof(MatchPriority);
        private const string SelectionBehavior = nameof(SelectionBehavior);

        public async Task<EditorCompletion.CompletionContext> GetCompletionContextAsync(
            EditorCompletion.CompletionTrigger trigger,
            SnapshotPoint triggerLocation,
            SnapshotSpan applicableSpan,
            CancellationToken token)
        {
            var snapshot = triggerLocation.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var completionService = document.GetLanguageService<CompletionService>();
            var completionList = await completionService.GetCompletionsAsync(
                document,
                triggerLocation,
                GetRoslynTrigger(trigger)).ConfigureAwait(false);

            if (completionList == null)
            {
                return new EditorCompletion.CompletionContext(ImmutableArray<EditorCompletion.CompletionItem>.Empty);
            }

            var filterCache = new Dictionary<string, CompletionFilter>();

            var items = completionList.Items.SelectAsArray(roslynItem =>
            {
                var needsCustomCommit = ((CompletionServiceWithProviders)completionService).GetProvider(roslynItem) is IFeaturesCustomCommitCompletionProvider;

                var item = Convert(document, roslynItem, completionService, filterCache, needsCustomCommit);
                item.Properties.AddProperty(TriggerBuffer, triggerLocation.Snapshot.TextBuffer);
                return item;
            });

            return new EditorCompletion.CompletionContext(
                items,
                useSoftSelection: false,
                completionList.SuggestionModeItem != null,
                completionList.SuggestionModeItem?.DisplayText);
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
            Dictionary<string, CompletionFilter> filterCache,
            bool needsCustomCommit)
        {
            var imageId = roslynItem.Tags.GetGlyph().GetImageId();
            var filters = GetFilters(roslynItem, filterCache);

            if (!roslynItem.Properties.TryGetValue("InsertionText", out var insertionText))
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
                        "Temporary Automation Name")); // TODO: There must be some way to get this text
            }

            var item = new EditorCompletion.CompletionItem(
                roslynItem.DisplayText,
                this,
                new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name"), // TODO: There must be some way to get this text
                filters,
                suffix: string.Empty,
                needsCustomCommit,
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
                            new AccessibleImageId(imageId.Guid, imageId.Id, "Temporary Automation Name")); // TODO: There must be some way to get this text
                        filterCache[filter.DisplayText] = itemFilter;
                        result.Add(itemFilter);
                    }
                }
            }

            return result.ToImmutableArray();
        }

        public async Task<object> GetDescriptionAsync(EditorCompletion.CompletionItem item, CancellationToken cancellationToken)
        {
            item.Properties.TryGetProperty<RoslynCompletionItem>(RoslynItem, out var roslynItem);
            item.Properties.TryGetProperty<ITextBuffer>(TriggerBuffer, out var triggerBuffer);

            Workspace.TryGetWorkspace(triggerBuffer.AsTextContainer(), out var workspace);

            var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentIdInCurrentContext(triggerBuffer.AsTextContainer()));
            var service = document.GetLanguageService<CompletionService>() as CompletionServiceWithProviders;
            var description = await service.GetProvider(roslynItem).GetDescriptionAsync(document, roslynItem, cancellationToken).ConfigureAwait(false);

            return new ClassifiedTextElement(description.TaggedParts.Select(p => new ClassifiedTextRun(p.Tag.ToClassificationTypeName(), p.Text)));
        }

        public CommitBehavior CustomCommit(
            ITextView view, 
            ITextBuffer buffer, 
            EditorCompletion.CompletionItem item, 
            ITrackingSpan applicableSpan, 
            char commitCharacter, 
            CancellationToken token)
        {
            var service = GetCompletionService(buffer.CurrentSnapshot) as CompletionServiceWithProviders;
            var roslynItem = item.Properties.GetProperty<RoslynCompletionItem>(RoslynItem);

            using (var edit = buffer.CreateEdit())
            {
                var provider = service.GetProvider(roslynItem);
                Workspace.TryGetWorkspace(buffer.AsTextContainer(), out var workspace);
                var document = workspace.CurrentSolution.GetDocument(workspace.GetDocumentIdInCurrentContext(buffer.AsTextContainer()));

                // TODO: Do we actually want the document from the initial snapshot?
                edit.Delete(applicableSpan.GetSpan(buffer.CurrentSnapshot));

                var change = ((IFeaturesCustomCommitCompletionProvider)provider).GetChangeAsync(
                    document, 
                    roslynItem, 
                    commitCharacter, 
                    CancellationToken.None).WaitAndGetResult(token);
                edit.Replace(change.TextChange.Span.ToSpan(), change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(buffer.CurrentSnapshot, change.NewPosition.Value));
                }
            }

            return CommitBehavior.SuppressFurtherCommandHandlers;
        }

        public CommitBehavior GetDefaultCommitBehavior(ITextView view, ITextBuffer buffer, EditorCompletion.CompletionItem item, ITrackingSpan applicableSpan, char typeChar, CancellationToken token)
        {
            if (PassEnterThroughToBuffer() && typeChar == '\n')
            {
                return CommitBehavior.RaiseFurtherCommandHandlers;
            }

            if (item.InsertText.EndsWith(":") && typeChar == ':')
            {
                return CommitBehavior.SuppressFurtherCommandHandlers;
            }

            return CommitBehavior.None;
        }

        public ImmutableArray<char> GetPotentialCommitCharacters()
        {
            return CommitChars;
        }

        public bool ShouldCommitCompletion(char typedChar, SnapshotPoint location)
        {
            // TODO: It's more complex than this.
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

            // TODO: Edit of 0 means Invoke or InvokeAndCommitIfUnique. An API update will make this better.
            if (edit != 0 && !service.ShouldTriggerCompletion(text, location.Position, RoslynTrigger.CreateInsertionTrigger(edit)))
            {
                return null;
            }

            // TODO: Check CompletionOptions.TriggerOnTyping
            // TODO: Check CompletionOptions.TriggerOnDeletion

            return new SnapshotSpan(location.Snapshot, service.GetDefaultCompletionListSpan(text, location.Position).ToSpan());
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
            applicableSpan = default;
            return false;
        }
    }
}
