// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// <param name="text">The text that completion is occuring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="options">The set of options in effect.</param>
        public virtual bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
        {
            return false;
        }

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
    }
}
