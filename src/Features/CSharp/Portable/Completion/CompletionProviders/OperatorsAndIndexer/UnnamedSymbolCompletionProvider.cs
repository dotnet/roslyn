// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(UnnamedSymbolCompletionProvider), LanguageNames.CSharp), Shared]
    [ExtensionOrder(After = nameof(SymbolCompletionProvider))]
    internal partial class UnnamedSymbolCompletionProvider : LSPCompletionProvider
    {
        // CompletionItems for indexers/operators should be sorted below other suggestions like methods or properties of
        // the type.  We accomplish this by placing a character known to be greater than all other normal identifier
        // characters as the start of our item's name. this doesn't affect what we insert though as all derived providers
        // have specialized logic for what they need to do.
        private const string SortingPrefix = "\uFFFD";

        internal const string KindName = "Kind";
        internal const string IndexerKindName = "Indexer";
        internal const string OperatorKindName = "Operator";
        internal const string ConversionKindName = "Conversion";

        private const string MinimalTypeNamePropertyName = "MinimalTypeName";
        private const string DocumentationCommentXmlName = "DocumentationCommentXml";

        [ImportingConstructor]
        [System.Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public UnnamedSymbolCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create('.');

        public override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => text[insertedCharacterPosition] == '.';

        private static string SortText(int sortingGroupIndex, string sortTextSymbolPart)
            => $"{SortingPrefix}{sortingGroupIndex:000}{sortTextSymbolPart}";

        private static (SyntaxToken tokenAtPosition, SyntaxToken dotToken) FindTokensAtPosition(
            SyntaxNode root, int position)
        {
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
            var potentialDotTokenLeftOfCursor = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
            if (potentialDotTokenLeftOfCursor.Kind() != SyntaxKind.DotToken)
                return default;

            if (potentialDotTokenLeftOfCursor.Parent is not ExpressionSyntax)
                return default;

            // don't want to trigger after a number.  All other cases after dot are ok.
            if (potentialDotTokenLeftOfCursor.GetPreviousToken().Kind() == SyntaxKind.NumericLiteralToken)
                return default;

            return (tokenAtPosition, potentialDotTokenLeftOfCursor);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var workspace = document.Project.Solution.Workspace;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (_, dotToken) = FindTokensAtPosition(root, position);
            if (dotToken == default)
                return;

            var recommender = document.GetRequiredLanguageService<IRecommendationService>();

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var recommendedSymbols = recommender.GetRecommendedSymbolsAtPosition(workspace, semanticModel, position, options, cancellationToken);

            var unnamedSymbols = recommendedSymbols.UnnamedSymbols;
            var indexers = unnamedSymbols.WhereAsArray(s => s.IsIndexer());
            AddIndexers(context, indexers);

            foreach (var symbol in recommendedSymbols.UnnamedSymbols)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbol.IsConversion())
                {
                    AddConversion(context, semanticModel, position, (IMethodSymbol)symbol);
                }
                else if (symbol.IsUserDefinedOperator())
                {
                    AddOperator(context, (IMethodSymbol)symbol);
                }
            }
        }

        internal override Task<CompletionChange> GetChangeAsync(
            Document document,
            CompletionItem item,
            TextSpan completionListSpan,
            char? commitKey,
            bool disallowAddingImports,
            CancellationToken cancellationToken)
        {
            var properties = item.Properties;
            var kind = properties[KindName];
            return kind switch
            {
                IndexerKindName => GetIndexerChangeAsync(document, item, cancellationToken),
                OperatorKindName => GetOperatorChangeAsync(document, item, cancellationToken),
                ConversionKindName => GetConversionChangeAsync(document, item, cancellationToken),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        public override async Task<CompletionDescription?> GetDescriptionAsync(
            Document document,
            CompletionItem item,
            CancellationToken cancellationToken)
        {
            var properties = item.Properties;
            var kind = properties[KindName];
            return kind switch
            {
                IndexerKindName => await GetIndexerDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false),
                OperatorKindName => await GetOperatorDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false),
                ConversionKindName => await GetConversionDescriptionAsync(document, item, cancellationToken).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }

        private static async Task<CompletionChange> ReplaceDotAndTokenAfterWithTextAsync(Document document,
            CompletionItem item, string text, bool removeConditionalAccess, int positionOffset,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var (tokenAtPosition, token) = FindTokensAtPosition(root, position);
            Contract.ThrowIfFalse(token.IsKind(SyntaxKind.DotToken)); // ProvideCompletionsAsync bails out, if token is not a DotToken

            var replacementStart = GetReplacementStart(removeConditionalAccess, token);
            var newPosition = replacementStart + text.Length + positionOffset;
            var replaceSpan = TextSpan.FromBounds(replacementStart, tokenAtPosition.Span.End);

            return CompletionChange.Create(new TextChange(replaceSpan, text), newPosition);
        }

        private static int GetReplacementStart(bool removeConditionalAccess, SyntaxToken token)
        {
            var replacementStart = token.SpanStart;
            if (removeConditionalAccess)
            {
                if (token.Parent is MemberBindingExpressionSyntax memberBinding &&
                    memberBinding.GetParentConditionalAccessExpression() is { } conditional)
                {
                    replacementStart = conditional.OperatorToken.SpanStart;
                }
            }

            return replacementStart;
        }
    }
}
