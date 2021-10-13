// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    /// <summary>
    /// A completion provider for offering <see langword="await"/> keyword.
    /// This is implemented separately, not as a keyword recommender as it contains extra logic for making container method async.
    /// </summary>
    internal abstract class AbstractAwaitCompletionProvider : LSPCompletionProvider
    {
        #region CompletionItem properties keys
        private const string AwaitCompletionTargetTokenPosition = nameof(AwaitCompletionTargetTokenPosition);

        private const string AddAwaitAtCursor = nameof(AddAwaitAtCursor);
        private const string AddAwaitBeforeDotExpression = nameof(AddAwaitBeforeDotExpression);
        private const string AppendConfigureAwait = nameof(AppendConfigureAwait);
        private const string MakeContainerAsync = nameof(MakeContainerAsync);
        #endregion

        protected enum DotAwaitContext
        {
            None,
            AwaitOnly,
            AwaitAndConfigureAwait,
        }

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        protected abstract int GetSpanStart(SyntaxNode declaration);

        protected abstract SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token);

        protected abstract ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken);
        protected abstract SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        protected static bool IsConfigureAwaitable(Compilation compilation, ITypeSymbol symbol)
        {
            var originalDefinition = symbol.OriginalDefinition;
            return
                originalDefinition.Equals(compilation.TaskOfTType()) ||
                originalDefinition.Equals(compilation.TaskType()) ||
                originalDefinition.Equals(compilation.ValueTaskOfTType()) ||
                originalDefinition.Equals(compilation.ValueTaskType());
        }

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
            var syntaxContext = document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            var isAwaitKeywordContext = syntaxContext.IsAwaitKeywordContext();
            var dotAwaitContext = GetDotAwaitKeywordContext(syntaxContext, cancellationToken);
            Debug.Assert(!(isAwaitKeywordContext && (dotAwaitContext is DotAwaitContext.AwaitOnly or DotAwaitContext.AwaitAndConfigureAwait)), "isDotAwaitContext and isAwaitKeywordContext should never be both true.");
            if (!isAwaitKeywordContext && dotAwaitContext == DotAwaitContext.None)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItems = GetCompletionItems(syntaxContext.TargetToken, isAwaitKeywordContext, dotAwaitContext, generator, syntaxKinds, syntaxFacts);
            context.AddItems(completionItems);
        }

        public sealed override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            // IsComplexTextEdit is true when we want to add async to the container or place await in front of the expression.
            if (!item.IsComplexTextEdit)
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxKinds = syntaxFacts.SyntaxKinds;
            var properties = item.Properties;

            if (properties.ContainsKey(MakeContainerAsync))
            {
                var root = await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var tokenPosition = int.Parse(properties[AwaitCompletionTargetTokenPosition]);
                var declaration = GetAsyncSupportingDeclaration(root.FindToken(tokenPosition));
                if (declaration is null)
                {
                    // IsComplexTextEdit should only be true when GetAsyncSupportingDeclaration returns non-null.
                    // This is ensured by the ShouldMakeContainerAsync overrides.
                    Debug.Fail("Expected non-null value for declaration.");
                    return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
                }

                builder.Add(new TextChange(new TextSpan(GetSpanStart(declaration), 0), syntaxFacts.GetText(syntaxKinds.AsyncKeyword) + " "));
            }

            var awaitKeyword = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            if (properties.ContainsKey(AddAwaitAtCursor))
            {
                builder.Add(new TextChange(item.Span, awaitKeyword));
            }

            if (properties.ContainsKey(AddAwaitBeforeDotExpression))
            {
                var position = item.Span.Start;
                var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
                var expr = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);
                Contract.ThrowIfFalse(dotToken.HasValue);
                Contract.ThrowIfNull(expr);
                // place "await" in front of expr
                builder.Add(new TextChange(new TextSpan(expr.SpanStart, 0), awaitKeyword + " "));

                // remove any remains after dot, including the dot token and optionally append .ConfigureAwait(false)
                var replacementText = properties.ContainsKey(AppendConfigureAwait)
                    ? $".ConfigureAwait({syntaxFacts.GetText(syntaxKinds.FalseKeyword)})"
                    : "";
                builder.Add(new TextChange(TextSpan.FromBounds(dotToken.Value.SpanStart, item.Span.End), replacementText));
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newText = text.WithChanges(builder);
            return CompletionChange.Create(Utilities.Collapse(newText, builder.ToImmutableArray()));
        }

