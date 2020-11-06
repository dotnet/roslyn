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

        protected abstract bool IsValidOpeningBraceToken(SyntaxToken token);

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
            var validOpeningPoint = await CheckOpeningPointAsync(token, openingPoint, document, cancellationToken).ConfigureAwait(false);
            if (!validOpeningPoint)
            {
                return null;
            }

            var braceTextEdit = new TextChange(TextSpan.FromBounds(closingPoint, closingPoint), ClosingBrace.ToString());
            var newText = sourceText.WithChanges(braceTextEdit);
            // The caret location should be right before where the closing brace was inserted.
            return new BraceCompletionResult(newText, ImmutableArray.Create(ImmutableArray.Create(braceTextEdit)), caretLocation: openingPoint + 1);
        }

        public virtual Task<BraceCompletionResult?> GetTextChangesAfterCompletionAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual Task<BraceCompletionResult?> GetTextChangeAfterReturnAsync(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken, bool supportsVirtualSpace = true)
            => SpecializedTasks.Default<BraceCompletionResult?>();

        public virtual async Task<bool> IsValidForBraceCompletionAsync(char brace, int openingPosition, Document document, CancellationToken cancellationToken)
        {
            // check that the user is not typing in a string literal or comment
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();

            return OpeningBrace == brace && !syntaxFactsService.IsInNonUserCode(tree, openingPosition, cancellationToken);
        }

        public BraceCompletionContext? IsInsideCompletedBraces(int caretLocation, SyntaxNode root, Document document)
        {
            var leftToken = root.FindTokenOnLeftOfPosition(caretLocation);
            var rightToken = root.FindTokenOnRightOfPosition(caretLocation);

            if (IsValidOpeningBraceToken(leftToken) && IsValidClosingBraceToken(rightToken))
            {
                return new BraceCompletionContext(document, leftToken.GetLocation().SourceSpan.Start, rightToken.GetLocation().SourceSpan.End, caretLocation);
            }

            return null;
        }

        protected virtual Task<bool> CheckOpeningPointAsync(SyntaxToken token, int position, Document document, CancellationToken cancellationToken)
        {
            var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
            if (!IsParentSkippedTokensTrivia(syntaxFactsService, token))
            {
                return SpecializedTasks.False;
            }

            return Task.FromResult(IsValidOpeningBraceToken(token) && token.SpanStart == position);
        }

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
