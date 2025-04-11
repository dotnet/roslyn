// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

/// <summary>
/// Provides completion for uncommon unnamed symbols, like conversions, indexer and operators.  These completion 
/// items will be brought up with <c>dot</c> like normal, but will end up inserting more than just a name into
/// the editor.  For example, committing a conversion will insert the conversion prior to the expression being
/// dotted off of.
/// </summary>
[ExportCompletionProvider(nameof(UnnamedSymbolCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(SymbolCompletionProvider))]
internal sealed partial class UnnamedSymbolCompletionProvider : LSPCompletionProvider
{
    /// <summary>
    /// CompletionItems for indexers/operators should be sorted below other suggestions like methods or properties
    /// of the type.  We accomplish this by placing a character known to be greater than all other normal identifier
    /// characters as the start of our item's name. This doesn't affect what we insert though as all derived
    /// providers have specialized logic for what they need to do.
    /// </summary> 
    private const string SortingPrefix = "\uFFFD";

    /// <summary>
    /// Used to store what sort of unnamed symbol a completion item represents.
    /// </summary>
    internal const string KindName = "Kind";
    internal const string IndexerKindName = "Indexer";
    internal const string OperatorKindName = "Operator";
    internal const string ConversionKindName = "Conversion";

    /// <summary>
    /// Used to store the doc comment for some operators/conversions.  This is because some of them will be
    /// synthesized, so there will be no symbol we can recover after the fact in <see cref="GetDescriptionAsync"/>.
    /// </summary>
    private const string DocumentationCommentXmlName = "DocumentationCommentXml";

    [ImportingConstructor]
    [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public UnnamedSymbolCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override ImmutableHashSet<char> TriggerCharacters => ['.'];

    public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, CompletionOptions options)
        => text[insertedCharacterPosition] == '.';

    /// <summary>
    /// We keep operators sorted in a specific order.  We don't want to sort them alphabetically, but instead want
    /// to keep things like <c>==</c> and <c>!=</c> together.
    /// </summary>
    private static string SortText(int sortingGroupIndex, string sortTextSymbolPart)
        => $"{SortingPrefix}{sortingGroupIndex:000}_{sortTextSymbolPart}";

    /// <summary>
    /// Gets the dot-like token we're after, and also the start of the expression we'd want to place any text before.
    /// </summary>
    private static (SyntaxToken dotLikeToken, int expressionStart) GetDotAndExpressionStart(SyntaxNode root, int position, CancellationToken cancellationToken)
    {
        if (CompletionUtilities.GetDotTokenLeftOfPosition(root.SyntaxTree, position, cancellationToken) is not SyntaxToken dotToken)
            return default;

        // if we have `.Name`, we want to get the parent member-access of that to find the starting position.
        // Otherwise, if we have .. then we want the left side of that to find the starting position.
        var expression = dotToken.Kind() == SyntaxKind.DotToken
            ? dotToken.Parent as ExpressionSyntax
            : (dotToken.Parent as RangeExpressionSyntax)?.LeftOperand;

        if (expression == null)
            return default;

        // If we're after a ?. find the root of that conditional to find the start position of the expression.
        expression = expression.GetRootConditionalAccessExpression() ?? expression;
        return (dotToken, expression.SpanStart);
    }

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        var cancellationToken = context.CancellationToken;
        var document = context.Document;
        var position = context.Position;

        // Escape hatch feature flag to let us disable this feature remotely if we run into any issues with it, 
        if (context.CompletionOptions.UnnamedSymbolCompletionDisabled)
            return;

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var dotAndExprStart = GetDotAndExpressionStart(root, position, cancellationToken);
        if (dotAndExprStart == default)
            return;

        var recommender = document.GetRequiredLanguageService<IRecommendationService>();
        var syntaxContext = await context.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = syntaxContext.SemanticModel;

        var options = context.CompletionOptions.ToRecommendationServiceOptions();
        var recommendedSymbols = recommender.GetRecommendedSymbolsInContext(syntaxContext, options, cancellationToken);

        AddUnnamedSymbols(context, position, semanticModel, recommendedSymbols.UnnamedSymbols, cancellationToken);
    }

    private void AddUnnamedSymbols(
        CompletionContext context, int position, SemanticModel semanticModel, ImmutableArray<ISymbol> unnamedSymbols, CancellationToken cancellationToken)
    {
        // Add one 'this[]' entry for all the indexers this type may have.
        AddIndexers(context, unnamedSymbols.WhereAsArray(s => s.IsIndexer()));

        // Group all the related operators and add a single completion entry per group.
        var operatorGroups = unnamedSymbols.WhereAsArray(s => s.IsUserDefinedOperator()).GroupBy(op => op.Name);
        foreach (var opGroup in operatorGroups)
            AddOperatorGroup(context, opGroup.Key, opGroup);

        foreach (var symbol in unnamedSymbols)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (symbol.IsConversion())
                AddConversion(context, semanticModel, position, (IMethodSymbol)symbol);
        }
    }

    public override Task<CompletionChange> GetChangeAsync(
        Document document,
        CompletionItem item,
        char? commitKey,
        CancellationToken cancellationToken)
    {
        var kind = item.GetProperty(KindName);
        return kind switch
        {
            IndexerKindName => GetIndexerChangeAsync(document, item, cancellationToken),
            OperatorKindName => GetOperatorChangeAsync(document, item, cancellationToken),
            ConversionKindName => GetConversionChangeAsync(document, item, cancellationToken),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    internal override async Task<CompletionDescription?> GetDescriptionAsync(
        Document document,
        CompletionItem item,
        CompletionOptions options,
        SymbolDescriptionOptions displayOptions,
        CancellationToken cancellationToken)
    {
        var kind = item.GetProperty(KindName);
        return kind switch
        {
            IndexerKindName => await GetIndexerDescriptionAsync(document, item, displayOptions, cancellationToken).ConfigureAwait(false),
            OperatorKindName => await GetOperatorDescriptionAsync(document, item, displayOptions, cancellationToken).ConfigureAwait(false),
            ConversionKindName => await GetConversionDescriptionAsync(document, item, displayOptions, cancellationToken).ConfigureAwait(false),
            _ => throw ExceptionUtilities.UnexpectedValue(kind),
        };
    }

    private static Task<CompletionChange> ReplaceTextAfterOperatorAsync(Document document, CompletionItem item, string text, CancellationToken cancellationToken)
        => ReplaceTextAfterOperatorAsync(document, item, text, keepQuestion: false, positionOffset: 0, cancellationToken);

    private static async Task<CompletionChange> ReplaceTextAfterOperatorAsync(
        Document document,
        CompletionItem item,
        string text,
        bool keepQuestion,
        int positionOffset,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var position = SymbolCompletionItem.GetContextPosition(item);

        var (dotToken, _) = GetDotAndExpressionStart(root, position, cancellationToken);
        var questionToken = dotToken.GetPreviousToken().Kind() == SyntaxKind.QuestionToken
            ? dotToken.GetPreviousToken()
            : (SyntaxToken?)null;

        var replacementStart = !keepQuestion && questionToken != null
            ? questionToken.Value.SpanStart
            : dotToken.SpanStart;
        var newPosition = replacementStart + text.Length + positionOffset;

        var tokenOnLeft = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
        return CompletionChange.Create(
            new TextChange(TextSpan.FromBounds(replacementStart, tokenOnLeft.Span.End), text),
            newPosition);
    }
}
