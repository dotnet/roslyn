// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;

using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

internal struct StackFrameLexer
{
    public readonly VirtualCharSequence Text;
    public int Position { get; private set; }

    private StackFrameLexer(string text)
        : this(VirtualCharSequence.Create(0, text))
    {
    }

    private StackFrameLexer(VirtualCharSequence text) : this()
        => Text = text;

    public static StackFrameLexer? TryCreate(string text)
    {
        foreach (var c in text)
        {
            if (c == '\r' || c == '\n')
            {
                return null;
            }
        }

        return new(text);
    }

    public static StackFrameLexer? TryCreate(VirtualCharSequence text)
    {
        foreach (var c in text)
        {
            if (c.Value == '\r' || c.Value == '\n')
            {
                return null;
            }
        }

        return new(text);
    }

    public readonly VirtualChar CurrentChar => Position < Text.Length ? Text[Position] : default;

    public readonly VirtualCharSequence GetSubSequenceToCurrentPos(int start)
        => GetSubSequence(start, Position);

    public readonly VirtualCharSequence GetSubSequence(int start, int end)
        => Text.GetSubSequence(TextSpan.FromBounds(start, end));

    public StackFrameTrivia? TryScanRemainingTrivia()
    {
        if (Position == Text.Length)
        {
            return null;
        }

        var start = Position;
        Position = Text.Length;

        return CreateTrivia(StackFrameKind.SkippedTextTrivia, GetSubSequenceToCurrentPos(start));
    }

    public StackFrameToken? TryScanIdentifier()
        => TryScanIdentifier(scanAtTrivia: false, scanLeadingWhitespace: false, scanTrailingWhitespace: false);

    public StackFrameToken? TryScanIdentifier(bool scanAtTrivia, bool scanLeadingWhitespace, bool scanTrailingWhitespace)
    {
        var originalPosition = Position;
        var atTrivia = scanAtTrivia ? TryScanAtTrivia() : null;
        var leadingWhitespace = scanLeadingWhitespace ? TryScanWhiteSpace() : null;

        var startPosition = Position;
        var ch = CurrentChar;
        if (!UnicodeCharacterUtilities.IsIdentifierStartCharacter((char)ch.Value))
        {
            var ctor = TryScanConstructor();
            if (ctor.HasValue)
            {
                return ctor.Value;
            }

            // If we scan only trivia but don't get an identifier, we want to make sure
            // to reset back to this original position to let the trivia be consumed
            // in some other fashion if necessary 
            Position = originalPosition;
            return null;
        }

        while (UnicodeCharacterUtilities.IsIdentifierPartCharacter((char)ch.Value))
        {
            Position++;
            ch = CurrentChar;
        }

        var identifierSequence = GetSubSequenceToCurrentPos(startPosition);
        var trailingWhitespace = scanTrailingWhitespace ? TryScanWhiteSpace() : null;

        return CreateToken(
            StackFrameKind.IdentifierToken,
            leadingTrivia: CreateTrivia(atTrivia, leadingWhitespace),
            identifierSequence,
            trailingTrivia: CreateTrivia(trailingWhitespace));
    }

    public readonly StackFrameToken CurrentCharAsToken()
    {
        if (Position == Text.Length)
        {
            return CreateToken(StackFrameKind.EndOfFrame, VirtualCharSequence.Empty);
        }

        var ch = Text[Position];
        return CreateToken(GetKind(ch), Text.GetSubSequence(new TextSpan(Position, 1)));
    }

    /// <summary>
    /// Progress the position by one if the current character
    /// matches the kind.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the position was incremented
    /// </returns>
    public bool ScanCurrentCharAsTokenIfMatch(StackFrameKind kind, out StackFrameToken token)
        => ScanCurrentCharAsTokenIfMatch(kind, scanTrailingWhitespace: false, out token);

    /// <summary>
    /// Progress the position by one if the current character
    /// matches the kind.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the position was incremented
    /// </returns>
    public bool ScanCurrentCharAsTokenIfMatch(StackFrameKind kind, bool scanTrailingWhitespace, out StackFrameToken token)
    {
        if (GetKind(CurrentChar) == kind)
        {
            token = CurrentCharAsToken();
            Position++;

            if (scanTrailingWhitespace)
            {
                token = token.With(trailingTrivia: CreateTrivia(TryScanWhiteSpace()));
            }

            return true;
        }

        token = default;
        return false;
    }

    /// <summary>
    /// Progress the position by one if the current character
    /// matches the kind.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the position was incremented
    /// </returns>
    public bool ScanCurrentCharAsTokenIfMatch(Func<StackFrameKind, bool> isMatch, out StackFrameToken token)
    {
        if (isMatch(GetKind(CurrentChar)))
        {
            token = CurrentCharAsToken();
            Position++;
            return true;
        }

        token = default;
        return false;
    }

