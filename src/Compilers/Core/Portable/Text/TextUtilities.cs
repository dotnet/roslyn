// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Holder for common Text Utility functions and values
    /// </summary>
    internal static class TextUtilities
    {
        // Note: a small amount of this below logic is also inlined into SourceText.ParseLineBreaks
        // for performance reasons.
        internal static int GetLengthOfLineBreak(SourceText text, int index)
        {
            var c = text[index];

            // common case - ASCII & not a line break
            // if (c > '\r' && c <= 127)
            // if (c >= ('\r'+1) && c <= 127)
            const uint bias = '\r' + 1;
            if (unchecked(c - bias) <= (127 - bias))
            {
                return 0;
            }

            return GetLengthOfLineBreakSlow(text, index, c);
        }

        private static int GetLengthOfLineBreakSlow(SourceText text, int index, char c)
        {
            if (c == '\r')
            {
                var next = index + 1;
                return (next < text.Length) && '\n' == text[next] ? 2 : 1;
            }
            else if (IsAnyLineBreakCharacter(c))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Return startLineBreak = index-1, lengthLineBreak = 2   if there is a \r\n at index-1
        /// Return startLineBreak = index,   lengthLineBreak = 1   if there is a 1-char newline at index
        /// Return startLineBreak = index+1, lengthLineBreak = 0   if there is no newline at index.
        /// </summary>
        public static void GetStartAndLengthOfLineBreakEndingAt(SourceText text, int index, out int startLinebreak, out int lengthLinebreak)
        {
            char c = text[index];
            if (c == '\n')
            {
                if (index > 0 && text[index - 1] == '\r')
                {
                    // "\r\n" is the only 2-character line break.
                    startLinebreak = index - 1;
                    lengthLinebreak = 2;
                }
                else
                {
                    startLinebreak = index;
                    lengthLinebreak = 1;
                }
            }
            else if (IsAnyLineBreakCharacter(c))
            {
                startLinebreak = index;
                lengthLinebreak = 1;
            }
            else
            {
                startLinebreak = index + 1;
                lengthLinebreak = 0;
            }
        }

        /// <summary>
        /// Determine if the character in question is any line break character
        /// </summary>
        internal static bool IsAnyLineBreakCharacter(char c)
        {
            return c == '\n' || c == '\r' || c == '\u0085' || c == '\u2028' || c == '\u2029';
        }
    }
}
