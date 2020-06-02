﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars
{
    internal class CSharpVirtualCharService : AbstractVirtualCharService
    {
        public static readonly IVirtualCharService Instance = new CSharpVirtualCharService();

        protected CSharpVirtualCharService()
        {
        }

        protected override bool IsStringOrCharLiteralToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.StringLiteralToken ||
               token.Kind() == SyntaxKind.CharacterLiteralToken;

        protected override VirtualCharSequence TryConvertToVirtualCharsWorker(SyntaxToken token)
        {
            // C# preprocessor directives can contain string literals.  However, these string
            // literals do not behave like normal literals.  Because they are used for paths (i.e.
            // in a #line directive), the language does not do any escaping within them.  i.e. if
            // you have a \ it's just a \   Note that this is not a verbatim string.  You can't put
            // a double quote in it either, and you cannot have newlines and whatnot.
            //
            // We technically could convert this trivially to an array of virtual chars.  After all,
            // there would just be a 1:1 correspondance with the literal contents and the chars
            // returned.  However, we don't even both returning anything here.  That's because
            // there's no useful features we can offer here.  Because there are no escape characters
            // we won't classify any escape characters.  And there is no way that these strings would
            // be Regex/Json snippets.  So it's easier to just bail out and return nothing.
            if (IsInDirective(token.Parent))
            {
                return default;
            }

            Debug.Assert(!token.ContainsDiagnostics);
            if (token.Kind() == SyntaxKind.StringLiteralToken)
            {
                return token.IsVerbatimStringLiteral()
                    ? TryConvertVerbatimStringToVirtualChars(token, "@\"", "\"", escapeBraces: false)
                    : TryConvertStringToVirtualChars(token, "\"", "\"", escapeBraces: false);
            }

            if (token.Kind() == SyntaxKind.CharacterLiteralToken)
            {
                return TryConvertStringToVirtualChars(token, "'", "'", escapeBraces: false);
            }

            if (token.Kind() == SyntaxKind.InterpolatedStringTextToken)
            {
                // The sections between  `}` and `{` are InterpolatedStringTextToken *as are* the
                // format specifiers in an interpolated string.  We only want to get the virtual
                // chars for this first type.
                if (token.Parent.Parent is InterpolatedStringExpressionSyntax interpolatedString)
                {
                    return interpolatedString.StringStartToken.Kind() == SyntaxKind.InterpolatedVerbatimStringStartToken
                       ? TryConvertVerbatimStringToVirtualChars(token, "", "", escapeBraces: true)
                       : TryConvertStringToVirtualChars(token, "", "", escapeBraces: true);
                }
            }

            return default;
        }

        private static bool IsInDirective(SyntaxNode node)
        {
            while (node != null)
            {
                if (node is DirectiveTriviaSyntax)
                {
                    return true;
                }

                node = node.GetParent(ascendOutOfTrivia: true);
            }

            return false;
        }

        private static VirtualCharSequence TryConvertVerbatimStringToVirtualChars(SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
            => TryConvertSimpleDoubleQuoteString(token, startDelimiter, endDelimiter, escapeBraces);

        private static VirtualCharSequence TryConvertStringToVirtualChars(
            SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
        {
            var tokenText = token.Text;
            if (startDelimiter.Length > 0 && !tokenText.StartsWith(startDelimiter))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }
            if (endDelimiter.Length > 0 && !tokenText.EndsWith(endDelimiter))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }

            var startIndexInclusive = startDelimiter.Length;
            var endIndexExclusive = tokenText.Length - endDelimiter.Length;

            // Do things in two passes.  First, convert everything in the string to a 16-bit-char+span.  Then walk
            // again, trying to create Runes from the 16-bit-chars. We do this to simplify complex cases where we may
            // have escapes and non-escapes mixed together.

            using var _1 = ArrayBuilder<(char ch, TextSpan span)>.GetInstance(out var charResults);

            // First pass, just convert everything in the string (i.e. escapes) to plain 16-bit characters.
            var offset = token.SpanStart;
            for (var index = startIndexInclusive; index < endIndexExclusive;)
            {
                var ch = tokenText[index];
                if (ch == '\\')
                {
                    if (!TryAddEscape(charResults, tokenText, offset, index))
                        return default;

                    index += charResults.Last().span.Length;
                }
                else if (escapeBraces && IsOpenOrCloseBrace(ch))
                {
                    if (!IsLegalBraceEscape(tokenText, index, offset, out var braceSpan))
                        return default;

                    charResults.Add((ch, braceSpan));
                    index += charResults.Last().span.Length;
                }
                else
                {
                    charResults.Add((ch, new TextSpan(offset + index, 1)));
                    index++;
                }
            }

            // Second pass.  Convert those characters to Runes.
            using var _2 = ArrayBuilder<VirtualChar>.GetInstance(out var runeResults);

            for (var i = 0; i < charResults.Count;)
            {
                var (ch, span) = charResults[i];

                // First, see if this was a valid single char that can become a Rune.
                if (Rune.TryCreate(ch, out var rune))
                {
                    runeResults.Add(VirtualChar.Create(rune, span));
                    i++;
                    continue;
                }

                // Next, see if we got at least a surrogate pair that can be converted into a Rune.
                if (i + 1 < charResults.Count)
                {
                    var (nextCh, nextSpan) = charResults[i + 1];
                    if (Rune.TryCreate(ch, nextCh, out rune))
                    {
                        runeResults.Add(VirtualChar.Create(rune, TextSpan.FromBounds(span.Start, nextSpan.End)));
                        i += 2;
                        continue;
                    }
                }

                // Had an unpaired surrogate.
                Debug.Assert(char.IsSurrogate(ch));
                runeResults.Add(VirtualChar.Create(ch, span));
                i++;
            }

            return CreateVirtualCharSequence(
                tokenText, offset, startIndexInclusive, endIndexExclusive, runeResults);
        }

        private static bool TryAddEscape(
            ArrayBuilder<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
        {
            // Copied from Lexer.ScanEscapeSequence.
            Debug.Assert(tokenText[index] == '\\');

            return TryAddSingleCharacterEscape(result, tokenText, offset, index) ||
                   TryAddMultiCharacterEscape(result, tokenText, offset, index);
        }

        public override bool TryGetEscapeCharacter(VirtualChar ch, out char escapedChar)
        {
            // Keep in sync with TryAddSingleCharacterEscape
            switch (ch.Value)
            {
                // Note: we don't care about single quote as that doesn't need to be escaped when
                // producing a normal C# string literal.

                // case '\'':

                // escaped characters that translate to themselves.  
                case '"': escapedChar = '"'; return true;
                case '\\': escapedChar = '\\'; return true;

                // translate escapes as per C# spec 2.4.4.4
                case '\0': escapedChar = '0'; return true;
                case '\a': escapedChar = 'a'; return true;
                case '\b': escapedChar = 'b'; return true;
                case '\f': escapedChar = 'f'; return true;
                case '\n': escapedChar = 'n'; return true;
                case '\r': escapedChar = 'r'; return true;
                case '\t': escapedChar = 't'; return true;
                case '\v': escapedChar = 'v'; return true;
            }

            escapedChar = default;
            return false;
        }

        private static bool TryAddSingleCharacterEscape(
            ArrayBuilder<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
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
                case 'f': ch = '\f'; break;
                case 'n': ch = '\n'; break;
                case 'r': ch = '\r'; break;
                case 't': ch = '\t'; break;
                case 'v': ch = '\v'; break;
                default:
                    return false;
            }

            result.Add((ch, new TextSpan(offset + index, 2)));
            return true;
        }

        private static bool TryAddMultiCharacterEscape(
            ArrayBuilder<(char ch, TextSpan span)> result, string tokenText, int offset, int index)
        {
            // Copied from Lexer.ScanEscapeSequence.
            Debug.Assert(tokenText[index] == '\\');

            var ch = tokenText[index + 1];
            switch (ch)
            {
                case 'x':
                case 'u':
                case 'U':
                    return TryAddMultiCharacterEscape(result, tokenText, offset, index, ch);
                default:
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
            }
        }

        private static bool TryAddMultiCharacterEscape(
            ArrayBuilder<(char ch, TextSpan span)> result, string tokenText, int offset, int index, char character)
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
                    return false;
                }

                for (var i = 0; i < 8; i++)
                {
                    character = tokenText[index + i];
                    if (!IsHexDigit(character))
                    {
                        Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                        return false;
                    }

                    uintChar = (uint)((uintChar << 4) + HexValue(character));
                }

                // Copied from Lexer.cs and SlidingTextWindow.cs

                if (uintChar > 0x0010FFFF)
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                if (uintChar < (uint)0x00010000)
                {
                    // something like \U0000000A
                    //
                    // Represents a single char value.
                    result.Add(((char)uintChar, new TextSpan(startIndex + offset, 2 + 8)));
                    return true;
                }
                else
                {
                    Debug.Assert(uintChar > 0x0000FFFF && uintChar <= 0x0010FFFF);
                    var lowSurrogate = ((uintChar - 0x00010000) % 0x0400) + 0xDC00;
                    var highSurrogate = ((uintChar - 0x00010000) / 0x0400) + 0xD800;

                    // Encode this as a surrogate pair.
                    var pos = startIndex + offset;
                    result.Add(((char)highSurrogate, new TextSpan(pos, 0)));
                    result.Add(((char)lowSurrogate, new TextSpan(pos, 2 + 8)));
                    return true;
                }
            }
            else if (character == 'u')
            {
                // 4 character escape representing one char.

                var intChar = 0;
                if (!IsHexDigit(tokenText[index]))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                for (var i = 0; i < 4; i++)
                {
                    var ch2 = tokenText[index + i];
                    if (!IsHexDigit(ch2))
                    {
                        Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                        return false;
                    }

                    intChar = (intChar << 4) + HexValue(ch2);
                }

                character = (char)intChar;
                result.Add((character, new TextSpan(startIndex + offset, 2 + 4)));
                return true;
            }
            else
            {
                Debug.Assert(character == 'x');
                // Variable length (up to 4 chars) hexadecimal escape.

                var intChar = 0;
                if (!IsHexDigit(tokenText[index]))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
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
                result.Add((character, TextSpan.FromBounds(startIndex + offset, endIndex + offset)));
                return true;
            }
        }

        private static int HexValue(char c)
        {
            Debug.Assert(IsHexDigit(c));
            return (c >= '0' && c <= '9') ? c - '0' : (c & 0xdf) - 'A' + 10;
        }

        private static bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'A' && c <= 'F') ||
                   (c >= 'a' && c <= 'f');
        }
    }
}
