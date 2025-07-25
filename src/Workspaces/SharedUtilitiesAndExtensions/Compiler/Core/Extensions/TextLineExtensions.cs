// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class TextLineExtensions
{
    extension(TextLine line)
    {
        public int? GetLastNonWhitespacePosition()
        {
            var text = line.Text!;
            var startPosition = line.Start;

            for (var i = line.End - 1; i >= startPosition; i--)
            {
                if (!char.IsWhiteSpace(text[i]))
                    return i;
            }

            return null;
        }

        /// <summary>
        /// Returns the first non-whitespace position on the given line, or null if 
        /// the line is empty or contains only whitespace.
        /// </summary>
        public int? GetFirstNonWhitespacePosition()
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
        public int? GetFirstNonWhitespaceOffset()
        {
            var text = line.Text;
            if (text != null)
            {
                for (var i = line.Start; i < line.End; i++)
                {
                    if (!char.IsWhiteSpace(text[i]))
                        return i - line.Start;
                }
            }

            return null;
        }

        public string GetLeadingWhitespace()
            => line.ToString().GetLeadingWhitespace();

        /// <summary>
        /// Determines whether the specified line is empty or contains whitespace only.
        /// </summary>
        public bool IsEmptyOrWhitespace()
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

        public int GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(int tabSize)
            => line.ToString().GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);

        public int GetColumnFromLineOffset(int lineOffset, int tabSize)
            => line.ToString().GetColumnFromLineOffset(lineOffset, tabSize);

        public int GetLineOffsetFromColumn(int column, int tabSize)
            => line.ToString().GetLineOffsetFromColumn(column, tabSize);
    }
}
