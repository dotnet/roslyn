﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// Implement a subtype of this class and export it to provide completions during typing in an editor.
    /// </summary>
    public abstract class CompletionProvider
    {
        internal string Name { get; }

        protected CompletionProvider()
        {
            Name = GetType().FullName;
        }

        /// <summary>
        /// Implement to contribute <see cref="CompletionItem"/>'s and other details to a <see cref="CompletionList"/>
        /// </summary>
        public abstract Task ProvideCompletionsAsync(CompletionContext context);

        /// <summary>
        /// Returns true if the character recently inserted or deleted in the text should trigger completion.
        /// </summary>
        /// <param name="text">The text that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="options">The set of options in effect.</param>
        public virtual bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            return false;
        }

        /// <summary>
        /// This allows Completion Providers that indicated they were triggered textually to use syntax to
        /// confirm they are really triggered, or decide they are not actually triggered and should become 
        /// an augmenting provider instead.
        /// </summary>
        internal virtual async Task<bool> IsSyntacticTriggerCharacterAsync(Document document, int caretPosition, CompletionTrigger trigger, OptionSet options, CancellationToken cancellationToken)
            => ShouldTriggerCompletion(await document.GetTextAsync(cancellationToken).ConfigureAwait(false), caretPosition, trigger, options);

        /// <summary>
        /// Gets the description of the specified item.
        /// </summary>
        public virtual Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
        {
            return Task.FromResult(CompletionDescription.Empty);
        }

        /// <summary>
        /// Gets the change to be applied when the specified item is committed.
        /// </summary>
        /// <param name="document">The current document.</param>
        /// <param name="item">The item to be committed.</param>
        /// <param name="commitKey">The optional key character that caused the commit.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionChange> GetChangeAsync(
            Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, item.DisplayText)));
        }

        internal virtual Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, TextSpan completionListSpan, char? commitKey, CancellationToken cancellationToken)
            => GetChangeAsync(document, item, commitKey, cancellationToken);

        /// <summary>
        /// True if the provider produces snippet items.
        /// </summary>
        internal virtual bool IsSnippetProvider => false;

        /// <summary>
        /// True if the provider produces items show be shown in expanded list only.
        /// </summary>
        internal virtual bool IsExpandItemProvider => false;
    }
}
