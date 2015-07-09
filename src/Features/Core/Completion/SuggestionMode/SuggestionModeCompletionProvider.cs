using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.SuggestionMode
{
    internal abstract class SuggestionModeCompletionProvider : ICompletionProvider
    {
        protected abstract Task<CompletionItem> GetBuilderAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken);
        protected abstract TextSpan GetFilterSpan(SourceText text, int position);

        public async Task<CompletionItemGroup> GetGroupAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            var builder = await this.GetBuilderAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            if (builder == null)
            {
                return null;
            }

            return new CompletionItemGroup(
                SpecializedCollections.EmptyEnumerable<CompletionItem>(),
                builder,
                isExclusive: false);
        }

        protected CompletionItem CreateEmptyBuilder(SourceText text, int position)
        {
            return CreateBuilder(text, position, displayText: null, description: null);
        }

        protected CompletionItem CreateBuilder(SourceText text, int position, string displayText, string description)
        {
            return new CompletionItem(
                completionProvider: this,
                displayText: displayText ?? string.Empty,
                filterSpan: GetFilterSpan(text, position),
                description: description != null ? description.ToSymbolDisplayParts() : default(ImmutableArray<SymbolDisplayPart>),
                isBuilder: true);
        }

        public TextChange GetTextChange(CompletionItem selectedItem, char? ch = default(char?), string textTypedSoFar = null) => new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        public bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar) => false;
        public bool IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar) => false;
        public bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options) => false;
        public bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar) => false;
    }
}
