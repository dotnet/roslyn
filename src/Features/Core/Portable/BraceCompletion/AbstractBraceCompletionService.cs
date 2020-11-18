// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BraceCompletion
{
    internal abstract class AbstractBraceCompletionService : IBraceCompletionService
    {
        protected abstract char OpeningBrace { get; }

        protected abstract char ClosingBrace { get; }

        /// <summary>
        /// Returns if the token is a valid opening token kind for this brace completion service.
        /// </summary>
        protected abstract bool IsValidOpeningBraceToken(SyntaxToken token);

        /// <summary>
        /// Returns if the token is a valid closing token kind for this brace completion service.
        /// </summary>
        protected abstract bool IsValidClosingBraceToken(SyntaxToken token);

        ///<inheritdoc cref="IBraceCompletionService.AllowOverTypeAsync(BraceCompletionContext, CancellationToken)"/>
        public abstract Task<bool> AllowOverTypeAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

        ///<inheritdoc cref="IBraceCompletionService.GetBraceCompletionAsync(BraceCompletionContext, CancellationToken)"/>
        public async Task<BraceCompletionResult?> GetBraceCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
        {
            var closingPoint = braceCompletionContext.ClosingPoint;
            if (closingPoint < 1)
            {
                return null;
            }

            var openingPoint = braceCompletionContext.OpeningPoint;
            var document = braceCompletionContext.Document;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (sourceText[openingPoint] != OpeningBrace)
            {
                return null;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(openingPoint, findInsideTrivia: true);
            var validOpeningPoint = await IsValidOpenBraceTokenAtPositionAsync(token, openingPoint, document, cancellationToken).ConfigureAwait(false);
            if (!validOpeningPoint)
            {
                return null;
            }

            var braceTextEdit = new TextChange(TextSpan.FromBounds(closingPoint, closingPoint), ClosingBrace.ToString());
            // The caret location should be in between the braces.
            var originalOpeningLinePosition = sourceText.Lines.GetLinePosition(openingPoint);
            var caretLocation = new LinePosition(originalOpeningLinePosition.Line, originalOpeningLinePosition.Character + 1);
            return new BraceCompletionResult(ImmutableArray.Create(braceTextEdit), caretLocation);
        }

        ///<inheritdoc cref="IBraceCompletionService.GetTextChangesAfterCompletionAsync(BraceCompletionContext, CancellationToken)"/>
        public virtual Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        ///<inheritdoc cref="IBraceCompletionService.GetTextChangeAfterReturnAsync(BraceCompletionContext, CancellationToken)"/>
        public virtual Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        ///<inheritdoc cref="IBraceCompletionService.IsValidForBraceCompletionAsync(char, int, Document, CancellationToken)"/>
        public virtual async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // check that the user is not typing in a string literal or comment
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return OpeningBrace == brace && !syntaxFactsService.IsInNonUserCode(tree, openingPosition, cancellationToken);
        }

        ///<inheritdoc cref="IBraceCompletionService.IsInsideCompletedBracesAsync(int, Document, CancellationToken)"/>
        public async Task<BraceCompletionContext?> IsInsideCompletedBracesAsync(int caretLocation, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var leftToken = root.FindTokenOnLeftOfPosition(caretLocation);
            var rightToken = root.FindTokenOnRightOfPosition(caretLocation);

            if (IsValidOpeningBraceToken(leftToken) && IsValidClosingBraceToken(rightToken))
            {
                return new BraceCompletionContext(document, leftToken.GetLocation().SourceSpan.Start, rightToken.GetLocation().SourceSpan.End, caretLocation);
            }

            return null;
        }

        /// <summary>
        /// Checks if the already inserted token is a valid opening token at the position in the document.
        /// </summary>
        protected virtual Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (!IsParentSkippedTokensTrivia(syntaxFactsService, token))
            {
                return SpecializedTasks.False;
            }

            return Task.FromResult(IsValidOpeningBraceToken(token) && token.SpanStart == position);
        }

        /// <summary>
        /// Checks that the current position is a valid location.
        /// Used to determine if overtype should be allowed.
        /// </summary>
        protected static async Task<bool> CheckCurrentPositionAsync(Document document, int? currentPosition, CancellationToken cancellationToken)
        {
            // make sure auto closing is called from a valid position
            if (currentPosition == null)
            {
                return false;
            }

            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return !syntaxFactsService.IsInNonUserCode(tree, currentPosition.Value, cancellationToken);
        }

        protected static bool IsParentSkippedTokensTrivia(ISyntaxFactsService syntaxFactsService, SyntaxToken token)
            => token.Parent != null && !syntaxFactsService.IsSkippedTokensTrivia(token.Parent);

        /// <summary>
        /// Checks that the token at the closing position is a valid closing token.
        /// </summary>
        protected async Task<bool> CheckClosingTokenKindAsync(Document document, int closingPosition, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var closingToken = root.FindTokenFromEnd(closingPosition, includeZeroWidth: false, findInsideTrivia: true);
            return IsValidClosingBraceToken(closingToken);
        }

        public static class CurlyBrace
        {
            public const char OpenCharacter = '{';
            public const char CloseCharacter = '}';
        }

        public static class Parenthesis
        {
            public const char OpenCharacter = '(';
            public const char CloseCharacter = ')';
        }

        public static class Bracket
        {
            public const char OpenCharacter = '[';
            public const char CloseCharacter = ']';
        }

        public static class LessAndGreaterThan
        {
            public const char OpenCharacter = '<';
            public const char CloseCharacter = '>';
        }

        public static class DoubleQuote
        {
            public const char OpenCharacter = '"';
            public const char CloseCharacter = '"';
        }

        public static class SingleQuote
        {
            public const char OpenCharacter = '\'';
            public const char CloseCharacter = '\'';
        }
    }
}
