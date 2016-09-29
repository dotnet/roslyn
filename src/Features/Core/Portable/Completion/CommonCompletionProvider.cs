// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Completion
{
    internal abstract class CommonCompletionProvider : CompletionProvider
    {
        public override bool ShouldTriggerCompletion(SourceText text, int position, CompletionTrigger trigger, OptionSet options)
        {
            switch (trigger.Kind)
            {
                case CompletionTriggerKind.Insertion:
                    var insertedCharacterPosition = position - 1;
                    return this.IsInsertionTrigger(text, insertedCharacterPosition, options);

                default:
                    return false;
            }
        }

        internal virtual bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            return false;
        }

        public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            if (CommonCompletionItem.HasDescription(item))
            {
                return Task.FromResult(CommonCompletionItem.GetDescription(item));
            }
            else
            {
                return Task.FromResult(CompletionDescription.Empty);
            }
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var change = (await GetTextChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false))
                ?? new TextChange(item.Span, item.DisplayText);
            return CompletionChange.Create(change);
        }

        public virtual Task<TextChange?> GetTextChangeAsync(Document document, CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return GetTextChangeAsync(selectedItem, ch, cancellationToken);
        }

        protected virtual Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            return Task.FromResult<TextChange?>(null);
        }
    }
}