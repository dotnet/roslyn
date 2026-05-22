// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SyntaxToken = Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax.SyntaxToken;
using SyntaxFactory = Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax.SyntaxFactory;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using CSharpSyntaxToken = Microsoft.CodeAnalysis.SyntaxToken;
using CSharpSyntaxTriviaList = Microsoft.CodeAnalysis.SyntaxTriviaList;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

#pragma warning disable RSEXPERIMENTAL003 // SyntaxTokenParser is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class RoslynCSharpTokenizer : CSharpTokenizer
{
    private readonly SyntaxTokenParser _roslynTokenParser;
    /// <summary>
    /// The current trivia enumerator that we're parsing through. This is a tuple of the enumerator and whether it's leading trivia.
    /// When this is non-null, we're in the <see cref="RoslynCSharpTokenizerState.TriviaForCSharpToken"/> state.
    /// </summary>
    private (CSharpSyntaxTriviaList.Enumerator enumerator, bool isLeading)? _currentCSharpTokenTriviaEnumerator;
    /// <summary>
    /// Previous result checkpoints that we can reset <see cref="_roslynTokenParser"/> to. This must be an ordered list
    /// by position, where the position is the start of the token that was parsed including leading trivia, so that searching
    /// is correct when performing a reset.
    /// </summary>
    private readonly List<(int position, SyntaxTokenParser.Result result, bool isOnlyWhitespaceOnLine)> _resultCache = ListPool<(int, SyntaxTokenParser.Result, bool)>.Default.Get();

    private bool _isOnlyWhitespaceOnLine = true;

    public RoslynCSharpTokenizer(SeekableTextReader source, CSharpParseOptions parseOptions)
        : base(source)
    {
        base.CurrentState = StartState;

        _roslynTokenParser = CodeAnalysis.CSharp.SyntaxFactory.CreateTokenParser(source.SourceText, parseOptions);
    }

    protected override int StartState => (int)RoslynCSharpTokenizerState.Start;

    private new RoslynCSharpTokenizerState? CurrentState
    {
        get => (RoslynCSharpTokenizerState?)base.CurrentState;
        set => base.CurrentState = (int?)value;
    }

    public override SyntaxKind RazorCommentKind => SyntaxKind.RazorCommentLiteral;

    public override SyntaxKind RazorCommentTransitionKind => SyntaxKind.RazorCommentTransition;

    public override SyntaxKind RazorCommentStarKind => SyntaxKind.RazorCommentStar;

    internal override void StartingBlock()
    {
        _roslynTokenParser.SkipForwardTo(Source.Position);
        ResetIsOnlyWhitespaceOnLine();
    }

    private void ResetIsOnlyWhitespaceOnLine()
    {
        // Reset isOnlyWhitespaceOnLine for the new block
        _isOnlyWhitespaceOnLine = true;
        for (int i = Source.Position - 1; i >= 0; i--)
        {
            var currentChar = Source.SourceText[i];
            if (currentChar is '\n' or '\r')
            {
                break;
            }
            else if (!SyntaxFacts.IsWhitespace(currentChar))
            {
                _isOnlyWhitespaceOnLine = false;
                break;
            }
        }
    }

    internal override void EndingBlock()
    {
        // We should always be transitioning to the other parser in response to content. This either means that the CSharp parser put the token it saw back, meaning that we're
        // in the Start state, or it means that we'll have parsed a token, and be in the TriviaForCSharpToken state. If we're in the Start state, there's nothing for us to do.
        // In order to ensure that we properly handle the trailing trivia (because the other parser will handle the trailing trivia on the node we found, if any), we need to
        // reset back before the start of that node, skip the content, and reset our state back to Start for when we're called back next.
        if (CurrentState == RoslynCSharpTokenizerState.Start)
        {
            return;
        }

        Debug.Assert(CurrentState == RoslynCSharpTokenizerState.TriviaForCSharpToken, $"Unexpected state: {CurrentState}");
        Debug.Assert(_currentCSharpTokenTriviaEnumerator is (_, isLeading: false));
        Debug.Assert(_resultCache.Count > 0);

        var (_, result, isOnlyWhitespaceOnLine) = _resultCache[^1];
        _roslynTokenParser.ResetTo(result);
        _isOnlyWhitespaceOnLine = isOnlyWhitespaceOnLine;
        _resultCache.RemoveAt(_resultCache.Count - 1);
        var lastToken = result.Token;
        if (lastToken.HasLeadingTrivia)
        {
            // If the previous token did indeed have leading trivia, we need to make sure to take it into account so that any preprocessor directives are seen by the
            // roslyn token parser
            _ = GetNextResult(NextResultType.LeadingTrivia);
        }

        _roslynTokenParser.SkipForwardTo(lastToken.Span.End);
        CurrentState = RoslynCSharpTokenizerState.Start;
    }

    protected override StateResult Dispatch()
    {
        switch (CurrentState)
        {
            case RoslynCSharpTokenizerState.Start:
                return Start();
            case RoslynCSharpTokenizerState.TriviaForCSharpToken:
                return Trivia();
            case RoslynCSharpTokenizerState.Token:
                return Token();
            case RoslynCSharpTokenizerState.OnRazorCommentStar:
                return OnRazorCommentStar();
            case RoslynCSharpTokenizerState.AfterRazorCommentTransition:
                return AfterRazorCommentTransition();
            case RoslynCSharpTokenizerState.RazorCommentBody:
                return RazorCommentBody();
            case RoslynCSharpTokenizerState.StarAfterRazorCommentBody:
                return StarAfterRazorCommentBody();
            case RoslynCSharpTokenizerState.AtTokenAfterRazorCommentBody:
                Debug.Assert(_currentCSharpTokenTriviaEnumerator is not null);
                _isOnlyWhitespaceOnLine = false;
                return AtTokenAfterRazorCommentBody(nextState: (int)RoslynCSharpTokenizerState.TriviaForCSharpToken);
            default:
                Debug.Fail("Invalid TokenizerState");
                return default(StateResult);
        }
    }

    // Optimize memory allocation by returning constants for the most frequent cases
    protected override string GetTokenContent(SyntaxKind type)
    {
        Debug.Assert(type != SyntaxKind.CSharpOperator, "CSharpOperator should be handled by getting the interned text from C#");
        var tokenLength = Buffer.Length;

        if (tokenLength == 1)
        {
            switch (type)
            {
                case SyntaxKind.NewLine:
                    if (Buffer[0] == '\n')
                    {
                        return "\n";
                    }
                    break;
                case SyntaxKind.Whitespace:
                    if (Buffer[0] == ' ')
                    {
                        return " ";
                    }
                    if (Buffer[0] == '\t')
                    {
                        return "\t";
                    }
                    break;
                case SyntaxKind.NumericLiteral:
                    Debug.Fail("This should be handled by using the C# lexer's interned string in NumericLiteral()");
                    return base.GetTokenContent(type);
                case SyntaxKind.Not:
                case SyntaxKind.LeftParenthesis:
                case SyntaxKind.RightParenthesis:
                case SyntaxKind.Comma:
                case SyntaxKind.Dot:
                case SyntaxKind.Colon:
                case SyntaxKind.Semicolon:
                case SyntaxKind.QuestionMark:
                case SyntaxKind.RightBracket:
                case SyntaxKind.LeftBracket:
                case SyntaxKind.LeftBrace:
                case SyntaxKind.RightBrace:
                case SyntaxKind.LessThan:
                case SyntaxKind.Assign:
                case SyntaxKind.GreaterThan:
                    Debug.Fail("This should be handled by using the C# lexer's interned string in Operator()");
                    return base.GetTokenContent(type);
                case SyntaxKind.Transition:
                    return "@";

            }
        }
        else if (tokenLength == 2)
        {
            switch (type)
            {
                case SyntaxKind.NewLine:
                    return "\r\n";
                case SyntaxKind.DoubleColon:
                case SyntaxKind.Equals:
                    Debug.Fail("This should be handled by using the C# lexer's interned string in Operator()");
                    return base.GetTokenContent(type);
            }
        }

        return base.GetTokenContent(type);
    }

    protected override SyntaxToken CreateToken(string content, SyntaxKind kind, RazorDiagnostic[] errors)
    {
        return SyntaxFactory.Token(kind, content, errors);
    }

    private StateResult Start()
    {
        var leadingTriviaResult = GetNextResult(NextResultType.LeadingTrivia);
        Debug.Assert(leadingTriviaResult.ContextualKind == CSharpSyntaxKind.None);
        Debug.Assert(leadingTriviaResult.Token.IsKind(CSharpSyntaxKind.None));

        if (leadingTriviaResult.Token.HasLeadingTrivia)
        {
            _currentCSharpTokenTriviaEnumerator = (leadingTriviaResult.Token.LeadingTrivia.GetEnumerator(), isLeading: true);
            return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, null);
        }
        else
        {
            return Transition(RoslynCSharpTokenizerState.Token, null);
        }
    }

    private StateResult Token()
    {
        if (SyntaxFacts.IsNewLine(CurrentCharacter) || SyntaxFacts.IsWhitespace(CurrentCharacter))
        {
            Assumed.Unreachable();
        }

        if (SyntaxFacts.IsIdentifierStartCharacter(CurrentCharacter))
        {
            return Identifier();
        }
        else if (char.IsDigit(CurrentCharacter))
        {
            return NumericLiteral();
        }
        switch (CurrentCharacter)
        {
            case '@':
                return AtToken();
            case '\'':
                return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.Character);
            case '"':
                return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.String_Or_Raw_String);
            case '$':
                switch (Peek())
                {
                    case '"' or '$':
                        return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.Interpolated_Or_Raw_Interpolated_String);
                    case '@' when Peek(2) == '"':
                        return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.Verbatim_Interpolated_Dollar_First_String);
                }
                goto default;
            case '.':
                if (char.IsDigit(Peek()))
                {
                    return NumericLiteral();
                }
                return Operator();
            case '/' when Peek() is '/' or '*':
                return Assumed.Unreachable<StateResult>();
            default:
                return Operator();
        }
    }

    private StateResult AtToken()
    {
        AssertCurrent('@');
        switch (Peek())
        {
            case '"':
                return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.Verbatim_String);
            case '$' when Peek(2) is '"':
                return TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind.Verbatim_Interpolated_At_First_String);
            case '*':
                return Assumed.Unreachable<StateResult>();
            case '@':
                // Escaped razor transition. Likely will error in the parser.
                AddResetPoint();
                TakeCurrent();
                _isOnlyWhitespaceOnLine = false;
                _roslynTokenParser.SkipForwardTo(Source.Position);
                AssertCurrent('@');
                return Transition(RoslynCSharpTokenizerState.Token, EndToken(SyntaxKind.Transition));
            default:
                // Either a standard razor transition or a C# identifier. The parser will take care of stitching together the transition and the
                // identifier if it's the latter case.
                AddResetPoint();
                TakeCurrent();
                _isOnlyWhitespaceOnLine = false;
                _roslynTokenParser.SkipForwardTo(Source.Position);
                var trailingTrivia = GetNextResult(NextResultType.TrailingTrivia);
                _currentCSharpTokenTriviaEnumerator = (trailingTrivia.Token.TrailingTrivia.GetEnumerator(), isLeading: false);
                return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, EndToken(SyntaxKind.Transition));
        }

        void AddResetPoint()
        {
            // We want to make it easy to reset the tokenizer back to just before this token; we can do that very simply by trying to parse
            // leading trivia, which gives us a reset point. We know that we can't have any leading trivia, since we're on an `@` character.
            var nextResult = GetNextResult(NextResultType.LeadingTrivia);
            Debug.Assert(nextResult.Token.IsKind(CSharpSyntaxKind.None));
            Debug.Assert(nextResult.Token.FullSpan.Length == 0);
        }
    }

    private StateResult Operator()
    {
        var result = GetNextResult(NextResultType.Token);
        var token = result.Token;

        AdvancePastToken(token);
        string content;
        var kind = token.RawKind switch
        {
            (int)CSharpSyntaxKind.ExclamationToken => SyntaxKind.Not,
            (int)CSharpSyntaxKind.OpenParenToken => SyntaxKind.LeftParenthesis,
            (int)CSharpSyntaxKind.CloseParenToken => SyntaxKind.RightParenthesis,
            (int)CSharpSyntaxKind.CommaToken => SyntaxKind.Comma,
            (int)CSharpSyntaxKind.DotToken => SyntaxKind.Dot,
            (int)CSharpSyntaxKind.ColonColonToken => SyntaxKind.DoubleColon,
            (int)CSharpSyntaxKind.ColonToken => SyntaxKind.Colon,
            (int)CSharpSyntaxKind.OpenBraceToken => SyntaxKind.LeftBrace,
            (int)CSharpSyntaxKind.CloseBraceToken => SyntaxKind.RightBrace,
            (int)CSharpSyntaxKind.LessThanToken => SyntaxKind.LessThan,
            (int)CSharpSyntaxKind.GreaterThanToken => SyntaxKind.GreaterThan,
            (int)CSharpSyntaxKind.EqualsToken => SyntaxKind.Assign,
            (int)CSharpSyntaxKind.OpenBracketToken => SyntaxKind.LeftBracket,
            (int)CSharpSyntaxKind.CloseBracketToken => SyntaxKind.RightBracket,
            (int)CSharpSyntaxKind.QuestionToken => SyntaxKind.QuestionMark,
            (int)CSharpSyntaxKind.SemicolonToken => SyntaxKind.Semicolon,
            <= (int)CSharpSyntaxKind.GreaterThanGreaterThanGreaterThanEqualsToken and >= (int)CSharpSyntaxKind.TildeToken => SyntaxKind.CSharpOperator,
            _ => SyntaxKind.Marker,
        };

        // Use the already-interned string from the C# lexer, rather than realizing the buffer, to ensure that
        // we don't allocate a new string for every operator token.
        content = kind == SyntaxKind.Marker ? Buffer.ToString() : token.ValueText;
        Debug.Assert(content == Buffer.ToString());
        Buffer.Clear();

        _currentCSharpTokenTriviaEnumerator = (token.TrailingTrivia.GetEnumerator(), isLeading: false);
        return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, EndToken(content, kind));
    }

    private StateResult TokenizedExpectedStringOrCharacterLiteral(StringOrCharacterKind expectedStringKind)
    {
        var result = GetNextResult(NextResultType.Token);
        var csharpToken = result.Token;
        (string expectedPrefix, string expectedPostfix, bool lookForPrePostFix) = expectedStringKind switch
        {
            StringOrCharacterKind.Character => ("'", "'", false),
            StringOrCharacterKind.Verbatim_String => ("@\"", "\"", false),
            StringOrCharacterKind.Verbatim_Interpolated_At_First_String => ("@$\"", "\"", false),
            StringOrCharacterKind.Verbatim_Interpolated_Dollar_First_String => ("$@\"", "\"", false),
            StringOrCharacterKind.String_Or_Raw_String when csharpToken.Text is "\"\"" => ("\"", "\"", false),
            StringOrCharacterKind.Interpolated_Or_Raw_Interpolated_String when csharpToken.Text is "$\"\"" => ("$\"", "\"", false),
            StringOrCharacterKind.String_Or_Raw_String or StringOrCharacterKind.Interpolated_Or_Raw_Interpolated_String => ("", "", true),
            _ => throw new InvalidOperationException($"Unexpected expectedStringKind: {expectedStringKind}."),
        };

        for (var finalPosition = Source.Position + csharpToken.Span.Length; Source.Position < finalPosition;)
        {
            if (lookForPrePostFix)
            {
                lookForPrePostFix = handleCurrentCharacterPrefixPostfix();
            }

            TakeCurrent();
        }

        // If the token is the expected kind and is only the expected prefix or doesn't have the expected postfix, then it's unterminated.
        // This is a case like `"test` (which doesn't end in the expected postfix), or `"` (which ends in the expected postfix, but
        // exactly matches the expected prefix).
        // Note: UTF-8 string literals can have a u8 or U8 suffix after the closing quote, e.g., "hello"u8
        if (lookForPrePostFix || csharpToken.Text == expectedPrefix || !IsStringProperlyTerminated(csharpToken.Text, expectedPostfix))
        {
            CurrentErrors.Add(
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(CurrentStart, contentLength: expectedPrefix?.Length ?? 0 /* " */)));
        }

        _currentCSharpTokenTriviaEnumerator = (csharpToken.TrailingTrivia.GetEnumerator(), isLeading: false);
        var razorTokenKind = expectedStringKind == StringOrCharacterKind.Character ? SyntaxKind.CharacterLiteral : SyntaxKind.StringLiteral;
        return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, EndToken(razorTokenKind));

        bool handleCurrentCharacterPrefixPostfix()
        {
            switch (expectedStringKind)
            {
                case StringOrCharacterKind.String_Or_Raw_String:
                    // We can either have a normal string or a raw string. Add to the prefix/postfix until we find a non-" character
                    if (CurrentCharacter != '"')
                    {
                        Debug.Assert(expectedPrefix != null);
                        Debug.Assert(expectedPostfix != null);
                        Debug.Assert(expectedPrefix == expectedPostfix);
                        return false;
                    }

                    expectedPrefix += '"';
                    expectedPostfix += '"';
                    return true;

                case StringOrCharacterKind.Interpolated_Or_Raw_Interpolated_String:
                    // Start with the leading $'s
                    if (expectedPrefix is "" or [.., '$'])
                    {
                        if (CurrentCharacter == '$')
                        {
                            expectedPrefix += '$';
                            return true;
                        }
                        else if (CurrentCharacter == '"')
                        {
                            expectedPrefix += '"';
                            expectedPostfix += '"';
                            return true;
                        }
                        else
                        {
                            // We expect roslyn to have ended parsing, so we should never get here
                            return Assumed.Unreachable<bool>();
                        }
                    }

                    Debug.Assert(expectedPrefix[^1] == '"');
                    if (CurrentCharacter == '"')
                    {
                        expectedPrefix += '"';
                        expectedPostfix += '"';
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case StringOrCharacterKind.Character:
                case StringOrCharacterKind.Verbatim_String:
                case StringOrCharacterKind.Verbatim_Interpolated_At_First_String:
                case StringOrCharacterKind.Verbatim_Interpolated_Dollar_First_String:
                default:
                    return Assumed.Unreachable<bool>();
            }
        }
    }


    private StateResult Trivia()
    {
        Debug.Assert(_currentCSharpTokenTriviaEnumerator is not null);
        var (triviaEnumerator, isLeading) = _currentCSharpTokenTriviaEnumerator.Value;

        if (!triviaEnumerator.MoveNext())
        {
            _currentCSharpTokenTriviaEnumerator = null;
            return Transition(isLeading ? RoslynCSharpTokenizerState.Token : RoslynCSharpTokenizerState.Start, null);
        }

        // Need to make sure the class state is correct, since structs are copied
        _currentCSharpTokenTriviaEnumerator = (triviaEnumerator, isLeading);

        var trivia = triviaEnumerator.Current;
        var triviaString = trivia.ToFullString();

        // We handle razor comments with dedicated nodes
        if (trivia.IsKind(CSharpSyntaxKind.MultiLineCommentTrivia) && triviaString.StartsWith("@*", StringComparison.Ordinal))
        {
            Debug.Assert(CurrentCharacter == '@');
            TakeCurrent();

            return Transition(
                RoslynCSharpTokenizerState.OnRazorCommentStar,
                EndToken(SyntaxKind.RazorCommentTransition));
        }

        // Use FullSpan here because doc comment trivias exclude the leading `///` or `/**` and the trailing `*/`
        AdvancePastSpan(trivia.FullSpan);

        if (EndOfFile
            && trivia.Kind() is CSharpSyntaxKind.MultiLineCommentTrivia or CSharpSyntaxKind.MultiLineDocumentationCommentTrivia
            && !triviaString.EndsWith("*/", StringComparison.Ordinal))
        {
            CurrentErrors.Add(
                RazorDiagnosticFactory.CreateParsing_BlockCommentNotTerminated(
                    new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
        }

        SyntaxKind tokenType;
        switch (trivia.Kind())
        {
            case CSharpSyntaxKind.WhitespaceTrivia:
                tokenType = SyntaxKind.Whitespace;
                break;
            case CSharpSyntaxKind.EndOfLineTrivia:
                tokenType = SyntaxKind.NewLine;
                _isOnlyWhitespaceOnLine = true;
                break;
            case CSharpSyntaxKind.SkippedTokensTrivia:
                // We treat skipped tokens as comments because they're tokens that were skipped over by roslyn,
                // and we want to keep them in the output so that the final C# ends up failing due to their presence.
                // We also don't want them to cause loops over comments and other trivia to break.
                tokenType = SyntaxKind.CSharpComment;

                // SkippedTokenTrivia is used for trailing directives; they can consume the trailing newline, so we need to reset _isOnlyWhitespaceOnLine if the trivia ends with one
                _isOnlyWhitespaceOnLine = triviaString.EndsWith('\n');

                // Look for any misplaced directives in the skipped tokens and error if we find them
                if (triviaString.Contains('#'))
                {
                    CurrentErrors.Add(
                        RazorDiagnosticFactory.CreateParsing_PreprocessorDirectivesMustBeAtTheStartOfLine(
                            // Won't be quite precise, but it's close enough
                            new SourceSpan(CurrentStart, contentLength: trivia.FullSpan.Length)));
                }

                break;
            case CSharpSyntaxKind.SingleLineCommentTrivia or
                 CSharpSyntaxKind.MultiLineCommentTrivia or
                 CSharpSyntaxKind.MultiLineDocumentationCommentTrivia or
                 CSharpSyntaxKind.SingleLineDocumentationCommentTrivia:
                tokenType = SyntaxKind.CSharpComment;
                _isOnlyWhitespaceOnLine = false;
                break;
            case var kind when SyntaxFacts.IsPreprocessorDirective(kind):
                tokenType = SyntaxKind.CSharpDirective;

                if (!_isOnlyWhitespaceOnLine)
                {
                    CurrentErrors.Add(
                        RazorDiagnosticFactory.CreateParsing_PreprocessorDirectivesMustBeAtTheStartOfLine(
                            new SourceSpan(CurrentStart, contentLength: trivia.FullSpan.Length)));
                }

                var directiveTrivia = (DirectiveTriviaSyntax)trivia.GetStructure()!;
                Debug.Assert(directiveTrivia != null);

                if (directiveTrivia is DefineDirectiveTriviaSyntax or UndefDirectiveTriviaSyntax)
                {
                    CurrentErrors.Add(
                        RazorDiagnosticFactory.CreateParsing_DefineAndUndefNotAllowed(
                            new SourceSpan(CurrentStart, contentLength: directiveTrivia.FullSpan.Length)));
                }

                _isOnlyWhitespaceOnLine = directiveTrivia.EndOfDirectiveToken.TrailingTrivia is [.., { RawKind: (int)CSharpSyntaxKind.EndOfLineTrivia }];
                break;
            case CSharpSyntaxKind.DisabledTextTrivia:
                tokenType = SyntaxKind.CSharpDisabledText;
                _isOnlyWhitespaceOnLine = true;

                // We want to scan through the disabled text and see if someone misplaced an #else or #endif by not putting it at the start of a line. We can't truly
                // be certain; for example, it could be in html, intentionally. But this is just a warning, and the user can disable it; since we made a breaking change
                // and there could be directives not at the start of a line, we want to be helpful.

                {
                    for (var i = 0; i < triviaString.Length; i++)
                    {
                        var currentChar = triviaString[i];
                        switch (currentChar)
                        {
                            case '\r':
                            case '\n':
                                _isOnlyWhitespaceOnLine = true;
                                break;

                            case '#' when !_isOnlyWhitespaceOnLine:
                                // If there is only whitespace on the current line, and we're about to see a directive, then it clearly wasn't
                                // #endif or #else
                                var start = CurrentStart.AbsoluteIndex + i;
                                if (startsWith("else"))
                                {
                                    var length = "#else".Length;
                                    var linePosition = Source.SourceText.Lines.GetLinePosition(start);
                                    CurrentErrors.Add(
                                        RazorDiagnosticFactory.CreateParsing_PossibleMisplacedPreprocessorDirective(
                                            new SourceSpan(
                                                absoluteIndex: start,
                                                lineIndex: linePosition.Line,
                                                characterIndex: linePosition.Character,
                                                length)));
                                    i += 4;
                                }
                                else if (startsWith("endif"))
                                {
                                    var length = "#endif".Length;
                                    var linePosition = Source.SourceText.Lines.GetLinePosition(start);
                                    CurrentErrors.Add(
                                        RazorDiagnosticFactory.CreateParsing_PossibleMisplacedPreprocessorDirective(
                                            new SourceSpan(
                                                absoluteIndex: start,
                                                lineIndex: linePosition.Line,
                                                characterIndex: linePosition.Character,
                                                length)));
                                    i += 5;
                                }

                                break;

                                bool startsWith(string substring)
                                {
                                    Debug.Assert(currentChar == '#');
                                    if (i + 1 + substring.Length > triviaString.Length)
                                    {
                                        return false;
                                    }

                                    return triviaString.AsSpan()[(i + 1)..].StartsWith(substring.AsSpan());
                                }

                            default:
                                if (!SyntaxFacts.IsWhitespace(currentChar))
                                {
                                    _isOnlyWhitespaceOnLine = false;
                                }
                                break;
                        }
                    }
                }
                break;
            case CSharpSyntaxKind.ConflictMarkerTrivia:
                tokenType = SyntaxKind.ConflictMarkerTrivia;
                // conflict markers are only parsed by Roslyn if they start the line, and everything on that line is considered part of the marker
                _isOnlyWhitespaceOnLine = true; 
                break;
            case var kind:
                throw new InvalidOperationException($"Unexpected trivia kind: {kind}.");
        };

        return Stay(EndToken(tokenType));
    }

    private StateResult OnRazorCommentStar()
    {
        AssertCurrent('*');
        TakeCurrent();

        return Transition(
            RoslynCSharpTokenizerState.RazorCommentBody,
            EndToken(SyntaxKind.RazorCommentStar));
    }

    // CSharp Spec §2.4.4
    private StateResult NumericLiteral()
    {
        var result = GetNextResult(NextResultType.Token);
        var csharpToken = result.Token;
        AdvancePastToken(csharpToken);

        Buffer.Clear();
        _currentCSharpTokenTriviaEnumerator = (csharpToken.TrailingTrivia.GetEnumerator(), isLeading: false);
        return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, EndToken(csharpToken.Text, SyntaxKind.NumericLiteral));
    }

    private StateResult Identifier()
    {
        var result = GetNextResult(NextResultType.Token);
        var csharpToken = result.Token;
        AdvancePastToken(csharpToken);

        var type = SyntaxKind.Identifier;
        if (!csharpToken.IsKind(CSharpSyntaxKind.IdentifierToken) || result.ContextualKind is not (CSharpSyntaxKind.None or CSharpSyntaxKind.IdentifierToken))
        {
            type = SyntaxKind.Keyword;
        }

        var token = EndToken(csharpToken.Text, type);

        Buffer.Clear();
        _currentCSharpTokenTriviaEnumerator = (csharpToken.TrailingTrivia.GetEnumerator(), isLeading: false);
        return Transition(RoslynCSharpTokenizerState.TriviaForCSharpToken, token);
    }

    private void AdvancePastToken(CSharpSyntaxToken csharpToken)
    {
        // Don't include trailing trivia; we handle that differently than Roslyn
        AdvancePastSpan(csharpToken.Span);
    }

    private void AdvancePastSpan(TextSpan span)
    {
        var finalPosition = Source.Position + span.Length;

        for (; Source.Position < finalPosition;)
        {
            TakeCurrent();
        }
    }

    private StateResult Transition(RoslynCSharpTokenizerState state, SyntaxToken? result)
    {
        return Transition((int)state, result);
    }

    internal override CSharpSyntaxKind? GetTokenKeyword(SyntaxToken token)
    {
        if (token is null)
        {
            return CSharpSyntaxKind.None;
        }

        var content = token.Content;
        return SyntaxFacts.GetKeywordKind(content) is var kind and not CSharpSyntaxKind.None
            ? kind
            : SyntaxFacts.GetContextualKeywordKind(content);
    }

    private SyntaxTokenParser.Result GetNextResult(NextResultType expectedType)
    {
        var nextResult = expectedType switch
        {
            NextResultType.LeadingTrivia => _roslynTokenParser.ParseLeadingTrivia(),
            NextResultType.Token => _roslynTokenParser.ParseNextToken(),
            NextResultType.TrailingTrivia => _roslynTokenParser.ParseTrailingTrivia(),
            _ => Assumed.Unreachable<SyntaxTokenParser.Result>()
        };

        Debug.Assert(_resultCache.All(r => r.position <= nextResult.Token.FullSpan.Start));

        if (_resultCache.Count > 0 && _resultCache[^1].position == nextResult.Token.FullSpan.Start)
        {
            // This can happen when there was no leading or trailing trivia for this token. We don't need to maintain both the previous
            // result and the current result, as the current result fully subsumes it.
            Debug.Assert(_resultCache[^1].result is { Token.FullSpan.Length: 0 });
            Debug.Assert(!nextResult.Token.HasLeadingTrivia);
            _resultCache[^1] = (nextResult.Token.FullSpan.Start, nextResult, _isOnlyWhitespaceOnLine);
        }
        else
        {
            _resultCache.Add((nextResult.Token.FullSpan.Start, nextResult, _isOnlyWhitespaceOnLine));
        }

        if (!nextResult.Token.IsKind(CSharpSyntaxKind.None))
        {
            _isOnlyWhitespaceOnLine = false;
        }

        return nextResult;
    }

    public override void Reset(int position)
    {
        // Most common reset point is the last parsed token, so just try that first.
        Debug.Assert(_resultCache.Count > 0);

        // We always walk backwards from the current position, rather than doing a binary search, because the common pattern in the parser is
        // to put tokens back in the order they were returned. This means that the most common reset point is the last token that was parsed.
        // If this ever changes, we can consider doing a binary search at that point.
        for (var i = _resultCache.Count - 1; i >= 0; i--)
        {
            var (currentPosition, currentResult, isOnlyWhitespaceOnLine) = _resultCache[i];
            if (currentPosition == position)
            {
                // We found an exact match, so we can reset to it directly.
                _roslynTokenParser.ResetTo(currentResult);
                _isOnlyWhitespaceOnLine = isOnlyWhitespaceOnLine;
                _resultCache.RemoveAt(i);
                base.CurrentState = (int)RoslynCSharpTokenizerState.Start;
#if DEBUG
                var oldIsOnlyWhitespaceOnLine = _isOnlyWhitespaceOnLine;
                ResetIsOnlyWhitespaceOnLine();
                Debug.Assert(isOnlyWhitespaceOnLine == _isOnlyWhitespaceOnLine);
#endif
                return;
            }
            else if (currentPosition < position)
            {
                // Reset to the one before reset point, then skip forward
                // We don't want to actually remove the result from the cache in this point: the parser could later ask to reset further back in this same result. This mostly happens for trivia, where the
                // parser may ask to put multiple tokens back, each part of the same roslyn trivia piece. However, it is not _only_ for trivia, so we can't assert that. The parser may decide, for example,
                // to split the @ from a token, reset the tokenizer after the @, and then keep going.
                _roslynTokenParser.ResetTo(currentResult);
                _roslynTokenParser.SkipForwardTo(position);
                base.CurrentState = (int)RoslynCSharpTokenizerState.Start;
                // Can't reuse the isOnlyWhitespaceOnLine value, since we're not before the start of the result anymore.
                ResetIsOnlyWhitespaceOnLine();
                return;
            }
            else
            {
                // We know we're not going to be interested in this reset point anymore, so we can remove it.
                Debug.Assert(currentPosition > position);
                _resultCache.RemoveAt(i);
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _roslynTokenParser.Dispose();
        ListPool<(int, SyntaxTokenParser.Result, bool)>.Default.Return(_resultCache);
    }

    private static bool IsStringProperlyTerminated(string tokenText, string expectedPostfix)
    {
        // Check if the string ends with the expected postfix (e.g., ")
        if (tokenText.EndsWith(expectedPostfix, StringComparison.Ordinal))
        {
            return true;
        }

        // Check if it's a UTF-8 string literal with u8 or U8 suffix
        // UTF-8 strings have the format: prefix + content + postfix + "u8"
        // The minimum valid UTF-8 string is when content is empty.
        // For a regular string: ""u8 is 4 characters total (opening quote + closing quote + u8)
        // For a raw string: """content"""u8 is at least 8 characters (3 opening quotes + content + 3 closing quotes + u8)
        // The expectedPostfix can vary: for "" it's "", for "x" it's ", for """x""" it's """
        // So minimum length is expectedPostfix.Length (closing delimiter) + 2 (u8 suffix)
        if (tokenText.Length >= expectedPostfix.Length + 2)
        {
            var suffix = tokenText.AsSpan(tokenText.Length - 2);
            if (suffix is ['u' or 'U', '8'])
            {
                // Check if the part before the suffix ends with the expected postfix
                var textBeforeSuffix = tokenText.AsSpan(0, tokenText.Length - 2);
                return textBeforeSuffix.EndsWith(expectedPostfix.AsSpan(), StringComparison.Ordinal);
            }
        }

        return false;
    }

    private enum NextResultType
    {
        LeadingTrivia,
        Token,
        TrailingTrivia,
    }

    private enum StringOrCharacterKind
    {
        Character,
        String_Or_Raw_String,
        Interpolated_Or_Raw_Interpolated_String,
        Verbatim_String,
        Verbatim_Interpolated_At_First_String,
        Verbatim_Interpolated_Dollar_First_String,
        Verbatim_Interpolated_String,
    }

    private enum RoslynCSharpTokenizerState
    {
        Start,
        Token,
        TriviaForCSharpToken,

        // Razor Comments - need to be the same for HTML and CSharp
        OnRazorCommentStar,
        AfterRazorCommentTransition = RazorCommentTokenizerState.AfterRazorCommentTransition,
        RazorCommentBody = RazorCommentTokenizerState.RazorCommentBody,
        StarAfterRazorCommentBody = RazorCommentTokenizerState.StarAfterRazorCommentBody,
        AtTokenAfterRazorCommentBody = RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
    }
}
