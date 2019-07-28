// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages.VirtualChars
{
    [ExportLanguageService(typeof(IVirtualCharService), LanguageNames.CSharp), Shared]
    internal class CSharpVirtualCharService : AbstractVirtualCharService
    {
        public static readonly IVirtualCharService Instance = new CSharpVirtualCharService();

        [ImportingConstructor]
        public CSharpVirtualCharService()
        {
        }

        protected override bool IsStringLiteralToken(SyntaxToken token)
            => token.Kind() == SyntaxKind.StringLiteralToken;

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

        private bool IsInDirective(SyntaxNode node)
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

        private VirtualCharSequence TryConvertVerbatimStringToVirtualChars(SyntaxToken token, string startDelimiter, string endDelimiter, bool escapeBraces)
            => TryConvertSimpleDoubleQuoteString(token, startDelimiter, endDelimiter, escapeBraces);

        private VirtualCharSequence TryConvertStringToVirtualChars(
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

            var result = ArrayBuilder<VirtualChar>.GetInstance();
            try
            {
                var offset = token.SpanStart;
                for (var index = startIndexInclusive; index < endIndexExclusive;)
                {
                    if (tokenText[index] == '\\')
                    {
                        if (!TryAddEscape(result, tokenText, offset, index))
                        {
                            return default;
                        }

                        index += result.Last().Span.Length;
                    }
                    else if (escapeBraces &&
                             (tokenText[index] == '{' || tokenText[index] == '}'))
                    {
                        if (!TryAddBraceEscape(result, tokenText, offset, index))
                        {
                            return default;
                        }

                        index += result.Last().Span.Length;
                    }
                    else
                    {
                        result.Add(new VirtualChar(tokenText[index], new TextSpan(offset + index, 1)));
                        index++;
                    }
                }

                return CreateVirtualCharSequence(
                    tokenText, offset, startIndexInclusive, endIndexExclusive, result);
            }
            finally
            {
                result.Free();
            }
        }

        private bool TryAddEscape(
            ArrayBuilder<VirtualChar> result, string tokenText, int offset, int index)
        {
            // Copied from Lexer.ScanEscapeSequence.
            Debug.Assert(tokenText[index] == '\\');

            return TryAddSingleCharacterEscape(result, tokenText, offset, index) ||
                   TryAddMultiCharacterEscape(result, tokenText, offset, index);
        }

        private bool TryAddSingleCharacterEscape(
            ArrayBuilder<VirtualChar> result, string tokenText, int offset, int index)
        {
            // Copied from Lexer.ScanEscapeSequence.
            Debug.Assert(tokenText[index] == '\\');

            var ch = tokenText[index + 1];
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

            result.Add(new VirtualChar(ch, new TextSpan(offset + index, 2)));
            return true;
        }

        private bool TryAddMultiCharacterEscape(
            ArrayBuilder<VirtualChar> result, string tokenText, int offset, int index)
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

        private bool TryAddMultiCharacterEscape(
            ArrayBuilder<VirtualChar> result, string tokenText, int offset, int index, char character)
        {
            var startIndex = index;
            Debug.Assert(tokenText[index] == '\\');

            // skip past the / and the escape type.
            index += 2;
            if (character == 'U')
            {
                // 8 character escape.  May represent 1 or 2 actual chars.  In the case of
                // 2 chars, we will fail out as that isn't supported in this system (currently).
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

                if (uintChar > 0x0010FFFF)
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                // Surrogate characters aren't supported here.
                if (uintChar >= 0x00010000)
                {
                    // This is possible.  It's a legal C# escape, but we don't support it here because it
                    // would need two chars to encode.
                    return false;
                }

                result.Add(new VirtualChar((char)uintChar, new TextSpan(startIndex + offset, 2 + 8)));
                return true;
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
                result.Add(new VirtualChar(character, new TextSpan(startIndex + offset, 2 + 4)));
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
                result.Add(new VirtualChar(character, TextSpan.FromBounds(startIndex + offset, endIndex + offset)));
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
