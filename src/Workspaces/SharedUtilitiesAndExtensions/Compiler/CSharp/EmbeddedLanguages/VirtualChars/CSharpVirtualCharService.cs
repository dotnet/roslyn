// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars;

internal class CSharpVirtualCharService : AbstractVirtualCharService
{
    public static readonly IVirtualCharService Instance = new CSharpVirtualCharService();

    private static readonly ObjectPool<ImmutableSegmentedList<VirtualCharGreen>.Builder> s_pooledBuilders = new(() => ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>());

    protected CSharpVirtualCharService()
    {
    }

    protected override ISyntaxFacts SyntaxFacts => CSharpSyntaxFacts.Instance;

    protected override bool IsMultiLineRawStringToken(SyntaxToken token)
    {
        if (token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
            return true;

        if (token.Parent?.Parent is InterpolatedStringExpressionSyntax { StringStartToken.RawKind: (int)SyntaxKind.InterpolatedMultiLineRawStringStartToken })
            return true;

        return false;
    }

    protected override VirtualCharGreenSequence TryConvertToVirtualCharsWorker(SyntaxToken token)
    {
        // C# preprocessor directives can contain string literals.  However, these string literals do not behave
        // like normal literals.  Because they are used for paths (i.e. in a #line directive), the language does not
        // do any escaping within them.  i.e. if you have a \ it's just a \   Note that this is not a verbatim
        // string.  You can't put a double quote in it either, and you cannot have newlines and whatnot.
        //
        // We technically could convert this trivially to an array of virtual chars.  After all, there would just be
        // a 1:1 correspondence with the literal contents and the chars returned.  However, we don't even both
        // returning anything here.  That's because there's no useful features we can offer here.  Because there are
        // no escape characters we won't classify any escape characters.  And there is no way that these strings
        // would be Regex/Json snippets.  So it's easier to just bail out and return nothing.
        if (IsInDirective(token.Parent))
            return default;

        Debug.Assert(!token.ContainsDiagnostics);

        switch (token.Kind())
        {
            case SyntaxKind.CharacterLiteralToken:
                return TryConvertStringToVirtualChars(token, "'", "'", escapeBraces: false);

            case SyntaxKind.StringLiteralToken:
                return token.IsVerbatimStringLiteral()
                    ? TryConvertVerbatimStringToVirtualChars(token, "@\"", "\"", escapeBraces: false)
                    : TryConvertStringToVirtualChars(token, "\"", "\"", escapeBraces: false);

            case SyntaxKind.Utf8StringLiteralToken:
                return token.IsVerbatimStringLiteral()
                    ? TryConvertVerbatimStringToVirtualChars(token, "@\"", "\"u8", escapeBraces: false)
                    : TryConvertStringToVirtualChars(token, "\"", "\"u8", escapeBraces: false);

            case SyntaxKind.SingleLineRawStringLiteralToken:
            case SyntaxKind.Utf8SingleLineRawStringLiteralToken:
                return TryConvertSingleLineRawStringToVirtualChars(token);

            case SyntaxKind.MultiLineRawStringLiteralToken:
            case SyntaxKind.Utf8MultiLineRawStringLiteralToken:
                return token.GetRequiredParent() is LiteralExpressionSyntax literalExpression
                    ? TryConvertMultiLineRawStringToVirtualChars(token, literalExpression, tokenIncludeDelimiters: true)
                    : default;

            case SyntaxKind.InterpolatedStringTextToken:
                {
                    var parent = token.GetRequiredParent();
                    var isFormatClause = parent is InterpolationFormatClauseSyntax;
                    if (isFormatClause)
                        parent = parent.GetRequiredParent();

                    var interpolatedString = (InterpolatedStringExpressionSyntax)parent.GetRequiredParent();

                    return interpolatedString.StringStartToken.Kind() switch
                    {
                        SyntaxKind.InterpolatedStringStartToken => TryConvertStringToVirtualChars(token, "", "", escapeBraces: true),
                        SyntaxKind.InterpolatedVerbatimStringStartToken => TryConvertVerbatimStringToVirtualChars(token, "", "", escapeBraces: true),
                        SyntaxKind.InterpolatedSingleLineRawStringStartToken => TryConvertSingleLineRawStringToVirtualChars(token),
                        SyntaxKind.InterpolatedMultiLineRawStringStartToken
                            // Format clauses must be single line, even when in a multi-line interpolation.
                            => isFormatClause
                                ? TryConvertSingleLineRawStringToVirtualChars(token)
                                : TryConvertMultiLineRawStringToVirtualChars(token, interpolatedString, tokenIncludeDelimiters: false),
                        _ => default,
                    };
                }
        }

        return default;
    }

    private static bool IsInDirective(SyntaxNode? node)
    {
        while (node != null)
        {
            if (node is DirectiveTriviaSyntax)
                return true;

            node = node.GetParent(ascendOutOfTrivia: true);
        }

        return false;
    }

    private static VirtualCharGreenSequence TryConvertVerbatimStringToVirtualChars(SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
        => TryConvertSimpleDoubleQuoteString(token, startDelimiter, endDelimiter, escapeBraces);

    private static VirtualCharGreenSequence TryConvertSingleLineRawStringToVirtualChars(SyntaxToken token)
    {
        var tokenText = token.Text;

        var result = ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>();

        var startIndexInclusive = 0;
        var endIndexExclusive = tokenText.Length;

        if (token.Kind() is SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        {
            Contract.ThrowIfFalse(tokenText is [.., 'u' or 'U', '8']);
            endIndexExclusive -= "u8".Length;
        }

        if (token.Kind() is SyntaxKind.SingleLineRawStringLiteralToken or SyntaxKind.Utf8SingleLineRawStringLiteralToken)
        {
            Contract.ThrowIfFalse(tokenText[0] == '"');

            while (tokenText[startIndexInclusive] == '"')
            {
                // All quotes should be paired at the end
                Contract.ThrowIfFalse(tokenText[endIndexExclusive - 1] == '"');
                startIndexInclusive++;
                endIndexExclusive--;
            }
        }

        for (var index = startIndexInclusive; index < endIndexExclusive;)
            index += ConvertTextAtIndexToVirtualChar(tokenText, index, result);

        return CreateVirtualCharSequence(tokenText, startIndexInclusive, endIndexExclusive, result);
    }

    /// <summary>
    /// Creates the sequence for the <b>content</b> characters in this <paramref name="token"/>.  This will not
    /// include indentation whitespace that the language specifies is not part of the content.
    /// </summary>
    /// <param name="parentExpression">The containing expression for this token.  This is needed so that we can
    /// determine the indentation whitespace based on the last line of the containing multiline literal.</param>
    /// <param name="tokenIncludeDelimiters">If this token includes the quote (<c>"</c>) characters for the
    /// delimiters inside of it or not.  If so, then those quotes will need to be skipped when determining the
    /// content</param>
    private static VirtualCharGreenSequence TryConvertMultiLineRawStringToVirtualChars(
        SyntaxToken token, ExpressionSyntax parentExpression, bool tokenIncludeDelimiters)
    {
        // if this is the first text content chunk of the multi-line literal.  The first chunk contains the leading
        // indentation of the line it's on (which thus must be trimmed), while all subsequent chunks do not (because
        // they start right after some `{...}` interpolation
        var isFirstChunk =
            parentExpression is LiteralExpressionSyntax ||
            (parentExpression is InterpolatedStringExpressionSyntax { Contents: [var firstContent, ..] } && firstContent == token.GetRequiredParent());

        if (parentExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return default;

        // Use the parent multi-line expression to determine what whitespace to remove from the start of each line.
        var parentSourceText = parentExpression.SyntaxTree.GetText();
        var indentationLength = parentSourceText.Lines.GetLineFromPosition(parentExpression.Span.End).GetFirstNonWhitespaceOffset() ?? 0;

        // Create a source-text view over the token.  This makes it very easy to treat the token as a set of lines
        // that can be processed sensibly.
        var tokenSourceText = SourceText.From(token.Text);

        // If we're on the very first chunk of the multi-line raw string literal, then we want to start on line 1 so
        // we skip the space and newline that follow the initial `"""`.
        var startLineInclusive = tokenIncludeDelimiters ? 1 : 0;

        // Similarly, if we're on the very last chunk of hte multi-line raw string literal, then we don't want to
        // include the line contents for the line that has the final `    """` on it.
        var lastLineExclusive = tokenIncludeDelimiters ? tokenSourceText.Lines.Count - 1 : tokenSourceText.Lines.Count;

        var result = ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>();
        for (var lineNumber = startLineInclusive; lineNumber < lastLineExclusive; lineNumber++)
        {
            var currentLine = tokenSourceText.Lines[lineNumber];
            var lineSpan = currentLine.Span;
            var lineStart = lineSpan.Start;

            // If we're on the second line onwards, we want to trim the indentation if we have it.  We also always
            // do this for the first line of the first chunk as that will contain the initial leading whitespace.
            if (isFirstChunk || lineNumber > startLineInclusive)
            {
                lineStart = lineSpan.Length > indentationLength
                    ? lineSpan.Start + indentationLength
                    : lineSpan.End;
            }

            // The last line of the last chunk does not include the final newline on the line.
            var lineEnd = lineNumber == lastLineExclusive - 1 ? currentLine.End : currentLine.EndIncludingLineBreak;

            // Now that we've found the start and end portions of that line, convert all the characters within to
            // virtual chars and return.
            for (var i = lineStart; i < lineEnd;)
                i += ConvertTextAtIndexToVirtualChar(tokenSourceText, i, result);
        }

        return VirtualCharGreenSequence.Create(result.ToImmutable());
    }

    private static VirtualCharGreenSequence TryConvertStringToVirtualChars(
        SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
    {
        var tokenText = token.Text;
        if (startDelimiter.Length > 0 && !tokenText.StartsWith(startDelimiter))
        {
            Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        if (endDelimiter.Length > 0 && !tokenText.EndsWith(endDelimiter, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
            return default;
        }

        var startIndexInclusive = startDelimiter.Length;
        var endIndexExclusive = tokenText.Length - endDelimiter.Length;

        // Avoid creating and processsing the runes if there are no escapes or surrogates in the string.
        if (!ContainsEscape(tokenText.AsSpan(startIndexInclusive, endIndexExclusive - startIndexInclusive), escapeBraces))
        {
            var sequence = VirtualCharGreenSequence.Create(tokenText);
            return sequence[startIndexInclusive..endIndexExclusive];
        }
        else
        {
            using var pooledRuneResults = s_pooledBuilders.GetPooledObject();
            var charResults = pooledRuneResults.Object;

            for (var index = startIndexInclusive; index < endIndexExclusive;)
            {
                var ch = tokenText[index];
                if (ch == '\\')
                {
                    if (TryAddEscape(charResults, tokenText, index) is not int escapeWidth)
                        return default;

                    index += escapeWidth;
                }
                else if (escapeBraces && IsOpenOrCloseBrace(ch))
                {
                    if (!IsLegalBraceEscape(tokenText, index, out var braceWidth))
                        return default;

                    charResults.Add(new VirtualCharGreen(ch, index, braceWidth));
                    index += braceWidth;
                }
                else
                {
                    charResults.Add(new VirtualCharGreen(ch, index, width: 1));
                    index++;
                }
            }

            var sequence = CreateVirtualCharSequence(tokenText, startIndexInclusive, endIndexExclusive, charResults);
            charResults.Clear();

            return sequence;
        }
    }

    private static bool ContainsEscape(ReadOnlySpan<char> tokenText, bool escapeBraces)
    {
        foreach (var ch in tokenText)
        {
            if (ch == '\\')
                return true;
            else if (escapeBraces && IsOpenOrCloseBrace(ch))
                return true;
        }

        return false;
    }

    /// <summary>Returns the number of characters consumed.</summary>
    private static int? TryAddEscape(
        ImmutableSegmentedList<VirtualCharGreen>.Builder result,
        string tokenText,
        int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        return TryAddSingleCharacterEscape(result, tokenText, index) ??
               TryAddMultiCharacterEscape(result, tokenText, index);
    }

    public override bool TryGetEscapeCharacter(VirtualChar ch, out char escapedChar)
        => ch.TryGetEscapeCharacter(out escapedChar);

    /// <summary>Returns the number of characters consumed.</summary>
    private static int? TryAddSingleCharacterEscape(
        ImmutableSegmentedList<VirtualCharGreen>.Builder result, string tokenText, int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        var ch = tokenText[index + 1];

        // Keep in sync with EscapeForRegularString
        switch (ch)
        {
            // escaped characters that translate to themselves
            case '\'':
            case '"':
            case '\\':
                break;
            // translate escapes as per C# spec 2.4.4.4
            case '0': ch = '\0'; break;
            case 'a': ch = '\a'; break;
            case 'b': ch = '\b'; break;
            case 'e': ch = '\u001b'; break;
            case 'f': ch = '\f'; break;
            case 'n': ch = '\n'; break;
            case 'r': ch = '\r'; break;
            case 't': ch = '\t'; break;
            case 'v': ch = '\v'; break;
            default:
                return null;
        }

        result.Add(new VirtualCharGreen(ch, offset: index, width: 2));
        return result.Last().Width;
    }

    /// <summary>Returns the number of characters consumed.</summary>
    private static int? TryAddMultiCharacterEscape(
        ImmutableSegmentedList<VirtualCharGreen>.Builder result, string tokenText, int index)
    {
        // Copied from Lexer.ScanEscapeSequence.
        Debug.Assert(tokenText[index] == '\\');

        var ch = tokenText[index + 1];
        switch (ch)
        {
            case 'x':
            case 'u':
            case 'U':
                return TryAddMultiCharacterEscape(result, tokenText, index, ch);
            default:
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return null;
        }
    }

    /// <summary>Returns the number of characters consumed.</summary>
    private static int? TryAddMultiCharacterEscape(
        ImmutableSegmentedList<VirtualCharGreen>.Builder result,
        string tokenText,
        int index,
        char character)
    {
        var startIndex = index;
        Debug.Assert(tokenText[index] == '\\');

        // skip past the / and the escape type.
        index += 2;
        if (character == 'U')
        {
            // 8 character escape.  May represent 1 or 2 actual chars.
            uint uintChar = 0;

            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return null;
            }

            for (var i = 0; i < 8; i++)
            {
                character = tokenText[index + i];
                if (!IsHexDigit(character))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return null;
                }

                uintChar = (uint)((uintChar << 4) + HexValue(character));
            }

            // Copied from Lexer.cs and SlidingTextWindow.cs

            if (uintChar > 0x0010FFFF)
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return null;
            }

            if (uintChar < 0x00010000)
            {
                // something like \U0000000A
                //
                // Represents a single char value.
                result.Add(new VirtualCharGreen((char)uintChar, offset: startIndex, width: 2 + 8));
            }
            else
            {
                Debug.Assert(uintChar is > 0x0000FFFF and <= 0x0010FFFF);
                var lowSurrogate = ((uintChar - 0x00010000) % 0x0400) + 0xDC00;
                var highSurrogate = ((uintChar - 0x00010000) / 0x0400) + 0xD800;

                // Encode this as a surrogate pair.  For the purposes of mapping, we'll say the high surrogate maps to
                // the first 6 chars (the \UAAAA in \UAAAABBBB) and the low surrogate maps to the last 4 chars (the BBBB
                // in \UAAAABBBB).
                const string prefix = @"\UAAAA";
                result.Add(new VirtualCharGreen((char)highSurrogate, offset: startIndex, width: prefix.Length));
                result.Add(new VirtualCharGreen((char)lowSurrogate, offset: startIndex + prefix.Length, width: 4));
            }

            return @"\UAAAABBBB".Length;
        }
        else if (character == 'u')
        {
            // 4 character escape representing one char.

            var intChar = 0;
            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return null;
            }

            for (var i = 0; i < 4; i++)
            {
                var ch2 = tokenText[index + i];
                if (!IsHexDigit(ch2))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return null;
                }

                intChar = (intChar << 4) + HexValue(ch2);
            }

            character = (char)intChar;
            var width = @"\uAAAA".Length;
            result.Add(new VirtualCharGreen(character, offset: startIndex, width));
            return width;
        }
        else
        {
            Debug.Assert(character == 'x');
            // Variable length (up to 4 chars) hexadecimal escape.

            var intChar = 0;
            if (!IsHexDigit(tokenText[index]))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return null;
            }

            var endIndex = index;
            for (var i = 0; i < 4 && endIndex < tokenText.Length; i++)
            {
                var ch2 = tokenText[index + i];
                if (!IsHexDigit(ch2))
                {
                    // This is possible.  These escape sequences are variable length.
                    break;
                }

                intChar = (intChar << 4) + HexValue(ch2);
                endIndex++;
            }

            character = (char)intChar;
            var width = endIndex - startIndex;
            result.Add(new VirtualCharGreen(character, offset: startIndex, width));
            return width;
        }
    }

    private static int HexValue(char c)
    {
        Debug.Assert(IsHexDigit(c));
        return (c is >= '0' and <= '9') ? c - '0' : (c & 0xdf) - 'A' + 10;
    }

    private static bool IsHexDigit(char c)
        => c is (>= '0' and <= '9') or
                (>= 'A' and <= 'F') or
                (>= 'a' and <= 'f');
}
