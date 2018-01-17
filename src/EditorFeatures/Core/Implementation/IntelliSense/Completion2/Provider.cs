using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense.Prototype.Definition;
using Prototype = Microsoft.VisualStudio.Language.Intellisense.Prototype.Definition;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.Extensions;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynTrigger = Microsoft.CodeAnalysis.Completion.CompletionTrigger;
using System.Threading;
using Microsoft.VisualStudio.Language.Intellisense;

namespace RoslynCompletionPrototype
{
    [Export(typeof(IAsyncCompletionItemSource))]
    [Name("C# completion item source")]
    [ContentType("CSharp")]
    [ContentType("Basic")]
    class RoslynCompletionItemSource : IAsyncCompletionItemSource
    {
        static readonly ImmutableArray<string> CommitChars = ImmutableArray.Create<string>(".", ",", "(", ")", "[", "]", " ", "\t");
        const string RoslynItem = nameof(RoslynItem);
        private const string TriggerSnapshot = nameof(TriggerSnapshot);

        public async Task<Prototype.CompletionContext> GetCompletionContextAsync(Prototype.CompletionTrigger trigger, SnapshotPoint triggerLocation)
        {
            var snapshot = triggerLocation.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return default;
            }

            var completionService = document.GetLanguageService<CompletionService>();

            var completionList = await completionService.GetCompletionsAsync(document, triggerLocation.Position).ConfigureAwait(false);
            if (completionList == null)
                return default(Prototype.CompletionContext);

            var text = await document.GetTextAsync().ConfigureAwait(false);
            var applicableSpan = completionService.GetDefaultCompletionListSpan(text, triggerLocation.Position).ToSnapshotSpan(triggerLocation.Snapshot);


            var imageIdService = document.Project.Solution.Workspace.Services.GetService<IImageIdService>();

            Dictionary<string, CompletionFilter> filterCache = new Dictionary<string, CompletionFilter>();

            var items = completionList.Items.Select(roslynItem =>
            {
                var i = Convert(roslynItem, imageIdService, completionService, filterCache);
                i.Properties.AddProperty(TriggerSnapshot, triggerLocation.Snapshot);
                return i;
            }).ToArray();


            return new Prototype.CompletionContext(items,
                applicableSpan, filterCache.Values.ToImmutableArray(), false, false, suggestionModeDescription: "builder");
        }

        private Prototype.CompletionItem Convert(RoslynCompletionItem roslynItem, IImageIdService imageService, CompletionService completionService, Dictionary<string, CompletionFilter> filterCache)
        {
            var imageId = imageService.GetImageId(roslynItem.Tags.GetGlyph());
            var insertionText = "BAD"; // because reasons, we can't specify this up front
            var filters = GetFilters(roslynItem, imageService, filterCache);
            var item = new Prototype.CompletionItem(
                roslynItem.DisplayText,
                insertionText,
                roslynItem.SortText,
                roslynItem.FilterText,
                this,
                filters,
                customCommit: true, //always true, sadly
                imageId);

            item.Properties.AddProperty(RoslynItem, roslynItem);
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
                        var itemFilter = new CompletionFilter(filter.DisplayText, filter.AccessKey.ToString(), imageService.GetImageId(filter.Tags.GetGlyph()));
                        filterCache[filter.DisplayText] = itemFilter;
                        result.Add(itemFilter);
                    }
                }
            }

            return result.ToImmutableArray();
        }



        public async Task<object> GetDescriptionAsync(Prototype.CompletionItem item)
        {
            return "Documentation!"; // Editor doesn't call this yet
        }

        public void CustomCommit(ITextView view, ITextBuffer buffer, Prototype.CompletionItem item, ITrackingSpan applicableSpan, string textEdit)
        {
            // We should crash if this fails
            var service = GetCompletionService(buffer.CurrentSnapshot) as CompletionServiceWithProviders;

            var roslynItem = item.Properties.GetProperty<Microsoft.CodeAnalysis.Completion.CompletionItem>(RoslynItem); // We're using custom data we deposited in GetCompletionContextAsync
            var triggerSnapshot = item.Properties.GetProperty<ITextSnapshot>(TriggerSnapshot);

            var edit = buffer.CreateEdit();
            var provider = service.GetProvider(roslynItem);
            if (provider is ICustomCommitCompletionProvider c)
            {
                c.Commit(roslynItem, view, buffer, triggerSnapshot, null);
            }
            else
            {
                var document = buffer.GetRelatedDocuments().First();
                char? commitCharacter = String.IsNullOrEmpty(textEdit) ? null : new char?(textEdit[0]);
                var roslynChange = service.GetChangeAsync(document, roslynItem, commitCharacter, CancellationToken.None).Result;

                // TODO: Editor to reapply inserted trigger after we commit
                var ts = new SnapshotSpan(triggerSnapshot, roslynChange.TextChange.Span.ToSpan());
                var mapped = ts.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive);

                edit.Replace(mapped.Span, roslynChange.TextChange.NewText);
            }

            edit.Apply();
        }

        public ImmutableArray<string> GetPotentialCommitCharacters()
        {
            return CommitChars;
        }

        public bool ShouldCommitCompletion(string typedChar, SnapshotPoint location)
        {
            return CommitChars.Contains(typedChar);
        }

        public bool ShouldTriggerCompletion(string edit, SnapshotPoint location)
        {
            var text = SourceText.From(location.Snapshot.GetText());
            var service = GetCompletionService(location.Snapshot);
            return service?.ShouldTriggerCompletion(text, location.Position, RoslynTrigger.CreateInsertionTrigger(edit[0])) ?? false;
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

        public Task HandleViewClosedAsync(ITextView view) => throw new NotImplementedException();
    }
}
