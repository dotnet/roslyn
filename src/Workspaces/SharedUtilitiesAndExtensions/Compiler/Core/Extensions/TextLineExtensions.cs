﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class TextLineExtensions
    {
        public static int? GetLastNonWhitespacePosition(this TextLine line)
        {
            var startPosition = line.Start;
            var text = line.ToString();

            for (var i = text.Length - 1; i >= 0; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return startPosition + i;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the first non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespacePosition(this TextLine line)
        {
            var firstNonWhitespaceOffset = line.GetFirstNonWhitespaceOffset();

            return firstNonWhitespaceOffset.HasValue
                ? firstNonWhitespaceOffset + line.Start
                : null;
        }

        /// <summary>
        /// Returns the first non-whitespace position on the given line as an offset
        /// from the start of the line, or null if the line is empty or contains only
        /// whitespace.
        /// </summary>
        public static int? GetFirstNonWhitespaceOffset(this TextLine line)
            => line.ToString().GetFirstNonWhitespaceOffset();

        public static string GetLeadingWhitespace(this TextLine line)
            => line.ToString().GetLeadingWhitespace();

        /// <summary>
        /// Determines whether the specified line is empty or contains whitespace only.
        /// </summary>
        public static bool IsEmptyOrWhitespace(this TextLine line)
        {
            var text = line.Text;
            RoslynDebug.Assert(text is object);
            for (var i = line.Span.Start; i < line.Span.End; i++)
            {
                if (!char.IsWhiteSpace(text[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(this TextLine line, int tabSize)
            => line.ToString().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);

        public static int GetColumnFromLineOffset(this TextLine line, int lineOffset, int tabSize)
            => line.ToString().GetColumnFromLineOffset(lineOffset, tabSize);

        public static int GetLineOffsetFromColumn(this TextLine line, int column, int tabSize)
            => line.ToString().GetLineOffsetFromColumn(column, tabSize);
    }
}
