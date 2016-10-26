// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Formatting
{
    internal struct LineColumn
    {
        public static LineColumn Default = new LineColumn { Line = 0, Column = 0, WhitespaceOnly = true };

        // absolute line number from first token
        public int Line { get; private set; }

        // absolute column from beginning of a line
        public int Column { get; private set; }

        // there is only whitespace on this line
        public bool WhitespaceOnly { get; private set; }

        public LineColumn(int line, int column, bool whitespaceOnly)
            : this()
        {
            this.Line = line;
            this.Column = column;
            this.WhitespaceOnly = whitespaceOnly;
        }

        public LineColumn With(LineColumnDelta delta)
        {
            if (delta.Lines <= 0)
            {
                return new LineColumn
                {
                    Line = this.Line,
                    Column = this.Column + delta.Spaces,
                    WhitespaceOnly = this.WhitespaceOnly && delta.WhitespaceOnly
                };
            }

            return new LineColumn
            {
                Line = this.Line + delta.Lines,
                Column = delta.Spaces,
                WhitespaceOnly = delta.WhitespaceOnly
            };
        }
    }
}
