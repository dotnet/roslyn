// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;

using static RegexHelpers;

using RegexToken = EmbeddedSyntaxToken<RegexKind>;
using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

/// <summary>
/// Produces tokens from the sequence of <see cref="VirtualChar"/> characters.  Unlike the
/// native C# and VB lexer, this lexer is much more tightly controlled by the parser.  For
/// example, while C# can have trivia on virtual every token, the same is not true for
/// RegexTokens.  As such, instead of automatically lexing out tokens to make them available for
/// the parser, the parser asks for each token as necessary passing the right information to
/// indicate which types and shapes of tokens are allowed.
///
/// The tight coupling means that the parser is allowed direct control of the position of the
/// lexer.
///
/// Note: most of the time, tokens returned are just a single character long, including for long
/// sequences of text characters (like ```"goo"```).  This is just three <see
/// cref="RegexTextNode"/>s in a row (each containing a <see cref="RegexKind.TextToken"/> a
/// single character long).
///
/// There are multi-character tokens though.  For example ```10``` in ```a{10,}``` or ```name```
/// in ```\k'name'```
/// </summary>
internal struct RegexLexer
{
    public readonly VirtualCharSequence Text;
    public int Position;

    public RegexLexer(VirtualCharSequence text) : this()
        => Text = text;

    public readonly VirtualChar CurrentChar => Position < Text.Length ? Text[Position] : default;

    public readonly VirtualCharSequence GetSubPatternToCurrentPos(int start)
        => GetSubPattern(start, Position);

    public readonly VirtualCharSequence GetSubPattern(int start, int end)
        => Text[start..end];

    public RegexToken ScanNextToken(bool allowTrivia, RegexOptions options)
    {
        var trivia = ScanLeadingTrivia(allowTrivia, options);
        if (Position == Text.Length)
        {
            return CreateToken(RegexKind.EndOfFile, trivia, VirtualCharSequence.Empty);
        }

        var ch = this.CurrentChar;
        Position++;

        return CreateToken(GetKind(ch), trivia, Text[(Position - 1)..Position]);
    }

    private static RegexKind GetKind(VirtualChar ch)
        => ch.Value switch
        {
            '|' => RegexKind.BarToken,
            '*' => RegexKind.AsteriskToken,
            '+' => RegexKind.PlusToken,
            '?' => RegexKind.QuestionToken,
            '{' => RegexKind.OpenBraceToken,
            '}' => RegexKind.CloseBraceToken,
            '\\' => RegexKind.BackslashToken,
            '[' => RegexKind.OpenBracketToken,
            ']' => RegexKind.CloseBracketToken,
            '.' => RegexKind.DotToken,
            '^' => RegexKind.CaretToken,
            '$' => RegexKind.DollarToken,
            '(' => RegexKind.OpenParenToken,
            ')' => RegexKind.CloseParenToken,
            ',' => RegexKind.CommaToken,
            ':' => RegexKind.ColonToken,
            '=' => RegexKind.EqualsToken,
            '!' => RegexKind.ExclamationToken,
            '<' => RegexKind.LessThanToken,
            '>' => RegexKind.GreaterThanToken,
            '-' => RegexKind.MinusToken,
            '\'' => RegexKind.SingleQuoteToken,
            _ => RegexKind.TextToken,
        };

    private ImmutableArray<RegexTrivia> ScanLeadingTrivia(bool allowTrivia, RegexOptions options)
    {
        if (!allowTrivia)
        {
            return [];
        }

        using var _ = ArrayBuilder<RegexTrivia>.GetInstance(out var result);

        while (Position < Text.Length)
        {
            var comment = ScanComment(options);
            if (comment != null)
            {
                result.Add(comment.Value);
                continue;
            }

            var whitespace = ScanWhitespace(options);
            if (whitespace != null)
            {
                result.Add(whitespace.Value);
                continue;
            }

            break;
        }

        return result.ToImmutableAndClear();
    }

