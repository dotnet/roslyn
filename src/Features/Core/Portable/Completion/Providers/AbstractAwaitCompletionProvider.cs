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
        private static class AwaitCompletionChange
        {
            public const string AddAwaitAtCursor = nameof(AwaitCompletionChange) + "." + nameof(AddAwaitAtCursor);
            public const string AddAwaitBeforeDotExpression = nameof(AwaitCompletionChange) + "." + nameof(AddAwaitBeforeDotExpression);
            public const string AppendConfigureAwait = nameof(AwaitCompletionChange) + "." + nameof(AppendConfigureAwait);
            public const string MakeContainerAsync = nameof(AwaitCompletionChange) + "." + nameof(MakeContainerAsync);
        }

        protected enum DotAwaitContext
        {
            None,
            AwaitOnly,
            AwaitAndConfigureAwait,
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
        /// <returns>
        ///     <see cref="DotAwaitContext.None"/>, if await can not be suggested for the expression left of the dot.
        ///     <see cref="DotAwaitContext.AwaitOnly"/>, if await should be suggested for the expression left of the dot, but ConfigureAwait(false) not.
        ///     <see cref="DotAwaitContext.AwaitAndConfigureAwait"/>, if await should be suggested for the expression left of the dot and ConfigureAwait(false).
        /// </returns>
        private protected abstract DotAwaitContext GetDotAwaitKeywordContext(SyntaxContext syntaxContext, CancellationToken cancellationToken);

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

            if (properties.ContainsKey(AwaitCompletionChange.MakeContainerAsync))
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

            var awaitKeyword = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            if (properties.ContainsKey(AwaitCompletionChange.AddAwaitAtCursor))
            {
                builder.Add(new TextChange(item.Span, awaitKeyword));
            }

            if (properties.ContainsKey(AwaitCompletionChange.AddAwaitBeforeDotExpression))
            {
                var position = item.Span.Start;
                var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
                var expr = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);
                Contract.ThrowIfFalse(dotToken.HasValue);
                Contract.ThrowIfNull(expr);
                // place "await" in front of expr
                builder.Add(new TextChange(new TextSpan(expr.SpanStart, 0), awaitKeyword + " "));

                // remove any remains after dot, including the dot token and optionally append .ConfigureAwait(false)
                var replacementText = properties.ContainsKey(AwaitCompletionChange.AppendConfigureAwait)
                    ? $".ConfigureAwait({syntaxFacts.GetText(syntaxKinds.FalseKeyword)})"
                    : "";
                builder.Add(new TextChange(TextSpan.FromBounds(dotToken.Value.SpanStart, item.Span.End), replacementText));
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

        private IEnumerable<CompletionItem> GetCompletionItems(SyntaxToken token, bool isAwaitKeywordContext, DotAwaitContext dotAwaitContext, SyntaxGenerator generator, ISyntaxKindsService syntaxKinds, ISyntaxFactsService syntaxFacts)
        {
            var shouldMakeContainerAsync = ShouldMakeContainerAsync(token, generator);
            var displayText = syntaxFacts.GetText(syntaxKinds.AwaitKeyword);
            var filterText = displayText;
            if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
            {
                // In the AwaitAndConfigureAwait case, we want to offer two completions: await and awaitf
                // This case adds "await"
                var completionPropertiesForAwaitOnly = GetCompletionProperties(isAwaitKeywordContext, DotAwaitContext.AwaitOnly, shouldMakeContainerAsync);
                yield return CreateCompletionItem(displayText, filterText, completionPropertiesForAwaitOnly);

            }

            var completionProperties = GetCompletionProperties(isAwaitKeywordContext, dotAwaitContext, shouldMakeContainerAsync);
            if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
            {
                displayText += "f";
                filterText += "F"; // Uppercase F to select "awaitf" if "af" is written.
            }

            yield return CreateCompletionItem(displayText, filterText, completionProperties);

            static CompletionItem CreateCompletionItem(string displayText, string filterText, ImmutableDictionary<string, string> completionProperties)
            {
                var shouldMakeContainerAsync = completionProperties.ContainsKey(AwaitCompletionChange.MakeContainerAsync);
                var inlineDescription = shouldMakeContainerAsync ? FeaturesResources.Make_containing_scope_async : null; // TODO: Description for ConfigureAwait(false)
                var isComplexTextEdit = shouldMakeContainerAsync |
                    completionProperties.ContainsKey(AwaitCompletionChange.AddAwaitBeforeDotExpression) |
                    completionProperties.ContainsKey(AwaitCompletionChange.AppendConfigureAwait);

                return CommonCompletionItem.Create(
                    displayText: displayText,
                    displayTextSuffix: "",
                    filterText: filterText,
                    rules: CompletionItemRules.Default,
                    glyph: Glyph.Keyword,
                    description: RecommendedKeyword.CreateDisplayParts(displayText, FeaturesResources.Asynchronously_waits_for_the_task_to_finish),
                    inlineDescription: inlineDescription,
                    isComplexTextEdit: isComplexTextEdit,
                    properties: completionProperties);
            }

            static ImmutableDictionary<string, string> GetCompletionProperties(bool isAwaitKeywordContext, DotAwaitContext dotAwaitContext, bool shouldMakeContainerAsync)
            {
                using var _ = PooledDictionary<string, string>.GetInstance(out var dict);
                if (isAwaitKeywordContext)
                    AddKey(AwaitCompletionChange.AddAwaitAtCursor);
                if (dotAwaitContext is DotAwaitContext.AwaitOnly or DotAwaitContext.AwaitAndConfigureAwait)
                    AddKey(AwaitCompletionChange.AddAwaitBeforeDotExpression);
                if (dotAwaitContext is DotAwaitContext.AwaitAndConfigureAwait)
                    AddKey(AwaitCompletionChange.AppendConfigureAwait);
                if (shouldMakeContainerAsync)
                    AddKey(AwaitCompletionChange.MakeContainerAsync);
                return dict.ToImmutableDictionary();

                void AddKey(string key) => dict.Add(key, string.Empty);
            }
        }
    }
}
