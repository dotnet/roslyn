// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// A per language service for constructing context dependent list of completions that 
    /// can be presented to a user during typing in an editor.
    /// </summary>
    public abstract class CompletionService : ILanguageService
    {
        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static CompletionService GetService(Document document)
        {
            return document.Project.LanguageServices.GetService<CompletionService>();
        }

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

        /// <summary>
        /// Gets the current presentation and behavior rules.
        /// </summary>
        public virtual CompletionRules GetRules()
        {
            return CompletionRules.Default;
        }

        /// <summary>
        /// Returns true if the character recently inserted or deleted in the text should trigger completion.
        /// </summary>
        /// <param name="text">The document text to trigger completion within </param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The potential triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <remarks>
        /// This API uses SourceText instead of Document so implementations can only be based on text, not syntax or semantics.
        /// </remarks>
        public virtual bool ShouldTriggerCompletion(
            SourceText text, 
            int caretPosition, 
            CompletionTrigger trigger, 
            ImmutableHashSet<string> roles = null, 
            OptionSet options = null)
        {
            return false;
        }

        /// <summary>
        /// Gets the span of the syntax element at the caret position.
        /// This is the most common value used for <see cref="CompletionItem.Span"/>.
        /// </summary>
        /// <param name="text">The document text that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret within the text.</param>
        public virtual TextSpan GetDefaultItemSpan(SourceText text, int caretPosition)
        {
            return CommonCompletionUtilities.GetWordSpan(text, caretPosition, c => char.IsLetter(c), c => char.IsLetterOrDigit(c));
        }

        /// <summary>
        /// Gets the completions available at the caret position.
        /// </summary>
        /// <param name="document">The document that completion is occuring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="roles">Optional set of roles associated with the editor state.</param>
        /// <param name="options">Optional options that override the default options.</param>
        /// <param name="cancellationToken"></param>
        public abstract Task<CompletionList> GetCompletionsAsync(
            Document document,
            int caretPosition,
            CompletionTrigger trigger = default(CompletionTrigger),
            ImmutableHashSet<string> roles = null,
            OptionSet options = null,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the description of the item.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="item">The item to get the description for.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionDescription> GetDescriptionAsync(
            Document document, 
            CompletionItem item, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(CompletionDescription.Empty);
        }

        /// <summary>
        /// Gets the change to be applied when the item is committed.
        /// </summary>
        /// <param name="document">The document that completion is occurring within.</param>
        /// <param name="item">The item to get the change for.</param>
        /// <param name="commitCharacter">The typed character that caused the item to be committed. 
        /// This character may be used as part of the change. 
        /// This value is null when the commit was caused by the [TAB] or [ENTER] keys.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionChange> GetChangeAsync(
            Document document, 
            CompletionItem item, 
            char? commitCharacter = null, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(CompletionChange.Create(ImmutableArray.Create(new TextChange(item.Span, item.DisplayText))));
        }
    }
}
