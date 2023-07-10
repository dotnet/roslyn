// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
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
            => Name = GetType().FullName!;

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
            => false;

        /// <summary>
        /// Returns true if the character recently inserted or deleted in the text should trigger completion.
        /// </summary>
        /// <param name="languageServices">The language services available on the text document.</param>
        /// <param name="text">The text that completion is occurring within.</param>
        /// <param name="caretPosition">The position of the caret after the triggering action.</param>
        /// <param name="trigger">The triggering action.</param>
        /// <param name="options">The set of options in effect.</param>
        internal virtual bool ShouldTriggerCompletion(LanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CompletionOptions options, OptionSet passThroughOptions)
#pragma warning disable RS0030, CS0618 // Do not used banned/obsolete APIs
            => ShouldTriggerCompletion(text, caretPosition, trigger, passThroughOptions);
#pragma warning restore

        /// <summary>
        /// This allows Completion Providers that indicated they were triggered textually to use syntax to
        /// confirm they are really triggered, or decide they are not actually triggered and should become 
        /// an augmenting provider instead.
        /// </summary>
        internal virtual async Task<bool> IsSyntacticTriggerCharacterAsync(Document document, int caretPosition, CompletionTrigger trigger, CompletionOptions options, CancellationToken cancellationToken)
            => ShouldTriggerCompletion(document.Project.Services, await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false), caretPosition, trigger, options, document.Project.Solution.Options);

        /// <summary>
        /// Gets the description of the specified item.
        /// </summary>
        public virtual Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => Task.FromResult<CompletionDescription?>(CompletionDescription.Empty);

        internal virtual Task<CompletionDescription?> GetDescriptionAsync(Document document, CompletionItem item, CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken)
#pragma warning disable RS0030 // Do not used banned APIs
            => GetDescriptionAsync(document, item, cancellationToken);
#pragma warning restore

        /// <summary>
        /// Gets the change to be applied when the specified item is committed.
        /// </summary>
        /// <param name="document">The current document.</param>
        /// <param name="item">The item to be committed.</param>
        /// <param name="commitKey">The optional key character that caused the commit.</param>
        /// <param name="cancellationToken"></param>
        public virtual Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
            => Task.FromResult(CompletionChange.Create(new TextChange(item.Span, item.DisplayText)));

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
