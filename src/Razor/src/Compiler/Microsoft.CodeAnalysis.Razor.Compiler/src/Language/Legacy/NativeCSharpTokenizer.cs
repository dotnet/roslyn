// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax;
using Microsoft.CodeAnalysis.CSharp;

using SyntaxFactory = Microsoft.AspNetCore.Razor.Language.Syntax.InternalSyntax.SyntaxFactory;
using CSharpSyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

/// <remarks>
/// This is the old tokenizer that was used in Razor. It natively implemented tokenization of C#, rather than using Roslyn. It is maintained for
/// backwards compatibility, controlled by user using a Feature flag in their project file.
/// </remarks>
internal class NativeCSharpTokenizer : CSharpTokenizer
{
    private readonly Dictionary<char, Func<SyntaxKind>> _operatorHandlers;

    private static readonly FrozenDictionary<string, CSharpSyntaxKind> _keywords = (new[] {
            CSharpSyntaxKind.AwaitKeyword,
            CSharpSyntaxKind.AbstractKeyword,
            CSharpSyntaxKind.ByteKeyword,
            CSharpSyntaxKind.ClassKeyword,
            CSharpSyntaxKind.DelegateKeyword,
            CSharpSyntaxKind.EventKeyword,
            CSharpSyntaxKind.FixedKeyword,
            CSharpSyntaxKind.IfKeyword,
            CSharpSyntaxKind.InternalKeyword,
            CSharpSyntaxKind.NewKeyword,
            CSharpSyntaxKind.OverrideKeyword,
            CSharpSyntaxKind.ReadOnlyKeyword,
            CSharpSyntaxKind.ShortKeyword,
            CSharpSyntaxKind.StructKeyword,
            CSharpSyntaxKind.TryKeyword,
            CSharpSyntaxKind.UnsafeKeyword,
            CSharpSyntaxKind.VolatileKeyword,
            CSharpSyntaxKind.AsKeyword,
            CSharpSyntaxKind.DoKeyword,
            CSharpSyntaxKind.IsKeyword,
            CSharpSyntaxKind.ParamsKeyword,
            CSharpSyntaxKind.RefKeyword,
            CSharpSyntaxKind.SwitchKeyword,
            CSharpSyntaxKind.UShortKeyword,
            CSharpSyntaxKind.WhileKeyword,
            CSharpSyntaxKind.CaseKeyword,
            CSharpSyntaxKind.ConstKeyword,
            CSharpSyntaxKind.ExplicitKeyword,
            CSharpSyntaxKind.FloatKeyword,
            CSharpSyntaxKind.NullKeyword,
            CSharpSyntaxKind.SizeOfKeyword,
            CSharpSyntaxKind.TypeOfKeyword,
            CSharpSyntaxKind.ImplicitKeyword,
            CSharpSyntaxKind.PrivateKeyword,
            CSharpSyntaxKind.ThisKeyword,
            CSharpSyntaxKind.UsingKeyword,
            CSharpSyntaxKind.ExternKeyword,
            CSharpSyntaxKind.ReturnKeyword,
            CSharpSyntaxKind.StackAllocKeyword,
            CSharpSyntaxKind.UIntKeyword,
            CSharpSyntaxKind.BaseKeyword,
            CSharpSyntaxKind.CatchKeyword,
            CSharpSyntaxKind.ContinueKeyword,
            CSharpSyntaxKind.DoubleKeyword,
            CSharpSyntaxKind.ForKeyword,
            CSharpSyntaxKind.InKeyword,
            CSharpSyntaxKind.LockKeyword,
            CSharpSyntaxKind.ObjectKeyword,
            CSharpSyntaxKind.ProtectedKeyword,
            CSharpSyntaxKind.StaticKeyword,
            CSharpSyntaxKind.FalseKeyword,
            CSharpSyntaxKind.PublicKeyword,
            CSharpSyntaxKind.SByteKeyword,
            CSharpSyntaxKind.ThrowKeyword,
            CSharpSyntaxKind.VirtualKeyword,
            CSharpSyntaxKind.DecimalKeyword,
            CSharpSyntaxKind.ElseKeyword,
            CSharpSyntaxKind.OperatorKeyword,
            CSharpSyntaxKind.StringKeyword,
            CSharpSyntaxKind.ULongKeyword,
            CSharpSyntaxKind.BoolKeyword,
            CSharpSyntaxKind.CharKeyword,
            CSharpSyntaxKind.DefaultKeyword,
            CSharpSyntaxKind.ForEachKeyword,
            CSharpSyntaxKind.LongKeyword,
            CSharpSyntaxKind.VoidKeyword,
            CSharpSyntaxKind.EnumKeyword,
            CSharpSyntaxKind.FinallyKeyword,
            CSharpSyntaxKind.IntKeyword,
            CSharpSyntaxKind.OutKeyword,
            CSharpSyntaxKind.SealedKeyword,
            CSharpSyntaxKind.TrueKeyword,
            CSharpSyntaxKind.GotoKeyword,
            CSharpSyntaxKind.UncheckedKeyword,
            CSharpSyntaxKind.InterfaceKeyword,
            CSharpSyntaxKind.BreakKeyword,
            CSharpSyntaxKind.CheckedKeyword,
            CSharpSyntaxKind.NamespaceKeyword,
            CSharpSyntaxKind.WhenKeyword,
            CSharpSyntaxKind.WhereKeyword }).ToFrozenDictionary(keySelector: k => SyntaxFacts.GetText(k));

