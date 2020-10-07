// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal abstract class OperatorIndexerCompletionProviderBase : LSPCompletionProvider
    {
        // CompletionItems for indexers/operators should be sorted below other suggestions like methods or properties of the type.
        // Identifier (of methods or properties) can start with "A Unicode character of classes Lu, Ll, Lt, Lm, Lo, or Nl". https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/lexical-structure#identifiers
        // Sorting is done via StringComparer.OrdinalIgnoreCase which compares the utf-16 bytes after converting to uppercase.
        // \ufffd http://www.fileformat.info/info/unicode/char/fffd/index.htm is the largest possible value for utf-16
        // and is also greater than surrogate pairs, if byte comparison is used. The "biggest" possible characters are 
        // \u3134a http://www.fileformat.info/info/unicode/char/3134a/index.htm surrogate pair "\ud884\udf4a" and
        // \uffdc http://www.fileformat.info/info/unicode/char/ffdc/index.htm (non-surrogate)
        private const string SortingPrefix = "\uFFFD";

        protected abstract int SortingGroupIndex { get; } // Indexer, operators and conversion should be listed grouped together.

        protected abstract ImmutableArray<CompletionItem> GetCompletionItemsForTypeSymbol(
            SemanticModel semanticModel,
            ITypeSymbol container,
            int position,
            bool isAccessedByConditionalAccess);

        internal override ImmutableHashSet<char> TriggerCharacters => ImmutableHashSet.Create('.');

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
            => text[insertedCharacterPosition] == '.';

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        protected static ImmutableDictionary<string, string> CreatePropertiesBag(params (string key, string value)[] properties)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            foreach (var (key, value) in properties)
            {
                builder.Add(key, value);
            }

            return builder.ToImmutable();
        }

        protected string SortText(string? sortTextSymbolPart = null)
            => $"{SortingPrefix}{SortingGroupIndex:000}{sortTextSymbolPart}";

        protected static (SyntaxToken tokenAtPosition, SyntaxToken potentialDotTokenLeftOfCursor) FindTokensAtPosition(int position, SyntaxNode root)
        {
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position, includeSkipped: true);
            var potentialDotTokenLeftOfCursor = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
            return (tokenAtPosition, potentialDotTokenLeftOfCursor);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var position = context.Position;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var (_, potentialDotToken) = FindTokensAtPosition(position, root);
            if (!potentialDotToken.IsKind(SyntaxKind.DotToken))
            {
                return;
            }

            var expression = GetParentExpressionOfToken(potentialDotToken);
            if (expression is null || expression.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                return;
            }
            var isAccessedByConditionalAccess = expression.GetRootConditionalAccessExpression() is not null;

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(expression.SpanStart, cancellationToken).ConfigureAwait(false);
            var container = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
            if (container is null)
            {
                return;
            }

            var expressionSymbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            if (expressionSymbolInfo.Symbol is INamedTypeSymbol)
            {
                // Expression looks like an access to a static member. Operator, indexer and conversions are not applicable.
                return;
            }

            if (expression.IsInsideNameOfExpression(semanticModel, cancellationToken))
            {
                return;
            }

            var completionItems = GetCompletionItemsForTypeSymbol(semanticModel, container, position, isAccessedByConditionalAccess);
            context.AddItems(completionItems);
        }

        protected static SyntaxToken? FindTokenToRemoveAtCursorPosition(SyntaxToken tokenAtCursor) => tokenAtCursor switch
        {
            var token when token.IsKind(SyntaxKind.IdentifierToken) => token,
            var token when token.IsKeyword() => token,
            _ => null,
        };

        /// <summary>
        /// Returns the expression left to the passed dot <paramref name="token"/>.
        /// </summary>
        /// <example>
        /// Expression: a.b?.ccc.
        /// Token     :  ↑  ↑   ↑
        /// Returns   :  a  a.b .ccc
        /// </example>
        /// <param name="token">A dot token.</param>
        /// <returns>The expression left to the dot token or null.</returns>
        protected static ExpressionSyntax? GetParentExpressionOfToken(SyntaxToken token)
        {
            var syntaxNode = token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
                MemberBindingExpressionSyntax memberBinding => memberBinding.GetParentConditionalAccessExpression()?.Expression,
                _ => null,
            };
        }

        /// <summary>
        /// Returns the expression left to the passed dot <paramref name="token"/>.
        /// </summary>
        /// <example>
        /// Given the expression a.b?.c.d. returns a.b?.c.d. for all dot tokens
        /// </example>
        /// <param name="token">A dot token.</param>
        /// <returns>The root expression associated with the dot or null.</returns>
        protected static ExpressionSyntax? GetRootExpressionOfToken(SyntaxToken token)
        {
            var syntaxNode = token.Parent;
            return syntaxNode switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Expression.GetRootConditionalAccessExpression() ?? (ExpressionSyntax)memberAccess,
                MemberBindingExpressionSyntax memberBinding => memberBinding.GetRootConditionalAccessExpression(),
                _ => null,
            };
        }

        protected static async Task<CompletionChange?> ReplaceDotAndTokenAfterWithTextAsync(Document document, CompletionItem item, string text, bool removeConditionalAccess, int positionOffset, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var position = SymbolCompletionItem.GetContextPosition(item);
            var tokenAtPosition = root.FindTokenOnLeftOfPosition(position);
            var token = tokenAtPosition.GetPreviousTokenIfTouchingWord(position);
            if (token.IsKind(SyntaxKind.DotToken))
            {
                var replacementStart = GetReplacementStart(removeConditionalAccess, token);
                var newPosition = replacementStart + text.Length + positionOffset;
                var replaceSpan = TextSpan.FromBounds(replacementStart, tokenAtPosition.Span.End);
                return CompletionChange.Create(new TextChange(replaceSpan, text), newPosition);
            }

            return null;
        }

        private static int GetReplacementStart(bool removeConditionalAccess, SyntaxToken token)
        {
            var replacementStart = token.SpanStart;
            if (removeConditionalAccess)
            {
                if (token.Parent is MemberBindingExpressionSyntax memberBinding && memberBinding.GetParentConditionalAccessExpression() is { } conditional)
                {
                    replacementStart = conditional.OperatorToken.SpanStart;
                }
            }

            return replacementStart;
        }
    }
}
