// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract partial class AbstractCompletionProvider : CompletionListProvider
    {
        protected abstract Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken);

        public override async Task RegisterCompletionListAsync(CompletionListContext context)
        {
            var items = await this.GetItemsAsync(context.Document, context.Position, context.TriggerInfo, context.CancellationToken).ConfigureAwait(false);
            var builder = await this.GetBuilderAsync(context.Document, context.Position, context.TriggerInfo, context.CancellationToken).ConfigureAwait(false);

            if (items == null && builder == null)
            {
                return;
            }

            if (items != null)
            {
                foreach (var item in items)
                {
                    context.AddCompletionItem(item);
                }
            }

            if (builder != null)
            {
                context.RegisterBuilder(builder);
            }

            var isExclusive = await this.IsExclusiveAsync(context.Document, context.Position, context.TriggerInfo, context.CancellationToken).ConfigureAwait(false);

            context.MakeExclusive(isExclusive);
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
    }
}