    public NativeCSharpTokenizer(SeekableTextReader source)
        : base(source)
    {
        base.CurrentState = StartState;

        _operatorHandlers = new Dictionary<char, Func<SyntaxKind>>()
            {
                { '-', MinusOperator },
                { '<', LessThanOperator },
                { '>', GreaterThanOperator },
                { '&', CreateTwoCharOperatorHandler(SyntaxKind.And, '=', SyntaxKind.AndAssign, '&', SyntaxKind.DoubleAnd) },
                { '|', CreateTwoCharOperatorHandler(SyntaxKind.Or, '=', SyntaxKind.OrAssign, '|', SyntaxKind.DoubleOr) },
                { '+', CreateTwoCharOperatorHandler(SyntaxKind.Plus, '=', SyntaxKind.PlusAssign, '+', SyntaxKind.Increment) },
                { '=', CreateTwoCharOperatorHandler(SyntaxKind.Assign, '=', SyntaxKind.Equals, '>', SyntaxKind.GreaterThanEqual) },
                { '!', CreateTwoCharOperatorHandler(SyntaxKind.Not, '=', SyntaxKind.NotEqual) },
                { '%', CreateTwoCharOperatorHandler(SyntaxKind.Modulo, '=', SyntaxKind.ModuloAssign) },
                { '*', CreateTwoCharOperatorHandler(SyntaxKind.Star, '=', SyntaxKind.MultiplyAssign) },
                { ':', CreateTwoCharOperatorHandler(SyntaxKind.Colon, ':', SyntaxKind.DoubleColon) },
                { '?', CreateTwoCharOperatorHandler(SyntaxKind.QuestionMark, '?', SyntaxKind.NullCoalesce) },
                { '^', CreateTwoCharOperatorHandler(SyntaxKind.Xor, '=', SyntaxKind.XorAssign) },
                { '(', () => SyntaxKind.LeftParenthesis },
                { ')', () => SyntaxKind.RightParenthesis },
                { '{', () => SyntaxKind.LeftBrace },
                { '}', () => SyntaxKind.RightBrace },
                { '[', () => SyntaxKind.LeftBracket },
                { ']', () => SyntaxKind.RightBracket },
                { ',', () => SyntaxKind.Comma },
                { ';', () => SyntaxKind.Semicolon },
                { '~', () => SyntaxKind.Tilde },
                { '#', () => SyntaxKind.Hash }
            };
    }

    protected override int StartState => (int)CSharpTokenizerState.Data;

    private new CSharpTokenizerState? CurrentState => (CSharpTokenizerState?)base.CurrentState;

    public override SyntaxKind RazorCommentKind => SyntaxKind.RazorCommentLiteral;

    public override SyntaxKind RazorCommentTransitionKind => SyntaxKind.RazorCommentTransition;

    public override SyntaxKind RazorCommentStarKind => SyntaxKind.RazorCommentStar;

