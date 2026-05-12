// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp;

using SyntaxFactory = Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax.SyntaxFactory;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

// Tokenizer _loosely_ based on http://dev.w3.org/html5/spec/Overview.html#tokenization
internal class HtmlTokenizer : Tokenizer
{
    private static readonly FrozenDictionary<SyntaxKind, SyntaxToken> s_kindToTokenMap = new Dictionary<SyntaxKind, SyntaxToken>()
    {
        [SyntaxKind.OpenAngle] = SyntaxFactory.Token(SyntaxKind.OpenAngle, "<"),
        [SyntaxKind.Bang] = SyntaxFactory.Token(SyntaxKind.Bang, "!"),
        [SyntaxKind.ForwardSlash] = SyntaxFactory.Token(SyntaxKind.ForwardSlash, "/"),
        [SyntaxKind.QuestionMark] = SyntaxFactory.Token(SyntaxKind.QuestionMark, "?"),
        [SyntaxKind.LeftBracket] = SyntaxFactory.Token(SyntaxKind.LeftBracket, "["),
        [SyntaxKind.CloseAngle] = SyntaxFactory.Token(SyntaxKind.CloseAngle, ">"),
        [SyntaxKind.RightBracket] = SyntaxFactory.Token(SyntaxKind.RightBracket, "]"),
        [SyntaxKind.Equals] = SyntaxFactory.Token(SyntaxKind.Equals, "="),
        [SyntaxKind.DoubleQuote] = SyntaxFactory.Token(SyntaxKind.DoubleQuote, "\""),
        [SyntaxKind.SingleQuote] = SyntaxFactory.Token(SyntaxKind.SingleQuote, "'"),
        [SyntaxKind.DoubleHyphen] = SyntaxFactory.Token(SyntaxKind.DoubleHyphen, "--"),
    }.ToFrozenDictionary();

    public HtmlTokenizer(SeekableTextReader source)
        : base(source)
    {
        base.CurrentState = StartState;
    }

    protected override int StartState => (int)HtmlTokenizerState.Data;

    private new HtmlTokenizerState? CurrentState => (HtmlTokenizerState?)base.CurrentState;

    public override SyntaxKind RazorCommentKind => SyntaxKind.RazorCommentLiteral;

    public override SyntaxKind RazorCommentTransitionKind => SyntaxKind.RazorCommentTransition;

    public override SyntaxKind RazorCommentStarKind => SyntaxKind.RazorCommentStar;

    protected override SyntaxToken CreateToken(string content, SyntaxKind type, RazorDiagnostic[] errors)
    {
        if (errors.Length == 0 && s_kindToTokenMap.TryGetValue(type, out var token))
        {
            Debug.Assert(token.Content == content);
            return token;
        }

        return SyntaxFactory.Token(type, content, errors);
    }

    protected override StateResult Dispatch()
    {
        switch (CurrentState)
        {
            case HtmlTokenizerState.Data:
                return Data();
            case HtmlTokenizerState.Text:
                return Text();
            case HtmlTokenizerState.AfterRazorCommentTransition:
                return AfterRazorCommentTransition();
            case HtmlTokenizerState.EscapedRazorCommentTransition:
                return EscapedRazorCommentTransition();
            case HtmlTokenizerState.RazorCommentBody:
                return RazorCommentBody();
            case HtmlTokenizerState.StarAfterRazorCommentBody:
                return StarAfterRazorCommentBody();
            case HtmlTokenizerState.AtTokenAfterRazorCommentBody:
                return AtTokenAfterRazorCommentBody(nextState: StartState);

            default:
                Debug.Fail("Invalid TokenizerState");
                return default;
        }
    }

    // Optimize memory allocation by returning constants for the most frequent cases
    protected override string GetTokenContent(SyntaxKind type)
    {
        var tokenLength = Buffer.Length;

        if (tokenLength == 1)
        {
            switch (type)
            {
                case SyntaxKind.OpenAngle:
                    return "<";
                case SyntaxKind.Bang:
                    return "!";
                case SyntaxKind.ForwardSlash:
                    return "/";
                case SyntaxKind.QuestionMark:
                    return "?";
                case SyntaxKind.LeftBracket:
                    return "[";
                case SyntaxKind.CloseAngle:
                    return ">";
                case SyntaxKind.RightBracket:
                    return "]";
                case SyntaxKind.Equals:
                    return "=";
                case SyntaxKind.DoubleQuote:
                    return "\"";
                case SyntaxKind.SingleQuote:
                    return "'";
                case SyntaxKind.Whitespace:
                    switch (Buffer[0])
                    {
                        case ' ':
                            return " ";
                        case '\t':
                            return "\t";
                    }

                    break;

                case SyntaxKind.NewLine:
                    if (Buffer[0] == '\n')
                    {
                        return "\n";
                    }

                    break;
            }
        }
        else if (tokenLength == 2)
        {
            switch (type)
            {
                case SyntaxKind.NewLine:
                    return "\r\n";
                case SyntaxKind.DoubleHyphen:
                    return "--";
            }
        }

        return base.GetTokenContent(type);
    }

