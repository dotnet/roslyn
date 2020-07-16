// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct LineColumn
    {
        public static LineColumn Default = new LineColumn(line: 0, column: 0, whitespaceOnly: true);

        /// <summary>
        /// absolute line number from first token
        /// </summary>
        public readonly int Line;

        /// <summary>
        /// absolute column from beginning of a line
        /// </summary>
        public readonly int Column;

        /// <summary>
        /// there is only whitespace on this line
        /// </summary>
        public readonly bool WhitespaceOnly;

        public LineColumn(int line, int column, bool whitespaceOnly)
        {
            Line = line;
            Column = column;
            WhitespaceOnly = whitespaceOnly;
        }

        public LineColumn With(LineColumnDelta delta)
        {
            if (delta.Lines <= 0)
            {
                return new LineColumn(
                    Line,
                    Column + delta.Spaces,
                    WhitespaceOnly && delta.WhitespaceOnly);
            }

            return new LineColumn(
                Line + delta.Lines,
                delta.Spaces,
                delta.WhitespaceOnly);
        }
    }
}