    protected override StateResult Dispatch()
    {
        switch (CurrentState)
        {
            case CSharpTokenizerState.Data:
                return Data();
            case CSharpTokenizerState.BlockComment:
                return BlockComment();
            case CSharpTokenizerState.QuotedCharacterLiteral:
                return QuotedCharacterLiteral();
            case CSharpTokenizerState.QuotedStringLiteral:
                return QuotedStringLiteral();
            case CSharpTokenizerState.VerbatimStringLiteral:
                return VerbatimStringLiteral();
            case CSharpTokenizerState.AfterRazorCommentTransition:
                return AfterRazorCommentTransition();
            case CSharpTokenizerState.EscapedRazorCommentTransition:
                return EscapedRazorCommentTransition();
            case CSharpTokenizerState.RazorCommentBody:
                return RazorCommentBody();
            case CSharpTokenizerState.StarAfterRazorCommentBody:
                return StarAfterRazorCommentBody();
            case CSharpTokenizerState.AtTokenAfterRazorCommentBody:
                return AtTokenAfterRazorCommentBody(nextState: StartState);
            default:
                Debug.Fail("Invalid TokenizerState");
                return default(StateResult);
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
                case SyntaxKind.IntegerLiteral:
                    switch (Buffer[0])
                    {
                        case '0':
                            return "0";
                        case '1':
                            return "1";
                        case '2':
                            return "2";
                        case '3':
                            return "3";
                        case '4':
                            return "4";
                        case '5':
                            return "5";
                        case '6':
                            return "6";
                        case '7':
                            return "7";
                        case '8':
                            return "8";
                        case '9':
                            return "9";
                    }
                    break;
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
                case SyntaxKind.Minus:
                    return "-";
                case SyntaxKind.Not:
                    return "!";
                case SyntaxKind.Modulo:
                    return "%";
                case SyntaxKind.And:
                    return "&";
                case SyntaxKind.LeftParenthesis:
                    return "(";
                case SyntaxKind.RightParenthesis:
                    return ")";
                case SyntaxKind.Star:
                    return "*";
                case SyntaxKind.Comma:
                    return ",";
                case SyntaxKind.Dot:
                    return ".";
                case SyntaxKind.Slash:
                    return "/";
                case SyntaxKind.Colon:
                    return ":";
                case SyntaxKind.Semicolon:
                    return ";";
                case SyntaxKind.QuestionMark:
                    return "?";
                case SyntaxKind.RightBracket:
                    return "]";
                case SyntaxKind.LeftBracket:
                    return "[";
                case SyntaxKind.Xor:
                    return "^";
                case SyntaxKind.LeftBrace:
                    return "{";
                case SyntaxKind.Or:
                    return "|";
                case SyntaxKind.RightBrace:
                    return "}";
                case SyntaxKind.Tilde:
                    return "~";
                case SyntaxKind.Plus:
                    return "+";
                case SyntaxKind.LessThan:
                    return "<";
                case SyntaxKind.Assign:
                    return "=";
                case SyntaxKind.GreaterThan:
                    return ">";
                case SyntaxKind.Hash:
                    return "#";
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
                case SyntaxKind.Arrow:
                    return "->";
                case SyntaxKind.Decrement:
                    return "--";
                case SyntaxKind.MinusAssign:
                    return "-=";
                case SyntaxKind.NotEqual:
                    return "!=";
                case SyntaxKind.ModuloAssign:
                    return "%=";
                case SyntaxKind.AndAssign:
                    return "&=";
                case SyntaxKind.DoubleAnd:
                    return "&&";
                case SyntaxKind.MultiplyAssign:
                    return "*=";
                case SyntaxKind.DivideAssign:
                    return "/=";
                case SyntaxKind.DoubleColon:
                    return "::";
                case SyntaxKind.NullCoalesce:
                    return "??";
                case SyntaxKind.XorAssign:
                    return "^=";
                case SyntaxKind.OrAssign:
                    return "|=";
                case SyntaxKind.DoubleOr:
                    return "||";
                case SyntaxKind.PlusAssign:
                    return "+=";
                case SyntaxKind.Increment:
                    return "++";
                case SyntaxKind.LessThanEqual:
                    return "<=";
                case SyntaxKind.LeftShift:
                    return "<<";
                case SyntaxKind.Equals:
                    return "==";
                case SyntaxKind.GreaterThanEqual:
                    if (Buffer[0] == '=')
                    {
                        return "=>";
                    }
                    return ">=";
                case SyntaxKind.RightShift:
                    return ">>";


            }
        }
        else if (tokenLength == 3)
        {
            switch (type)
            {
                case SyntaxKind.LeftShiftAssign:
                    return "<<=";
                case SyntaxKind.RightShiftAssign:
                    return ">>=";
            }
        }

