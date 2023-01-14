// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace Roslyn.Utilities
{
    internal static class StringEscapeEncoder
    {
        public static string Escape(this string text, char escapePrefix, params char[] prohibitedCharacters)
        {
            StringBuilder? builder = null;

            var startIndex = 0;
            while (startIndex < text.Length)
            {
                var prefixIndex = text.IndexOf(escapePrefix, startIndex);
                var prohibitIndex = text.IndexOfAny(prohibitedCharacters, startIndex);
                var index = prefixIndex >= 0 && prohibitIndex >= 0 ? Math.Min(prefixIndex, prohibitIndex)
                        : prefixIndex >= 0 ? prefixIndex
                        : prohibitIndex >= 0 ? prohibitIndex
                        : -1;

                if (index < 0)
                {
                    // append remaining text
                    builder?.Append(text, startIndex, text.Length - startIndex);

                    break;
                }

                builder ??= new StringBuilder();

                if (index > startIndex)
                {
                    // everything between the start and the prohibited character
                    builder.Append(text, startIndex, index - startIndex);
                }

                // add the escape prefix before the character that needs escaping
                builder.Append(escapePrefix);

                // add the prohibited character data as hex after the prefix
                builder.AppendFormat("{0:X2}", (int)text[index]);

                startIndex = index + 1;
            }

            if (builder != null)
            {
                return builder.ToString();
            }
            else
            {
                return text;
            }
        }

        public static string Unescape(this string text, char escapePrefix)
        {
            StringBuilder? builder = null;
            var startIndex = 0;

            while (startIndex < text.Length)
            {
                var index = text.IndexOf(escapePrefix, startIndex);
                if (index < 0)
                {
                    // append remaining text
                    builder?.Append(text, startIndex, text.Length - startIndex);

                    break;
                }

                builder ??= new StringBuilder();

                // add everything up to the escape prefix
                builder.Append(text, startIndex, index - startIndex);

                // skip over the escape prefix and the following character that was escaped
                var hex = ParseHex(text, index + 1, 2);
                builder.Append((char)hex);

                startIndex = index + 3; // includes escape + 2 hex digits
            }

            if (builder != null)
            {
                return builder.ToString();
            }
            else
            {
                return text;
            }
        }

        private static int ParseHex(string text, int start, int length)
        {
            var value = 0;

            for (int i = start, end = start + length; i < end; i++)
            {
                var ch = text[i];
                if (!IsHexDigit(ch))
                {
                    break;
                }

                value = (value << 4) + GetHexValue(ch);
            }

            return value;
        }

        private static bool IsHexDigit(char ch)
        {
            return ch is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';
        }

        private static int GetHexValue(char ch)
        {
            if (ch is >= '0' and <= '9')
            {
                return ch - '0';
            }
            else if (ch is >= 'A' and <= 'F')
            {
                return (ch - 'A') + 10;
            }
            else if (ch is >= 'a' and <= 'f')
            {
                return (ch - 'a') + 10;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
    }
}
