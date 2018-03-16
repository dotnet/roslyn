// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VirtualChars;

namespace Microsoft.CodeAnalysis.CSharp.VirtualChars
{
    [ExportLanguageService(typeof(IVirtualCharService), LanguageNames.CSharp), Shared]
    internal class CSharpVirtualCharService : AbstractVirtualCharService
    {
        public static readonly IVirtualCharService Instance = new CSharpVirtualCharService();

        protected override ImmutableArray<VirtualChar> TryConvertToVirtualCharsWorker(SyntaxToken token)
        {
            Debug.Assert(!token.ContainsDiagnostics);
            if (token.Kind() != SyntaxKind.StringLiteralToken)
            {
                return default;
            }

            return token.IsVerbatimStringLiteral()
                ? TryConvertVerbatimStringToVirtualChars(token)
                : TryConvertStringToVirtualChars(token);
        }

        private ImmutableArray<VirtualChar> TryConvertVerbatimStringToVirtualChars(SyntaxToken token)
            => TryConvertSimpleDoubleQuoteString(token, "@\"", "\"");

        private ImmutableArray<VirtualChar> TryConvertStringToVirtualChars(SyntaxToken token)
        {
            const string StartDelimeter = "\"";
            const string EndDelimeter = "\"";

            var tokenText = token.Text;
            if (!tokenText.StartsWith(StartDelimeter) ||
                !tokenText.EndsWith(EndDelimeter))
            {
                Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                return default;
            }

            var startIndexInclusive = StartDelimeter.Length;
            var endIndexExclusive = tokenText.Length - EndDelimeter.Length;

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
                    else
                    {
                        result.Add(new VirtualChar(tokenText[index], new TextSpan(offset + index, 1)));
                        index++;
                    }
                }

                return result.ToImmutable();
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
                case '0':
                    ch = (char)0;
                    break;
                case 'a':
                    ch = '\u0007';
                    break;
                case 'b':
                    ch = '\u0008';
                    break;
                case 'f':
                    ch = '\u000c';
                    break;
                case 'n':
                    ch = '\u000a';
                    break;
                case 'r':
                    ch = '\u000d';
                    break;
                case 't':
                    ch = '\u0009';
                    break;
                case 'v':
                    ch = '\u000b';
                    break;
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

                var endIndex = index + 1;
                for (var i = 0; i < 4; i++)
                {
                    var ch2 = tokenText[index + i];
                    if (!IsHexDigit(ch2))
                    {
                        Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                        return false;
                    }

                    intChar = (intChar << 4) + HexValue(ch2);
                    endIndex++;
                }

                character = (char)intChar;
                result.Add(new VirtualChar(character, new TextSpan(startIndex + offset, 2 + 4)));
                return true;
            }
            else
            {
                Debug.Assert(character == 'x');
                // Variable length (up to 4 chars) hexidecimal escape.

                var intChar = 0;
                if (!IsHexDigit(tokenText[index]))
                {
                    Debug.Fail("This should not be reachable as long as the compiler added no diagnostics.");
                    return false;
                }

                var endIndex = index;
                for (var i = 0; i < 4; i++)
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