        return base.GetTokenContent(type);
    }

    protected override SyntaxToken CreateToken(string content, SyntaxKind kind, RazorDiagnostic[] errors)
    {
        return SyntaxFactory.Token(kind, content, errors);
    }

    private StateResult Data()
    {
        if (SyntaxFacts.IsNewLine(CurrentCharacter))
        {
            // CSharp Spec §2.3.1
            var checkTwoCharNewline = CurrentCharacter == '\r';
            TakeCurrent();
            if (checkTwoCharNewline && CurrentCharacter == '\n')
            {
                TakeCurrent();
            }
            return Stay(EndToken(SyntaxKind.NewLine));
        }
        else if (SyntaxFacts.IsWhitespace(CurrentCharacter))
        {
            // CSharp Spec §2.3.3
            TakeUntil(c => !SyntaxFacts.IsWhitespace(c));
            return Stay(EndToken(SyntaxKind.Whitespace));
        }
        else if (SyntaxFacts.IsIdentifierStartCharacter(CurrentCharacter))
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
                TakeCurrent();
                return Transition(CSharpTokenizerState.QuotedCharacterLiteral);
            case '"':
                TakeCurrent();
                return Transition(CSharpTokenizerState.QuotedStringLiteral);
            case '.':
                if (char.IsDigit(Peek()))
                {
                    return RealLiteral();
                }
                return Stay(Single(SyntaxKind.Dot));
            case '/':
                TakeCurrent();
                if (CurrentCharacter == '/')
                {
                    TakeCurrent();
                    return SingleLineComment();
                }
                else if (CurrentCharacter == '*')
                {
                    TakeCurrent();
                    return Transition(CSharpTokenizerState.BlockComment);
                }
                else if (CurrentCharacter == '=')
                {
                    TakeCurrent();
                    return Stay(EndToken(SyntaxKind.DivideAssign));
                }
                else
                {
                    return Stay(EndToken(SyntaxKind.Slash));
                }
            default:
                return Stay(EndToken(Operator()));
        }
    }

    private StateResult AtToken()
    {
        TakeCurrent();
        if (CurrentCharacter == '"')
        {
            TakeCurrent();
            return Transition(CSharpTokenizerState.VerbatimStringLiteral);
        }
        else if (CurrentCharacter == '*')
        {
            return Transition(
                CSharpTokenizerState.AfterRazorCommentTransition,
                EndToken(SyntaxKind.RazorCommentTransition));
        }
        else if (CurrentCharacter == '@')
        {
            // Could be escaped comment transition
            return Transition(
                CSharpTokenizerState.EscapedRazorCommentTransition,
                EndToken(SyntaxKind.Transition));
        }

        return Stay(EndToken(SyntaxKind.Transition));
    }

    private StateResult EscapedRazorCommentTransition()
    {
        TakeCurrent();
        return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.Transition));
    }

    private SyntaxKind Operator()
    {
        var first = CurrentCharacter;
        TakeCurrent();
        Func<SyntaxKind> handler;
        if (_operatorHandlers.TryGetValue(first, out handler))
        {
            return handler();
        }
        return SyntaxKind.Marker;
    }

    private SyntaxKind LessThanOperator()
    {
        if (CurrentCharacter == '=')
        {
            TakeCurrent();
            return SyntaxKind.LessThanEqual;
        }
        return SyntaxKind.LessThan;
    }

    private SyntaxKind GreaterThanOperator()
    {
        if (CurrentCharacter == '=')
        {
            TakeCurrent();
            return SyntaxKind.GreaterThanEqual;
        }
        return SyntaxKind.GreaterThan;
    }

    private SyntaxKind MinusOperator()
    {
        if (CurrentCharacter == '>')
        {
            TakeCurrent();
            return SyntaxKind.Arrow;
        }
        else if (CurrentCharacter == '-')
        {
            TakeCurrent();
            return SyntaxKind.Decrement;
        }
        else if (CurrentCharacter == '=')
        {
            TakeCurrent();
            return SyntaxKind.MinusAssign;
        }
        return SyntaxKind.Minus;
    }

    private Func<SyntaxKind> CreateTwoCharOperatorHandler(SyntaxKind typeIfOnlyFirst, char second, SyntaxKind typeIfBoth)
    {
        return () =>
        {
            if (CurrentCharacter == second)
            {
                TakeCurrent();
                return typeIfBoth;
            }
            return typeIfOnlyFirst;
        };
    }

    private Func<SyntaxKind> CreateTwoCharOperatorHandler(SyntaxKind typeIfOnlyFirst, char option1, SyntaxKind typeIfOption1, char option2, SyntaxKind typeIfOption2)
    {
        return () =>
        {
            if (CurrentCharacter == option1)
            {
                TakeCurrent();
                return typeIfOption1;
            }
            else if (CurrentCharacter == option2)
            {
                TakeCurrent();
                return typeIfOption2;
            }
            return typeIfOnlyFirst;
        };
    }

    private StateResult VerbatimStringLiteral()
    {
        TakeUntil(c => c == '"');
        if (CurrentCharacter == '"')
        {
            TakeCurrent();
            if (CurrentCharacter == '"')
            {
                TakeCurrent();
                // Stay in the literal, this is an escaped "
                return Stay();
            }
        }
        else if (EndOfFile)
        {
            CurrentErrors.Add(
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));
        }
        return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.StringLiteral));
    }

    private StateResult QuotedCharacterLiteral() => QuotedLiteral('\'', IsEndQuotedCharacterLiteral, SyntaxKind.CharacterLiteral);

    private StateResult QuotedStringLiteral() => QuotedLiteral('\"', IsEndQuotedStringLiteral, SyntaxKind.StringLiteral);

    private static readonly Func<char, bool> IsEndQuotedCharacterLiteral = static (c) => c == '\\' || c == '\'' || SyntaxFacts.IsNewLine(c);
    private static readonly Func<char, bool> IsEndQuotedStringLiteral = static (c) => c == '\\' || c == '\"' || SyntaxFacts.IsNewLine(c);

    private StateResult QuotedLiteral(char quote, Func<char, bool> isEndQuotedLiteral, SyntaxKind literalType)
    {
        TakeUntil(isEndQuotedLiteral);
        if (CurrentCharacter == '\\')
        {
            TakeCurrent(); // Take the '\'

            // If the next char is the same quote that started this
            if (CurrentCharacter == quote || CurrentCharacter == '\\')
            {
                TakeCurrent(); // Take it so that we don't prematurely end the literal.
            }
            return Stay();
        }
        else if (EndOfFile || SyntaxFacts.IsNewLine(CurrentCharacter))
        {
            CurrentErrors.Add(
                RazorDiagnosticFactory.CreateParsing_UnterminatedStringLiteral(
                    new SourceSpan(CurrentStart, contentLength: 1 /* " */)));
        }
        else
        {
            TakeCurrent(); // No-op if at EOF
        }
        return Transition(CSharpTokenizerState.Data, EndToken(literalType));
    }

    // CSharp Spec §2.3.2
    private StateResult BlockComment()
    {
        TakeUntil(c => c == '*');
        if (EndOfFile)
        {
            CurrentErrors.Add(
                RazorDiagnosticFactory.CreateParsing_BlockCommentNotTerminated(
                    new SourceSpan(CurrentStart, contentLength: 1 /* end of file */)));

            return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.CSharpComment));
        }
        if (CurrentCharacter == '*')
        {
            TakeCurrent();
            if (CurrentCharacter == '/')
            {
                TakeCurrent();
                return Transition(CSharpTokenizerState.Data, EndToken(SyntaxKind.CSharpComment));
            }
        }
        return Stay();
    }

    // CSharp Spec §2.3.2
    private StateResult SingleLineComment()
    {
        TakeUntil(c => SyntaxFacts.IsNewLine(c));
        return Stay(EndToken(SyntaxKind.CSharpComment));
    }

    // CSharp Spec §2.4.4
    private StateResult NumericLiteral()
    {
        if (TakeAll("0x", caseSensitive: true))
        {
            return HexLiteral();
        }
        else
        {
            return DecimalLiteral();
        }
    }

    private StateResult HexLiteral()
    {
        TakeUntil(c => !IsHexDigit(c));
        TakeIntegerSuffix();
        return Stay(EndToken(SyntaxKind.IntegerLiteral));
    }

    private StateResult DecimalLiteral()
    {
        TakeUntil(c => !Char.IsDigit(c));
        if (CurrentCharacter == '.' && Char.IsDigit(Peek()))
        {
            return RealLiteral();
        }
        else if (IsRealLiteralSuffix(CurrentCharacter) ||
                 CurrentCharacter == 'E' || CurrentCharacter == 'e')
        {
            return RealLiteralExponentPart();
        }
        else
        {
            TakeIntegerSuffix();
            return Stay(EndToken(SyntaxKind.IntegerLiteral));
        }
    }

    private StateResult RealLiteralExponentPart()
    {
        if (CurrentCharacter == 'E' || CurrentCharacter == 'e')
        {
            TakeCurrent();
            if (CurrentCharacter == '+' || CurrentCharacter == '-')
            {
                TakeCurrent();
            }
            TakeUntil(c => !Char.IsDigit(c));
        }
        if (IsRealLiteralSuffix(CurrentCharacter))
        {
            TakeCurrent();
        }
        return Stay(EndToken(SyntaxKind.RealLiteral));
    }

    // CSharp Spec §2.4.4.3
    private StateResult RealLiteral()
    {
        AssertCurrent('.');
        TakeCurrent();
        Debug.Assert(Char.IsDigit(CurrentCharacter));
        TakeUntil(c => !Char.IsDigit(c));
        return RealLiteralExponentPart();
    }

    private void TakeIntegerSuffix()
    {
        if (Char.ToLowerInvariant(CurrentCharacter) == 'u')
        {
            TakeCurrent();
            if (Char.ToLowerInvariant(CurrentCharacter) == 'l')
            {
                TakeCurrent();
            }
        }
        else if (Char.ToLowerInvariant(CurrentCharacter) == 'l')
        {
            TakeCurrent();
            if (Char.ToLowerInvariant(CurrentCharacter) == 'u')
            {
                TakeCurrent();
            }
        }
    }

    // CSharp Spec §2.4.2
    private StateResult Identifier()
    {
        Debug.Assert(SyntaxFacts.IsIdentifierStartCharacter(CurrentCharacter));
        TakeCurrent();
        TakeUntil(c => !SyntaxFacts.IsIdentifierPartCharacter(c));
        SyntaxToken token = null;
        if (HaveContent)
        {
            var type = SyntaxKind.Identifier;
            var tokenContent = Buffer.ToString();
            if (_keywords.TryGetValue(tokenContent, value: out _))
            {
                type = SyntaxKind.Keyword;
            }

            token = SyntaxFactory.Token(type, tokenContent);

            Buffer.Clear();
            CurrentErrors.Clear();
        }

        return Stay(token);
    }

    private StateResult Transition(CSharpTokenizerState state)
    {
        return Transition((int)state, result: null);
    }

    private StateResult Transition(CSharpTokenizerState state, SyntaxToken result)
    {
        return Transition((int)state, result);
    }

    private static bool IsRealLiteralSuffix(char character)
    {
        return character == 'F' ||
               character == 'f' ||
               character == 'D' ||
               character == 'd' ||
               character == 'M' ||
               character == 'm';
    }

    private static bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
    }

    internal override CSharpSyntaxKind? GetTokenKeyword(SyntaxToken token)
    {
        if (token != null && _keywords.TryGetValue(token.Content, out var keyword))
        {
            return keyword;
        }

        return null;
    }

    private enum CSharpTokenizerState
    {
        Data,
        BlockComment,
        QuotedCharacterLiteral,
        QuotedStringLiteral,
        VerbatimStringLiteral,
        EscapedRazorCommentTransition,

        // Razor Comments - need to be the same for HTML and CSharp
        AfterRazorCommentTransition = RazorCommentTokenizerState.AfterRazorCommentTransition,
        RazorCommentBody = RazorCommentTokenizerState.RazorCommentBody,
        StarAfterRazorCommentBody = RazorCommentTokenizerState.StarAfterRazorCommentBody,
        AtTokenAfterRazorCommentBody = RazorCommentTokenizerState.AtTokenAfterRazorCommentBody,
    }
}
