// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
        private protected abstract string AsyncKeywordTextWithSpace { get; }

        private protected abstract CompletionItem GetCompletionItem(SyntaxToken token);

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private protected abstract int GetSpanStart(SyntaxNode declaration);

        private protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        private protected abstract bool ShouldMakeContainerAsync(SyntaxToken token);
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods

        public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(workspace, semanticModel, position, cancellationToken);
            if (!syntaxContext.IsAwaitKeywordContext())
            {
                return;
            }

            var completionItem = GetCompletionItem(syntaxContext.TargetToken);
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

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
            builder.Add(new TextChange(new TextSpan(GetSpanStart(declaration), 0), AsyncKeywordTextWithSpace));
            builder.Add(new TextChange(item.Span, item.DisplayText));

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);
            return CompletionChange.Create(Utilities.Collapse(newText, builder.ToImmutableArray()));
        }
    }
}
