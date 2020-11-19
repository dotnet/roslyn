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

        public abstract Task<bool> AllowOverTypeAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

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

        public virtual Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // check that the user is not typing in a string literal or comment
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return OpeningBrace == brace && !syntaxFactsService.IsInNonUserCode(tree, openingPosition, cancellationToken);
        }

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
        /// By default checks that the opening token is a valid token at the position and not in skipped token trivia.
        /// </summary>
        protected virtual Task<bool> IsValidOpenBraceTokenAtPositionAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            
            // The open token is typed in skipped token trivia, we should not attempt to complete it.
            if (ParentIsSkippedTokensTrivia(syntaxFactsService, token))
            {
                return SpecializedTasks.False;
            }

            return Task.FromResult(token.SpanStart == position && IsValidOpeningBraceToken(token));
        }

        /// <summary>
        /// Returns true when the current position is inside user code (e.g. not strings) and the closing token
        /// matches the expected closing token for this brace completion service.
        /// Helper method used by <see cref="AllowOverTypeAsync(BraceCompletionContext, CancellationToken)"/> implementations.
        /// </summary>
        protected async Task<bool> AllowOverTypeInUserCodeWithValidClosingTokenAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            return await IsCurrentPositionInUserCodeAsync(context.Document, context.CaretLocation, cancellationToken).ConfigureAwait(false)
                && await CheckClosingTokenKindAsync(context.Document, context.ClosingPoint, cancellationToken).ConfigureAwait(false);

            static async Task<bool> IsCurrentPositionInUserCodeAsync(Document document, int currentPosition, CancellationToken cancellationToken)
            {
                var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
                return !syntaxFactsService.IsInNonUserCode(tree, currentPosition, cancellationToken);
            }
        }

        /// <summary>
        /// Returns true when the closing token matches the expected closing token for this brace completion service.
        /// Used by <see cref="AllowOverTypeAsync(BraceCompletionContext, CancellationToken)"/> implementations
        /// when the over type could be triggered from outside of user code (e.g. overtyping end quotes in a string).
        /// </summary>
        protected Task<bool> AllowOverTypeWithValidClosingTokenAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            return CheckClosingTokenKindAsync(context.Document, context.ClosingPoint, cancellationToken);
        }

        protected static bool ParentIsSkippedTokensTrivia(ISyntaxFactsService syntaxFactsService, SyntaxToken token)
            => token.Parent == null || syntaxFactsService.IsSkippedTokensTrivia(token.Parent);

        /// <summary>
        /// Checks that the token at the closing position is a valid closing token.
        /// </summary>
        private async Task<bool> CheckClosingTokenKindAsync(Document document, int closingPosition, CancellationToken cancellationToken)
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
