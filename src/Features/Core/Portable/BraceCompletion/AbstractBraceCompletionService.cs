// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BraceCompletion;

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

    public abstract bool AllowOverType(BraceCompletionContext braceCompletionContext, CancellationToken cancellationToken);

    public Task<bool> HasBraceCompletionAsync(BraceCompletionContext context, Document document, CancellationToken cancellationToken)
    {
        if (!context.HasCompletionForOpeningBrace(OpeningBrace))
        {
            return Task.FromResult(false);
        }

        var openingToken = context.GetOpeningToken();
        if (!NeedsSemantics)
        {
            return Task.FromResult(IsValidOpenBraceTokenAtPosition(context.Document.Text, openingToken, context.OpeningPoint));
        }

        // Pass along a document with frozen partial semantics.  Brace completion is a highly latency sensitive
        // operation.  We don't want to wait on things like source generators to figure things out.
        return IsValidOpenBraceTokenAtPositionAsync(document.WithFrozenPartialSemantics(cancellationToken), openingToken, context.OpeningPoint, cancellationToken);
    }

    public BraceCompletionResult GetBraceCompletion(BraceCompletionContext context)
    {
        Debug.Assert(context.HasCompletionForOpeningBrace(OpeningBrace));

        var closingPoint = context.ClosingPoint;
        var braceTextEdit = new TextChange(TextSpan.FromBounds(closingPoint, closingPoint), ClosingBrace.ToString());

        // The caret location should be in between the braces.
        var originalOpeningLinePosition = context.Document.Text.Lines.GetLinePosition(context.OpeningPoint);
        var caretLocation = new LinePosition(originalOpeningLinePosition.Line, originalOpeningLinePosition.Character + 1);
        return new BraceCompletionResult([braceTextEdit], caretLocation);
    }

    public virtual BraceCompletionResult? GetTextChangesAfterCompletion(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken)
        => null;

    public virtual BraceCompletionResult? GetTextChangeAfterReturn(BraceCompletionContext braceCompletionContext, IndentationOptions options, CancellationToken cancellationToken)
        => null;

    public virtual bool CanProvideBraceCompletion(char brace, int openingPosition, ParsedDocument document, CancellationToken cancellationToken)
    {
        if (OpeningBrace != brace)
        {
            return false;
        }

        // check that the user is not typing in a string literal or comment
        var syntaxFactsService = document.LanguageServices.GetRequiredService<ISyntaxFactsService>();

        return !syntaxFactsService.IsInNonUserCode(document.SyntaxTree, openingPosition, cancellationToken);
    }

    public BraceCompletionContext? GetCompletedBraceContext(ParsedDocument document, int caretLocation)
    {
        var leftToken = document.Root.FindTokenOnLeftOfPosition(caretLocation);
        var rightToken = document.Root.FindTokenOnRightOfPosition(caretLocation);

        if (IsValidOpeningBraceToken(leftToken) && IsValidClosingBraceToken(rightToken))
        {
            return new BraceCompletionContext(document, leftToken.GetLocation().SourceSpan.Start, rightToken.GetLocation().SourceSpan.End, caretLocation);
        }

        return null;
    }

    /// <summary>
    /// Only called if <see cref="NeedsSemantics"/> returns true;
    /// </summary>
    protected virtual Task<bool> IsValidOpenBraceTokenAtPositionAsync(Document document, SyntaxToken token, int position, CancellationToken cancellationToken)
    {
        // Subclass should have overridden this.
        throw ExceptionUtilities.Unreachable();
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
    /// Helper method used by <see cref="AllowOverType(BraceCompletionContext, CancellationToken)"/> implementations.
    /// </summary>
    protected bool AllowOverTypeInUserCodeWithValidClosingToken(BraceCompletionContext context, CancellationToken cancellationToken)
    {
        var tree = context.Document.SyntaxTree;
        var syntaxFactsService = context.Document.LanguageServices.GetRequiredService<ISyntaxFactsService>();

        return !syntaxFactsService.IsInNonUserCode(tree, context.CaretLocation, cancellationToken)
            && CheckClosingTokenKind(context.Document, context.ClosingPoint);
    }

    /// <summary>
    /// Returns true when the closing token matches the expected closing token for this brace completion service.
    /// Used by <see cref="AllowOverType(BraceCompletionContext, CancellationToken)"/> implementations
    /// when the over type could be triggered from outside of user code (e.g. overtyping end quotes in a string).
    /// </summary>
    protected bool AllowOverTypeWithValidClosingToken(BraceCompletionContext context)
    {
        return CheckClosingTokenKind(context.Document, context.ClosingPoint);
    }

    protected static bool ParentIsSkippedTokensTriviaOrNull(ISyntaxFacts syntaxFacts, SyntaxToken token)
        => token.Parent == null || syntaxFacts.IsSkippedTokensTrivia(token.Parent);

    /// <summary>
    /// Checks that the token at the closing position is a valid closing token.
    /// </summary>
    private bool CheckClosingTokenKind(ParsedDocument document, int closingPosition)
    {
        var closingToken = document.Root.FindTokenFromEnd(closingPosition, includeZeroWidth: false, findInsideTrivia: true);
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
    protected static bool CouldEscapePreviousOpenBrace(char openingBrace, int position, SourceText text)
    {
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