    public RegexTrivia? ScanComment(RegexOptions options)
    {
        if (Position >= Text.Length)
        {
            return null;
        }

        if (HasOption(options, RegexOptions.IgnorePatternWhitespace))
        {
            if (Text[Position] == '#')
            {
                var start = Position;

                // Note: \n is the only newline the native regex parser looks for.
                while (Position < Text.Length &&
                        Text[Position] != '\n')
                {
                    Position++;
                }

                return CreateTrivia(RegexKind.CommentTrivia, GetSubPatternToCurrentPos(start));
            }
        }

        if (IsAt("(?#"))
        {
            var start = Position;
            while (Position < Text.Length &&
                   Text[Position] != ')')
            {
                Position++;
            }

            if (Position == Text.Length)
            {
                var diagnostics = ImmutableArray.Create(new EmbeddedDiagnostic(
                    FeaturesResources.Unterminated_regex_comment,
                    GetTextSpan(start, Position)));
                return CreateTrivia(RegexKind.CommentTrivia, GetSubPatternToCurrentPos(start), diagnostics);
            }

            Position++;
            return CreateTrivia(RegexKind.CommentTrivia, GetSubPatternToCurrentPos(start));
        }

        return null;
    }

    public readonly TextSpan GetTextSpan(int startInclusive, int endExclusive)
        => TextSpan.FromBounds(Text[startInclusive].Span.Start, Text[endExclusive - 1].Span.End);

    public readonly bool IsAt(string val)
        => TextAt(this.Position, val);

    private readonly bool TextAt(int position, string val)
    {
        for (var i = 0; i < val.Length; i++)
        {
            if (position + i >= Text.Length ||
                Text[position + i] != val[i])
            {
                return false;
            }
        }

        return true;
    }

    private RegexTrivia? ScanWhitespace(RegexOptions options)
    {
        if (HasOption(options, RegexOptions.IgnorePatternWhitespace))
        {
            var start = Position;
            while (Position < Text.Length && IsBlank(Text[Position]))
            {
                Position++;
            }

            if (Position > start)
            {
                return CreateTrivia(RegexKind.WhitespaceTrivia, GetSubPatternToCurrentPos(start));
            }
        }

        return null;
    }

    private static bool IsBlank(VirtualChar ch)
    {
        // List taken from the native regex parser.
        switch (ch.Value)
        {
            case '\u0009':
            case '\u000A':
            case '\u000C':
            case '\u000D':
            case ' ':
                return true;
            default:
                return false;
        }
    }

