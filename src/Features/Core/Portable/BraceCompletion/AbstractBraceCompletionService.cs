// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BraceCompletion
{
    internal abstract class AbstractBraceCompletionService : IBraceCompletionService
    {
        protected abstract ISyntaxFacts SyntaxFacts { get; }

        protected abstract char OpeningBrace { get; }
        protected abstract char ClosingBrace { get; }

        /// <summary>
        /// Whether or not this brace completion session actually needs semantics to work (and thus should get a semantic model).
        /// </summary>
        protected virtual bool NeedsSemantics => false;

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
                return null;

            var openingPoint = braceCompletionContext.OpeningPoint;
            var document = braceCompletionContext.Document;

            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (sourceText[openingPoint] != OpeningBrace)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(openingPoint, findInsideTrivia: true);

            if (NeedsSemantics)
            {
                // Pass along a document with frozen partial semantics.  Brace completion is a highly latency sensitive
                // operation.  We don't want to wait on things like source generators to figure things out.
                var validOpeningPoint = await IsValidOpenBraceTokenAtPositionAsync(
                     document.WithFrozenPartialSemantics(cancellationToken), token, openingPoint, cancellationToken).ConfigureAwait(false);
                if (!validOpeningPoint)
                    return null;
            }
            else
            {
                var validOpeningPoint = IsValidOpenBraceTokenAtPosition(sourceText, token, openingPoint);
                if (!validOpeningPoint)
                    return null;
            }

            var braceTextEdit = new TextChange(TextSpan.FromBounds(closingPoint, closingPoint), ClosingBrace.ToString());

            // The caret location should be in between the braces.
            var originalOpeningLinePosition = sourceText.Lines.GetLinePosition(openingPoint);
            var caretLocation = new LinePosition(originalOpeningLinePosition.Line, originalOpeningLinePosition.Character + 1);
            return new BraceCompletionResult(ImmutableArray.Create(braceTextEdit), caretLocation);
        }

        public virtual Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual async Task<bool> CanProvideBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            if (OpeningBrace != brace)
            {
                return false;
            }

            // check that the user is not typing in a string literal or comment
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return !syntaxFactsService.IsInNonUserCode(tree, openingPosition, cancellationToken);
        }

        public async Task<BraceCompletionContext?> GetCompletedBraceContextAsync(Document document, int caretLocation, CancellationToken cancellationToken)
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
        /// Only called if <see cref="NeedsSemantics"/> returns true;
        /// </summary>
        protected virtual ValueTask<bool> IsValidOpenBraceTokenAtPositionAsync(Document document, SyntaxToken token, int position, CancellationToken cancellationToken)
        {
            // Subclass should have overridden this.
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Checks if the already inserted token is a valid opening token at the position in the document.
        /// By default checks that the opening token is a valid token at the position and not in skipped token trivia.
        /// </summary>
        protected virtual bool IsValidOpenBraceTokenAtPosition(SourceText text, SyntaxToken token, int position)
            => token.SpanStart == position && IsValidOpeningBraceToken(token) && !ParentIsSkippedTokensTriviaOrNull(this.SyntaxFacts, token);

        /// <summary>
        /// Returns true when the current position is inside user code (e.g. not strings) and the closing token
        /// matches the expected closing token for this brace completion service.
        /// Helper method used by <see cref="AllowOverTypeAsync(BraceCompletionContext, CancellationToken)"/> implementations.
        /// </summary>
        protected async Task<bool> AllowOverTypeInUserCodeWithValidClosingTokenAsync(BraceCompletionContext context, CancellationToken cancellationToken)
        {
            var tree = await context.Document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = context.Document.GetRequiredLanguageService<ISyntaxFactsService>();

            return !syntaxFactsService.IsInNonUserCode(tree, context.CaretLocation, cancellationToken)
                && await CheckClosingTokenKindAsync(context.Document, context.ClosingPoint, cancellationToken).ConfigureAwait(false);
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

        protected static bool ParentIsSkippedTokensTriviaOrNull(ISyntaxFacts syntaxFacts, SyntaxToken token)
            => token.Parent == null || syntaxFacts.IsSkippedTokensTrivia(token.Parent);

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

        /// <summary>
        /// Determines if inserting the opening brace at the location could be an attempt to
        /// escape a previously inserted opening brace.
        /// E.g. they are trying to type $"{{"
        /// </summary>
        protected static async Task<bool> CouldEscapePreviousOpenBraceAsync(char openingBrace, int position, Document document, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var index = position - 1;
            var openBraceCount = 0;
            while (index >= 0)
            {
                if (text[index] == openingBrace)
                {
                    openBraceCount++;
                }
                else
                {
                    break;
                }

                index--;
            }

            if (openBraceCount > 0 && openBraceCount % 2 == 1)
            {
                return true;
            }

            return false;
        }
    }
}
