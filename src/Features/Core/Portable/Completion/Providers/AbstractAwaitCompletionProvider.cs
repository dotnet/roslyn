// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

/// <summary>
/// A completion provider for offering <see langword="await"/> keyword.
/// This is implemented separately, not as a keyword recommender as it contains extra logic for making container method async.
/// </summary>
internal abstract class AbstractAwaitCompletionProvider : AbstractAsyncSupportingCompletionProvider
{
    private const string AppendConfigureAwait = nameof(AppendConfigureAwait);

    /// <summary>
    /// If 'await' should be placed at the current position.  If not present, it means to add 'await' prior 
    /// to the preceding expression.
    /// </summary>
    private const string AddAwaitAtCurrentPosition = nameof(AddAwaitAtCurrentPosition);

    protected enum DotAwaitContext
    {
        None,
        AwaitOnly,
        AwaitAndConfigureAwait,
    }

    private readonly string _awaitKeyword;
    private readonly string _awaitfDisplayText;
    private readonly string _awaitfFilterText;
    private readonly string _falseKeyword;

    protected AbstractAwaitCompletionProvider(ISyntaxFacts syntaxFacts)
    {
        _falseKeyword = syntaxFacts.GetText(syntaxFacts.SyntaxKinds.FalseKeyword);
        _awaitKeyword = syntaxFacts.GetText(syntaxFacts.SyntaxKinds.AwaitKeyword);
        _awaitfDisplayText = $"{_awaitKeyword}f";
        _awaitfFilterText = $"{_awaitKeyword}F"; // Uppercase F to select "awaitf" if "af" is written.
    }

    protected abstract ITypeSymbol? GetTypeSymbolOfExpression(SemanticModel semanticModel, SyntaxNode potentialAwaitableExpression, CancellationToken cancellationToken);
    protected abstract SyntaxNode? GetExpressionToPlaceAwaitInFrontOf(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
    protected abstract SyntaxToken? GetDotTokenLeftOfPosition(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

    private static bool IsConfigureAwaitable(Compilation compilation, ITypeSymbol symbol)
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
            return;

        var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);

        var isAwaitKeywordContext = syntaxContext.IsAwaitKeywordContext;
        var dotAwaitContext = GetDotAwaitKeywordContext(syntaxContext, cancellationToken);
        if (!isAwaitKeywordContext && dotAwaitContext == DotAwaitContext.None)
            return;

        var leftToken = syntaxContext.LeftToken;
        var declaration = GetAsyncSupportingDeclaration(leftToken, position);

        using var builder = TemporaryArray<KeyValuePair<string, string>>.Empty;

        builder.Add(KeyValuePair.Create(Position, position.ToString()));
        builder.Add(KeyValuePair.Create(LeftTokenPosition, leftToken.SpanStart.ToString()));

        var makeContainerAsync = declaration is not null && !SyntaxGenerator.GetGenerator(document).GetModifiers(declaration).IsAsync;
        if (makeContainerAsync)
            builder.Add(KeyValuePair.Create(AddModifiers, string.Empty));

        if (isAwaitKeywordContext)
        {
            builder.Add(KeyValuePair.Create(AddAwaitAtCurrentPosition, string.Empty));
            var properties = builder.ToImmutableAndClear();

            context.AddItem(CreateCompletionItem(
                properties, _awaitKeyword, _awaitKeyword,
                FeaturesResources.Asynchronously_waits_for_the_task_to_finish,
                isComplexTextEdit: makeContainerAsync,
                appendConfigureAwait: false));
        }
        else
        {
            Contract.ThrowIfTrue(dotAwaitContext == DotAwaitContext.None);

            var properties = builder.ToImmutableAndClear();

            // add the `await` option that will remove the dot and add `await` to the start of the expression.
            context.AddItem(CreateCompletionItem(
                properties, _awaitKeyword, _awaitKeyword,
                FeaturesResources.Await_the_preceding_expression,
                isComplexTextEdit: true,
                appendConfigureAwait: false));

            if (dotAwaitContext == DotAwaitContext.AwaitAndConfigureAwait)
            {
                // add the `awaitf` option to do the same, but also add .ConfigureAwait(false);
                properties = properties.Add(KeyValuePair.Create(AppendConfigureAwait, string.Empty));
                context.AddItem(CreateCompletionItem(
                    properties, _awaitfDisplayText, _awaitfFilterText,
                    string.Format(FeaturesResources.Await_the_preceding_expression_and_add_ConfigureAwait_0, _falseKeyword),
                    isComplexTextEdit: true,
                    appendConfigureAwait: true));
            }
        }

        return;

        static CompletionItem CreateCompletionItem(
            ImmutableArray<KeyValuePair<string, string>> completionProperties, string displayText, string filterText, string tooltip, bool isComplexTextEdit, bool appendConfigureAwait)
        {
            var description = appendConfigureAwait
                ? [new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, tooltip)]
                : RecommendedKeyword.CreateDisplayParts(displayText, tooltip);

            return CommonCompletionItem.Create(
                displayText: displayText,
                displayTextSuffix: "",
                filterText: filterText,
                rules: CompletionItemRules.Default,
                glyph: Glyph.Keyword,
                description: description,
                isComplexTextEdit: isComplexTextEdit,
                properties: completionProperties);
        }
    }

    protected override async Task<(TextChange textChange, ImmutableArray<TextChange> allChanges)> GetCompletionTextChangesAsync(
        Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<TextChange>.GetInstance(out var builder);

        if (item.TryGetProperty(AddAwaitAtCurrentPosition, out var _))
        {
            builder.Add(new TextChange(item.Span, _awaitKeyword));
        }
        else
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var position = item.Span.Start;
            var dotToken = GetDotTokenLeftOfPosition(syntaxTree, position, cancellationToken);
            var expr = GetExpressionToPlaceAwaitInFrontOf(syntaxTree, position, cancellationToken);

            Contract.ThrowIfFalse(dotToken.HasValue);
            Contract.ThrowIfNull(expr);

            // place "await" in front of expr
            builder.Add(new TextChange(new TextSpan(expr.SpanStart, 0), _awaitKeyword + " "));

            // remove any text after dot, including the dot token and optionally append .ConfigureAwait(false)
            var replacementText = item.TryGetProperty(AppendConfigureAwait, out var _)
                ? $".{nameof(Task.ConfigureAwait)}({_falseKeyword})"
                : "";

            builder.Add(new TextChange(TextSpan.FromBounds(dotToken.Value.SpanStart, item.Span.End), replacementText));
        }

        var allChanges = builder.ToImmutable();
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return (Utilities.Collapse(text.WithChanges(allChanges), allChanges), allChanges);
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
    private DotAwaitContext GetDotAwaitKeywordContext(SyntaxContext syntaxContext, CancellationToken cancellationToken)
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
                    var syntaxContextAtInsertationPosition = syntaxContext.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(
                        document, syntaxContext.SemanticModel, potentialAwaitableExpression.SpanStart, cancellationToken);
                    if (syntaxContextAtInsertationPosition.IsAwaitKeywordContext)
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
}
