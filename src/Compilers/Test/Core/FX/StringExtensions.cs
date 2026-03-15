// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Test.Utilities
{
    public static class StringExtensions
    {
        public static string NormalizeLineEndings(this string input)
        {
            if (input.Contains("\n") && !input.Contains("\r\n"))
            {
                input = input.Replace("\n", "\r\n");
            }

            return input;
        }

        /// <summary>
        /// Normalizes only the common line ending sequences (CRLF, CR, LF) to the specified
        /// <paramref name="replacementText"/>. Unlike <see cref="ReplaceLineEndings(string, string)"/>,
        /// this does NOT replace form feed (FF), vertical tab (VT), NEL, LS, or PS characters,
        /// which may be semantically significant in source text.
        /// </summary>
        public static string NormalizePlatformLineEndings(this string input, string replacementText = "\r\n")
        {
            return input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", replacementText);
        }

        public static string ReplaceLineEndings(this string input)
        {
#if NET6_0_OR_GREATER
            return input.ReplaceLineEndings();
#else
            return ReplaceLineEndings(input, Environment.NewLine);
#endif
        }

        public static string ReplaceLineEndings(this string input, string replacementText)
        {
#if NET6_0_OR_GREATER
            return input.ReplaceLineEndings(replacementText);
#else
            // First normalize to LF
            var lineFeedInput = input
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Replace("\f", "\n")
                .Replace("\x0085", "\n")
                .Replace("\x2028", "\n")
                .Replace("\x2029", "\n");

            // Then normalize to the replacement text
            return lineFeedInput.Replace("\n", replacementText);
#endif
        }
    }
}
