// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractCompletionProvider : ICompletionProvider
    {
        public abstract bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar);
        public abstract bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar);
        public abstract bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options);

        protected abstract Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken);

        public async Task<CompletionItemGroup> GetGroupAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken = default(CancellationToken))
        {
            var items = await this.GetItemsAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            var builder = await this.GetBuilderAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);

            if (items == null && builder == null)
            {
                return null;
            }

            return new CompletionItemGroup(
                items ?? SpecializedCollections.EmptyEnumerable<CompletionItem>(),
                builder,
                await this.IsExclusiveAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false));
        }

        public virtual TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        }

        protected virtual Task<bool> IsExclusiveAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            return SpecializedTasks.False;
        }

        private async Task<IEnumerable<CompletionItem>> GetItemsAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            if (document == null)
            {
                return null;
            }

            // If we were triggered by typing a character, then do a semantic check to make sure
            // we're still applicable.  If not, then return immediately.
            if (triggerInfo.TriggerReason == CompletionTriggerReason.TypeCharCommand)
            {
                var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(document, position - 1, cancellationToken).ConfigureAwait(false);
                if (!isSemanticTriggerCharacter)
                {
                    return null;
                }
            }

            return await GetItemsWorkerAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
        }

        protected virtual Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
            return SpecializedTasks.True;
        }

        protected virtual Task<CompletionItem> GetBuilderAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            return SpecializedTasks.Default<CompletionItem>();
        }

        public virtual bool IsFilterCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return false;
        }
    }
}
