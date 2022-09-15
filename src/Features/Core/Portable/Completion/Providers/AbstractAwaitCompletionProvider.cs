// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// A completion provider for offering <see langword="await"/> keyword.
    /// This is implemented separately, not as a keyword recommender as it contains extra logic for making container method async.
    /// </summary>
    internal abstract class AbstractAwaitCompletionProvider : LSPCompletionProvider
    {
        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private protected abstract int GetSpanStart(SyntaxNode declaration);

        private protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

        public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            if (!syntaxContext.IsAwaitKeywordContext())
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItem = GetCompletionItem(syntaxContext.TargetToken, generator, syntaxKinds, syntaxFacts);
            context.AddItem(completionItem);
        }

        public sealed override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // IsComplexTextEdit is true when we want to add async to the container.
            if (!item.IsComplexTextEdit)
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var declaration = GetAsyncSupportingDeclaration(root.FindToken(item.Span.Start));
            if (declaration is null)
            {
                // IsComplexTextEdit should only be true when GetAsyncSupportingDeclaration returns non-null.
                // This is ensured by the ShouldMakeContainerAsync overrides.
                Debug.Assert(false, "Expected non-null value for declaration.");
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
            builder.Add(new TextChange(new TextSpan(GetSpanStart(declaration), 0), syntaxFacts.GetText(syntaxKinds.AsyncKeyword) + " "));
            builder.Add(new TextChange(item.Span, item.DisplayText));

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);
            return CompletionChange.Create(Utilities.Collapse(newText, builder.ToImmutableArray()));
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private protected bool ShouldMakeContainerAsync(SyntaxToken token, SyntaxGenerator generator)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var declaration = GetAsyncSupportingDeclaration(token);
            return declaration is not null && !generator.GetModifiers(declaration).IsAsync;
        }

        private CompletionItem GetCompletionItem(SyntaxToken token, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            var shouldMakeContainerAsync = ShouldMakeContainerAsync(token, generator);
            var text = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            return CommonCompletionItem.Create(
                displayText: text,
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Keyword,
                description: RecommendedKeyword.CreateDisplayParts(text, FeaturesResources.Asynchronously_waits_for_the_task_to_finish),
                inlineDescription: shouldMakeContainerAsync ? FeaturesResources.Make_containing_scope_async : null,
                isComplexTextEdit: shouldMakeContainerAsync);
        }
    }
}
