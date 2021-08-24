// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        [Flags]
        private enum CompletionChangeEdit
        {
            AddAwaitAtCursor = 1,
            AddAwaitBeforeDotExpression = 2,
            MakeContainerAsync = 4,
        }

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private protected abstract int GetSpanStart(SyntaxNode declaration);

        private protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

        /// <summary>
        /// Should <see langword="await"/> be offered, if left of the dot at position is an awaitable expression?
        /// <code>
        ///   someTask.$$ // Suggest await completion
        ///   await someTask.$$ // Don't suggest await completion
        /// </code>
        /// </summary>
        /// <returns><see langword="true"/>, if await should be suggested for the expression left of the dot.</returns>
        private protected abstract bool IsDotAwaitKeywordContext(SyntaxContext syntaxContext, CancellationToken cancellationToken);

        private protected abstract SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        private protected abstract SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        public sealed override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var workspace = document.Project.Solution.Workspace;
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(workspace, semanticModel, position, cancellationToken);

            var isAwaitKeywordContext = syntaxContext.IsAwaitKeywordContext();
            var isDotAwaitContext = IsDotAwaitKeywordContext(syntaxContext, cancellationToken);
            Debug.Assert(!(isAwaitKeywordContext && isDotAwaitContext), "isDotAwaitContext and isAwaitKeywordContext should never be both true.");
            if (!isAwaitKeywordContext && !isDotAwaitContext)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItem = GetCompletionItem(syntaxContext.TargetToken, isAwaitKeywordContext, isDotAwaitContext, generator, syntaxKinds, syntaxFacts);
            context.AddItem(completionItem);
        }

        public sealed override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // IsComplexTextEdit is true when we want to add async to the container or place await in front of the expression.
            if (!item.IsComplexTextEdit)
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }
            var completionChangeEdit = (CompletionChangeEdit)Enum.Parse(typeof(CompletionChangeEdit), item.Properties[nameof(CompletionChangeEdit)]);
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();

            if ((completionChangeEdit & CompletionChangeEdit.MakeContainerAsync) != 0)
            {
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var declaration = GetAsyncSupportingDeclaration(root.FindToken(item.Span.Start));
                if (declaration is null)
                {
                    // IsComplexTextEdit should only be true when GetAsyncSupportingDeclaration returns non-null.
                    // This is ensured by the ShouldMakeContainerAsync overrides.
                    Debug.Assert(false, "Expected non-null value for declaration.");
                    return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
                }

                builder.Add(new TextChange(new TextSpan(GetSpanStart(declaration), 0), syntaxFacts.GetText(syntaxKinds.AsyncKeyword) + " "));
            }

            if ((completionChangeEdit & CompletionChangeEdit.AddAwaitAtCursor) != 0)
            {
                builder.Add(new TextChange(item.Span, item.DisplayText));
            }

            if ((completionChangeEdit & CompletionChangeEdit.AddAwaitBeforeDotExpression) != 0)
            {
                var position = item.Span.Start;
                var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
                var expr = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);
                if (expr is not null && dotToken.HasValue)
                {
                    // place "await" in front of expr
                    builder.Add(new TextChange(new TextSpan(expr.SpanStart, 0), item.DisplayText + " "));
                    // remove any remains after dot, including the dot token
                    builder.Add(new TextChange(TextSpan.FromBounds(dotToken.Value.SpanStart, item.Span.End), ""));
                }
            }

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

        private CompletionItem GetCompletionItem(SyntaxToken token, bool isAwaitKeywordContext, bool isDotAwaitContext, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            var shouldMakeContainerAsync = ShouldMakeContainerAsync(token, generator);
            var text = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            var completionChangeEdit = GetCompletionChangeEdit(isAwaitKeywordContext, isDotAwaitContext, shouldMakeContainerAsync);

            return CommonCompletionItem.Create(
                displayText: text,
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Keyword,
                description: RecommendedKeyword.CreateDisplayParts(text, FeaturesResources.Asynchronously_waits_for_the_task_to_finish),
                inlineDescription: shouldMakeContainerAsync ? FeaturesResources.Make_containing_scope_async : null,
                isComplexTextEdit: shouldMakeContainerAsync || isDotAwaitContext,
                properties: ImmutableDictionary.Create<string, string>().Add(nameof(CompletionChangeEdit), completionChangeEdit.ToString()));

            static CompletionChangeEdit GetCompletionChangeEdit(bool isAwaitKeywordContext, bool isDotAwaitContext, bool shouldMakeContainerAsync)
            {
                var codeActionEdit = default(CompletionChangeEdit);
                if (isAwaitKeywordContext)
                    codeActionEdit |= CompletionChangeEdit.AddAwaitAtCursor;
                if (isDotAwaitContext)
                    codeActionEdit |= CompletionChangeEdit.AddAwaitBeforeDotExpression;
                if (shouldMakeContainerAsync)
                    codeActionEdit |= CompletionChangeEdit.MakeContainerAsync;
                return codeActionEdit;
            }
        }
    }
}