    public StackFrameTrivia? TryScanAtTrivia()
    {
        foreach (var language in s_languages)
        {
            var result = TryScanStringTrivia(language.At, StackFrameKind.AtTrivia);
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return null;
    }

    public StackFrameTrivia? TryScanInTrivia()
    {
        foreach (var language in s_languages)
        {
            var result = TryScanStringTrivia(language.In, StackFrameKind.InTrivia);
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return null;
    }

    public StackFrameTrivia? TryScanLineTrivia()
    {
        foreach (var language in s_languages)
        {
            var result = TryScanStringTrivia(language.Line, StackFrameKind.LineTrivia);
            if (result.HasValue)
            {
                return result.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to parse <see cref="StackFrameKind.InTrivia"/> and a path following https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#file-and-directory-names
    /// Uses <see cref="FileInfo"/> as a tool to determine if the path is correct for returning. 
    /// </summary>
    public Result<StackFrameToken> TryScanPath()
    {
        var inTrivia = TryScanInTrivia();
        if (!inTrivia.HasValue)
        {
            return Result<StackFrameToken>.Empty;
        }

        var startPosition = Position;

        while (Position < Text.Length)
        {
            // Path needs to do a look ahead to determine if adding the next character
            // invalidates the path. Break if it does.
            //
            // This helps to keep the complex rules for what FileInfo does to validate path by calling to it directly.
            // We can't simply check all invalid characters for a path because location in the path is important, and we're not 
            // in the business of validating if something is correctly a file. It's cheap enough to let the FileInfo constructor do that. The downside 
            // is that we are constructing a new object with every pass. If this becomes problematic we can revisit and fine a more 
            // optimized pattern to handle all of the edge cases.
            //
            // Example: C:\my\path \:other
            //                      ^-- the ":" breaks the path, but we can't simply break on all ":" (which is included in the invalid characters for Path)
            //

            var str = GetSubSequence(startPosition, Position + 1).CreateString();

            var isValidPath = IOUtilities.PerformIO(() =>
            {
                var fileInfo = new FileInfo(str);
                return true;
            }, false);

            if (!isValidPath)
            {
                break;
            }

            Position++;
        }

        if (startPosition == Position)
        {
            return Result<StackFrameToken>.Abort;
        }

        return CreateToken(StackFrameKind.PathToken, inTrivia.ToImmutableArray(), GetSubSequenceToCurrentPos(startPosition));
    }

    /// <summary>
    /// Returns a number token with the <see cref="StackFrameKind.LineTrivia"/> and remaining <see cref="StackFrameKind.SkippedTextTrivia"/>
    /// attached to it. 
    /// </summary>
    /// <returns></returns>
    public StackFrameToken? TryScanRequiredLineNumber()
    {
        var lineTrivia = TryScanLineTrivia();
        if (!lineTrivia.HasValue)
        {
            return null;
        }

        var numberToken = TryScanNumbers();
        if (!numberToken.HasValue)
        {
            return null;
        }

        var remainingTrivia = TryScanRemainingTrivia();

        return numberToken.Value.With(
            leadingTrivia: lineTrivia.ToImmutableArray(),
            trailingTrivia: remainingTrivia.ToImmutableArray());
    }

    public StackFrameToken? TryScanNumbers()
    {
        var start = Position;
        while (IsNumber(CurrentChar))
        {
            Position++;
        }

        if (start == Position)
        {
            return null;
        }

        return CreateToken(StackFrameKind.NumberToken, GetSubSequenceToCurrentPos(start));
    }

    /// <summary>
    /// Scans a form similar to g__, where g is a GeneratedNameKind (a single character)
    /// and identifier is valid identifier characters as with <see cref="TryScanIdentifier()"/>
    /// </summary>
    public Result<StackFrameToken> TryScanRequiredGeneratedNameSeparator()
    {
        var start = Position;
        if (IsAsciiAlphaCharacter(CurrentChar))
        {
            Position++;
        }
        else
        {
            return Result<StackFrameToken>.Abort;
        }

        if (IsStringAtPosition("__"))
        {
            Position += 2;
        }
        else
        {
            return Result<StackFrameToken>.Abort;
        }

        return CreateToken(StackFrameKind.GeneratedNameSeparatorToken, GetSubSequenceToCurrentPos(start));
    }

    /// <summary>
    /// In a generated name for local functions the final portion is {numeric}_{numeric}
    /// </summary>
    public Result<StackFrameToken> TryScanRequiredGeneratedNameSuffix()
    {
        var start = Position;

        var n1 = TryScanNumbers();
        if (!n1.HasValue)
        {
            return Result<StackFrameToken>.Abort;
        }

        if (IsStringAtPosition("_"))
        {
            Position++;
        }
        else
        {
            return Result<StackFrameToken>.Abort;
        }

        var n2 = TryScanNumbers();
        if (!n2.HasValue)
        {
            return Result<StackFrameToken>.Abort;
        }

        return CreateToken(StackFrameKind.GeneratedNameSuffixToken, GetSubSequenceToCurrentPos(start));
    }

    public static bool IsBlank(VirtualChar ch)
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

    private static bool IsAsciiAlphaCharacter(VirtualChar ch)
        => ch.Value is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

    public static StackFrameToken CreateToken(StackFrameKind kind, VirtualCharSequence virtualChars)
        => CreateToken(kind, [], virtualChars);

    public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars)
        => new(kind, leadingTrivia, virtualChars, [], [], value: null!);

    public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars, ImmutableArray<StackFrameTrivia> trailingTrivia)
        => new(kind, leadingTrivia, virtualChars, trailingTrivia, [], value: null!);

    private static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars)
        => CreateTrivia(kind, virtualChars, []);

    private static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
    {
        // Empty trivia is not supported in StackFrames
        Debug.Assert(virtualChars.Length > 0);
        return new(kind, virtualChars, diagnostics);
    }

    private static ImmutableArray<StackFrameTrivia> CreateTrivia(params StackFrameTrivia?[] triviaArray)
    {
        using var _ = ArrayBuilder<StackFrameTrivia>.GetInstance(out var builder);
        foreach (var trivia in triviaArray)
        {
            if (trivia.HasValue)
            {
                builder.Add(trivia.Value);
            }
        }

        return builder.ToImmutable();
    }

    private readonly bool IsStringAtPosition(string val)
       => IsAtStartOfText(Position, val);

    private readonly bool IsAtStartOfText(int position, string val)
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

    private StackFrameTrivia? TryScanStringTrivia(string valueToLookFor, StackFrameKind triviaKind)
    {
        if (IsStringAtPosition(valueToLookFor))
        {
            var start = Position;
            Position += valueToLookFor.Length;

            return CreateTrivia(triviaKind, GetSubSequenceToCurrentPos(start));
        }

        return null;
    }

    private StackFrameToken? TryScanStringToken(string valueToLookFor, StackFrameKind tokenKind)
    {
        if (IsStringAtPosition(valueToLookFor))
        {
            var start = Position;
            Position += valueToLookFor.Length;

            return CreateToken(tokenKind, GetSubSequenceToCurrentPos(start));
        }

        return null;
    }

    private StackFrameTrivia? TryScanWhiteSpace()
    {
        var startPosition = Position;

        while (IsBlank(CurrentChar))
        {
            Position++;
        }

        if (Position == startPosition)
        {
            return null;
        }

        return CreateTrivia(StackFrameKind.WhitespaceTrivia, GetSubSequenceToCurrentPos(startPosition));
    }

    /// <summary>
    /// Scans for .ctor or .cctor as a ConstructorToken
    /// </summary>
    private StackFrameToken? TryScanConstructor()
    {
        return TryScanStringToken(".ctor", StackFrameKind.ConstructorToken) ??
               TryScanStringToken(".cctor", StackFrameKind.ConstructorToken);
    }

    private static StackFrameKind GetKind(VirtualChar ch)
        => ch.Value switch
        {
            '\n' => throw new InvalidOperationException(),
            '\r' => throw new InvalidOperationException(),
            '&' => StackFrameKind.AmpersandToken,
            '[' => StackFrameKind.OpenBracketToken,
            ']' => StackFrameKind.CloseBracketToken,
            '(' => StackFrameKind.OpenParenToken,
            ')' => StackFrameKind.CloseParenToken,
            '.' => StackFrameKind.DotToken,
            '+' => StackFrameKind.PlusToken,
            ',' => StackFrameKind.CommaToken,
            ':' => StackFrameKind.ColonToken,
            '=' => StackFrameKind.EqualsToken,
            '>' => StackFrameKind.GreaterThanToken,
            '<' => StackFrameKind.LessThanToken,
            '-' => StackFrameKind.MinusToken,
            '\'' => StackFrameKind.SingleQuoteToken,
            '`' => StackFrameKind.GraveAccentToken,
            '\\' => StackFrameKind.BackslashToken,
            '/' => StackFrameKind.ForwardSlashToken,
            '$' => StackFrameKind.DollarToken,
            '|' => StackFrameKind.PipeToken,
            _ => IsBlank(ch)
                ? StackFrameKind.WhitespaceTrivia
                : IsNumber(ch)
                    ? StackFrameKind.NumberToken
                    : StackFrameKind.SkippedTextTrivia
        };

    private static bool IsNumber(VirtualChar ch)
        => ch.Value is >= '0' and <= '9';

    private readonly record struct Language(string At, string In, string Line);
    private static readonly ImmutableArray<Language> s_languages =
    [
        new Language("at ", " in ", "line "),
        new Language("v ", " v ", "řádek "),
        new Language("bei ", " in ", "Zeile "),
        new Language("en ", " en ", "línea "),
        new Language("à ", " dans ", "ligne "),
        new Language("in ", " in ", "riga "),
        new Language("場所 ", " 場所 ", "行 "),
        new Language("위치: ", " 파일 ", "줄 "),
        new Language("w ", " w ", "wiersz "),
        new Language("em ", " na ", "linha "),
        new Language("в ", " в ", "строка "),
        new Language("在 ", " 位置 ", "行号 "),
        new Language("於 ", " 於 ", " 行 ")
, // zh-Hant
    ];
}
