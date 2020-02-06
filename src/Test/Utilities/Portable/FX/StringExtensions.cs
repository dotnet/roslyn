// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Roslyn.Test.Utilities
{
    public static class StringExtensions
    {
        public static string? GetLineBreak(this string str)
            => str.Contains("\r\n") ? "\r\n" : str.Contains("\n") ? "\n" : str.Contains("\r") ? "\r" : null;

        /// <summary>
        /// Normalize line endings to Windows style.
        /// </summary>
        public static string NormalizeLineEndings(this string input)
        {
            var lineBreak = input.GetLineBreak();
            return (lineBreak == "\n" || lineBreak == "\r") ? input.Replace(lineBreak, "\r\n") : input;
        }

        /// <summary>
        /// Normalize line endings to specified style ("\r\n", "\n", "\r").
        /// </summary>
        public static string NormalizeLineEndings(this string input, string lineBreak)
        {
            if (lineBreak == "\n")
            {
                return input.Replace("\r\n", lineBreak).Replace("\r", lineBreak);
            }

            if (lineBreak == "\r")
            {
                return input.Replace("\r\n", lineBreak).Replace("\n", lineBreak);
            }

            return input.NormalizeLineEndings();
        }
    }
}