    public RegexToken? TryScanEscapeCategory()
    {
        var start = Position;
        while (Position < Text.Length && IsEscapeCategoryChar(this.CurrentChar))
            Position++;

        if (Position == start)
            return null;

        var token = CreateToken(RegexKind.EscapeCategoryToken, [], GetSubPatternToCurrentPos(start));
        var category = token.VirtualChars.CreateString();

        if (!RegexCharClass.IsEscapeCategory(category))
        {
            token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                string.Format(FeaturesResources.Unknown_property_0, category),
                token.GetSpan()));
        }

        return token;
    }

    public static bool IsEscapeCategoryChar(VirtualChar ch)
        => ch.Value is '-' or '_' or
           (>= 'a' and <= 'z') or
           (>= 'A' and <= 'Z') or
           (>= '0' and <= '9');

    public RegexToken? TryScanNumber()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        const int MaxValueDiv10 = int.MaxValue / 10;
        const int MaxValueMod10 = int.MaxValue % 10;

        var value = 0;
        var start = Position;
        var error = false;
        while (Position < Text.Length && this.CurrentChar is var ch && IsDecimalDigit(ch))
        {
            Position++;

            unchecked
            {
                var charVal = ch - '0';
                if (value > MaxValueDiv10 || (value == MaxValueDiv10 && charVal > MaxValueMod10))
                {
                    error = true;
                }

                value *= 10;
                value += charVal;
            }
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RegexKind.NumberToken, [], GetSubPatternToCurrentPos(start));
        token = token.With(value: value);

        if (error)
        {
            token = token.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                FeaturesResources.Quantifier_and_capture_group_numbers_must_be_less_than_or_equal_to_Int32_MaxValue,
                token.GetSpan()));
        }

        return token;
    }

    public RegexToken? TryScanCaptureName()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        while (Position < Text.Length && RegexCharClass.IsBoundaryWordChar(this.CurrentChar))
        {
            Position++;
        }

        if (Position == start)
        {
            return null;
        }

        var token = CreateToken(RegexKind.CaptureNameToken, [], GetSubPatternToCurrentPos(start));
        token = token.With(value: token.VirtualChars.CreateString());
        return token;
    }

    public RegexToken? TryScanNumberOrCaptureName()
        => TryScanNumber() ?? TryScanCaptureName();

    public RegexToken? TryScanOptions()
    {
        var start = Position;
        while (Position < Text.Length && IsOptionChar(this.CurrentChar))
        {
            Position++;
        }

        return start == Position
            ? null
            : CreateToken(RegexKind.OptionsToken, ImmutableArray<RegexTrivia>.Empty, GetSubPatternToCurrentPos(start));
    }

    private static bool IsOptionChar(VirtualChar ch)
    {
        switch (ch.Value)
        {
            case '+':
            case '-':
            case 'i':
            case 'I':
            case 'm':
            case 'M':
            case 'n':
            case 'N':
            case 's':
            case 'S':
            case 'x':
            case 'X':
                return true;
            default:
                return false;
        }
    }

    public RegexToken ScanHexCharacters(int count)
    {
        var start = Position;
        var beforeSlash = start - 2;

        // Make sure we're right after the \x or \u.
        Debug.Assert(Text[beforeSlash] == '\\');
        Debug.Assert(Text[beforeSlash + 1].Value is 'x' or 'u');

        for (var i = 0; i < count; i++)
        {
            if (Position < Text.Length && IsHexChar(this.CurrentChar))
            {
                Position++;
            }
        }

        var result = CreateToken(
            RegexKind.TextToken, [], GetSubPatternToCurrentPos(start));

        var length = Position - start;
        if (length != count)
        {
            result = result.AddDiagnosticIfNone(new EmbeddedDiagnostic(
                FeaturesResources.Insufficient_or_invalid_hexadecimal_digits,
                GetTextSpan(beforeSlash, Position)));
        }

        return result;
    }

    public static bool IsHexChar(VirtualChar ch)
        => IsDecimalDigit(ch) ||
           (ch >= 'a' && ch <= 'f') ||
           (ch >= 'A' && ch <= 'F');

    private static bool IsDecimalDigit(VirtualChar ch)
        => ch.Value is >= '0' and <= '9';

    private static bool IsOctalDigit(VirtualChar ch)
        => ch.Value is >= '0' and <= '7';

    public RegexToken ScanOctalCharacters(RegexOptions options)
    {
        var start = Position;
        var beforeSlash = start - 1;

        // Make sure we're right after the \
        // And we only should have been called if we were \octal-char 
        Debug.Assert(Text[beforeSlash] == '\\');
        Debug.Assert(IsOctalDigit(Text[start]));

        const int maxChars = 3;
        var currentVal = 0;

        for (var i = 0; i < maxChars; i++)
        {
            if (Position < Text.Length && IsOctalDigit(this.CurrentChar))
            {
                var octalVal = this.CurrentChar - '0';
                Debug.Assert(octalVal is >= 0 and <= 7);
                currentVal *= 8;
                currentVal += octalVal;

                Position++;

                // Ecmascript doesn't allow octal values above 32 (0x20 in hex). Note: we do
                // *not* add a diagnostic.  This is not an error situation. The .NET lexer
                // simply stops once it hits a value greater than a legal octal value.
                if (HasOption(options, RegexOptions.ECMAScript) && currentVal >= 0x20)
                {
                    break;
                }
            }
        }

        Debug.Assert(Position - start > 0);

        var result = CreateToken(
            RegexKind.TextToken, [], GetSubPatternToCurrentPos(start));

        return result;
    }
}