    // http://dev.w3.org/html5/spec/Overview.html#data-state
    private StateResult Data()
    {
        if (SyntaxFacts.IsWhitespace(CurrentCharacter))
        {
            return Stay(Whitespace());
        }
        else if (SyntaxFacts.IsNewLine(CurrentCharacter))
        {
            return Stay(Newline());
        }
        else if (CurrentCharacter == '@')
        {
            TakeCurrent();
            if (CurrentCharacter == '*')
            {
                return Transition(
                    HtmlTokenizerState.AfterRazorCommentTransition,
                    EndToken(SyntaxKind.RazorCommentTransition));
            }
            else if (CurrentCharacter == '@')
            {
                // Could be escaped comment transition
                return Transition(
                    HtmlTokenizerState.EscapedRazorCommentTransition,
                    EndToken(SyntaxKind.Transition));
            }

            return Stay(EndToken(SyntaxKind.Transition));
        }
        else if (AtToken())
        {
            return Stay(Token());
        }
        else
        {
            return Transition(HtmlTokenizerState.Text);
        }
    }

    private StateResult EscapedRazorCommentTransition()
    {
        TakeCurrent();
        return Transition(HtmlTokenizerState.Data, EndToken(SyntaxKind.Transition));
    }

    private StateResult Text()
    {
        var prev = '\0';
        while (!EndOfFile &&
            !(SyntaxFacts.IsWhitespace(CurrentCharacter) || SyntaxFacts.IsNewLine(CurrentCharacter)) &&
            !AtToken())
        {
            prev = CurrentCharacter;
            TakeCurrent();
        }

        if (CurrentCharacter == '@')
        {
            var next = Peek();
            if ((char.IsLetter(prev) || char.IsDigit(prev)) &&
                (char.IsLetter(next) || char.IsDigit(next)))
            {
                TakeCurrent(); // Take the "@"
                return Stay(); // Stay in the Text state
            }
        }

        // Output the Text token and return to the Data state to tokenize the next character (if there is one)
        return Transition(HtmlTokenizerState.Data, EndToken(SyntaxKind.Text));
    }

    private SyntaxToken? Token()
    {
        Debug.Assert(AtToken());

        var sym = CurrentCharacter;
        TakeCurrent();

        switch (sym)
        {
            case '<':
                return EndToken(SyntaxKind.OpenAngle);
            case '!':
                return EndToken(SyntaxKind.Bang);
            case '/':
                return EndToken(SyntaxKind.ForwardSlash);
            case '?':
                return EndToken(SyntaxKind.QuestionMark);
            case '[':
                return EndToken(SyntaxKind.LeftBracket);
            case '>':
                return EndToken(SyntaxKind.CloseAngle);
            case ']':
                return EndToken(SyntaxKind.RightBracket);
            case '=':
                return EndToken(SyntaxKind.Equals);
            case '"':
                return EndToken(SyntaxKind.DoubleQuote);
            case '\'':
                return EndToken(SyntaxKind.SingleQuote);
            case '-':
                Debug.Assert(CurrentCharacter == '-');
                TakeCurrent();
                return EndToken(SyntaxKind.DoubleHyphen);

            default:
                Debug.Fail("Unexpected token!");
                return EndToken(SyntaxKind.Marker);
        }
    }

    private SyntaxToken? Whitespace()
    {
        while (SyntaxFacts.IsWhitespace(CurrentCharacter))
        {
            TakeCurrent();
        }

        return EndToken(SyntaxKind.Whitespace);
    }

    private SyntaxToken? Newline()
    {
        Debug.Assert(SyntaxFacts.IsNewLine(CurrentCharacter));

        // CSharp Spec §2.3.1
        var checkTwoCharNewline = CurrentCharacter == '\r';
        TakeCurrent();

        if (checkTwoCharNewline && CurrentCharacter == '\n')
        {
            TakeCurrent();
        }

        return EndToken(SyntaxKind.NewLine);
    }

    private bool AtToken()
        => CurrentCharacter switch
        {
            '<' or '!' or '/' or '?' or '[' or '>' or ']' or '=' or '"' or '\'' or '@' => true,
            '-' => Peek() == '-',
            _ => false,
        };

    private StateResult Transition(HtmlTokenizerState state)
        => Transition((int)state, result: null);

    private StateResult Transition(HtmlTokenizerState state, SyntaxToken? result)
        => Transition((int)state, result);

    private enum HtmlTokenizerState
    {
        Data,
        Text,
        EscapedRazorCommentTransition,

        // Razor Comments - need to be the same for HTML and CSharp
        AfterRazorCommentTransition = RazorCommentTokenizerState.AfterRazorCommentTransition,
        RazorCommentBody = RazorCommentTokenizerState.RazorCommentBody,
        StarAfterRazorCommentBody = RazorCommentTokenizerState.StarAfterRazorCommentBody,
        AtTokenAfterRazorCommentBody = RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
    }
}