#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        protected bool ShouldMakeContainerAsync(SyntaxToken token, SyntaxGenerator generator)
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            var declaration = GetAsyncSupportingDeclaration(token);
            return declaration is not null && !generator.GetModifiers(declaration).IsAsync;
        }

        /// <summary>
        /// Should <see langword="await"/> be offered, if left of the dot at position is an awaitable expression?
        /// <code>
        ///   someTask.$$ // Suggest await completion
        ///   await someTask.$$ // Don't suggest await completion
        /// </code>
        /// </summary>
        /// <returns>
        ///     <see cref="DotAwaitContext.None"/>, if await can not be suggested for the expression left of the dot.
        ///     <see cref="DotAwaitContext.AwaitOnly"/>, if await should be suggested for the expression left of the dot, but ConfigureAwait(false) not.
        ///     <see cref="DotAwaitContext.AwaitAndConfigureAwait"/>, if await should be suggested for the expression left of the dot and ConfigureAwait(false).
        /// </returns>
        protected DotAwaitContext GetDotAwaitKeywordContext(SyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            var position = syntaxContext.Position;
            var syntaxTree = syntaxContext.SyntaxTree;
            var potentialAwaitableExpression = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);
            if (potentialAwaitableExpression is not null)
            {
                var parentOfAwaitable = potentialAwaitableExpression.Parent;
                var document = syntaxContext.Document;
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                if (!syntaxFacts.IsAwaitExpression(parentOfAwaitable))
                {
                    var semanticModel = syntaxContext.SemanticModel;
                    var symbol = GetTypeSymbolOfExpression(semanticModel, potentialAwaitableExpression, cancellationToken);
                    if (symbol.IsAwaitableNonDynamic(semanticModel, position))
                    {
                        // We have a awaitable type left of the dot, that is not yet awaited.
                        // We need to check if await is valid at the insertion position.
                        var syntaxContextAtInsertationPosition = syntaxContext.GetLanguageService<ISyntaxContextService>().CreateContext(
                            document, syntaxContext.SemanticModel, potentialAwaitableExpression.SpanStart, cancellationToken);
                        if (syntaxContextAtInsertationPosition.IsAwaitKeywordContext())
                        {
                            return IsConfigureAwaitable(syntaxContext.SemanticModel.Compilation, symbol)
                                ? DotAwaitContext.AwaitAndConfigureAwait
                                : DotAwaitContext.AwaitOnly;
                        }
                    }
                }
            }

            return DotAwaitContext.None;
        }

        private IEnumerable<CompletionItem> GetCompletionItems(SyntaxToken token, bool isAwaitKeywordContext, DotAwaitContext dotAwaitContext, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            var shouldMakeContainerAsync = ShouldMakeContainerAsync(token, generator);
            var displayText = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            var falseKeyword = syntaxFacts.GetText(syntaxKinds.FalseKeyword);
            var filterText = displayText;
            if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
            {
                // In the AwaitAndConfigureAwait case, we want to offer two completions: await and awaitf
                // This case adds "await"
                var completionPropertiesForAwaitOnly = GetCompletionProperties(token, isAwaitKeywordContext, DotAwaitContext.AwaitOnly, shouldMakeContainerAsync);
                yield return CreateCompletionItem(displayText, filterText, falseKeyword, completionPropertiesForAwaitOnly);
            }

            var completionProperties = GetCompletionProperties(token, isAwaitKeywordContext, dotAwaitContext, shouldMakeContainerAsync);
            if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
            {
                displayText += "f";
                filterText += "F"; // Uppercase F to select "awaitf" if "af" is written.
            }

            yield return CreateCompletionItem(displayText, filterText, falseKeyword, completionProperties);
            yield break;

            static CompletionItem CreateCompletionItem(string displayText, string filterText, string falseKeyword, ImmutableDictionary<string, string> completionProperties)
            {
                var makeContainerAsync = completionProperties.ContainsKey(MakeContainerAsync);
                var addAwaitBeforeDotExpression = completionProperties.ContainsKey(AddAwaitBeforeDotExpression);
                var appendConfigureAwait = completionProperties.ContainsKey(AppendConfigureAwait);
                var inlineDescription = makeContainerAsync ? FeaturesResources.Make_containing_scope_async : null;
                var isComplexTextEdit = makeContainerAsync | addAwaitBeforeDotExpression | appendConfigureAwait;
                var tooltip =
                    addAwaitBeforeDotExpression
                        ? appendConfigureAwait
                            ? string.Format(FeaturesResources.Await_the_preceding_expression_and_add_ConfigureAwait_0, falseKeyword)
                            : FeaturesResources.Await_the_preceding_expression
                        : FeaturesResources.Asynchronously_waits_for_the_task_to_finish;
                var description = appendConfigureAwait
                    ? ImmutableArray.Create(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, tooltip))
                    : RecommendedKeyword.CreateDisplayParts(displayText, tooltip);

                return CommonCompletionItem.Create(
                    displayText: displayText,
                    displayTextSuffix: "",
                    filterText: filterText,
                    rules: CompletionItemRules.Default,
                    glyph: Glyph.Keyword,
                    description: description,
                    inlineDescription: inlineDescription,
                    isComplexTextEdit: isComplexTextEdit,
                    properties: completionProperties);
            }

            static ImmutableDictionary<string, string> GetCompletionProperties(SyntaxToken targetToken, bool isAwaitKeywordContext, DotAwaitContext dotAwaitContext, bool shouldMakeContainerAsync)
            {
                using var _ = PooledDictionary<string, string>.GetInstance(out var dict);
                dict.Add(AwaitCompletionTargetTokenPosition, targetToken.SpanStart.ToString());
                if (isAwaitKeywordContext)
                    dict.Add(AddAwaitAtCursor, string.Empty);
                if (dotAwaitContext is DotAwaitContext.AwaitOnly or DotAwaitContext.AwaitAndConfigureAwait)
                    dict.Add(AddAwaitBeforeDotExpression, string.Empty);
                if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
                    dict.Add(AppendConfigureAwait, string.Empty);
                if (shouldMakeContainerAsync)
                    dict.Add(MakeContainerAsync, string.Empty);
                return dict.ToImmutableDictionary();
            }
        }
    }
}
