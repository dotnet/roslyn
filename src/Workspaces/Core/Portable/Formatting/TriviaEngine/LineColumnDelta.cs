// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.Formatting
{
    internal struct LineColumnDelta
    {
        public static LineColumnDelta Default = new LineColumnDelta(lines: 0, spaces: 0, whitespaceOnly: true, forceUpdate: false);

        // relative line number between calls
        public int Lines { get; private set; }

        // relative spaces between calls
        public int Spaces { get; private set; }

        // there is only whitespace in this space
        public bool WhitespaceOnly { get; private set; }

        // force text change regardless line and space changes
        public bool ForceUpdate { get; private set; }

        public LineColumnDelta(int lines, int spaces) : this()
        {
            this.Lines = lines;
            this.Spaces = spaces;

            this.WhitespaceOnly = true;
            this.ForceUpdate = false;
        }

        public LineColumnDelta(int lines, int spaces, bool whitespaceOnly)
            : this(lines, spaces)
        {
            this.WhitespaceOnly = whitespaceOnly;
            this.ForceUpdate = false;
        }

        public LineColumnDelta(int lines, int spaces, bool whitespaceOnly, bool forceUpdate)
            : this(lines, spaces, whitespaceOnly)
        {
            this.ForceUpdate = forceUpdate;
        }

        internal LineColumnDelta With(LineColumnDelta delta)
        {
            if (delta.Lines <= 0)
            {
                return new LineColumnDelta
                {
                    Lines = this.Lines,
                    Spaces = this.Spaces + delta.Spaces,
                    WhitespaceOnly = this.WhitespaceOnly && delta.WhitespaceOnly,
                    ForceUpdate = this.ForceUpdate || delta.ForceUpdate
                };
            }

            return new LineColumnDelta
            {
                Lines = this.Lines + delta.Lines,
                Spaces = delta.Spaces,
                WhitespaceOnly = delta.WhitespaceOnly,
                ForceUpdate = this.ForceUpdate || delta.ForceUpdate || (this.Spaces > 0)
            };
        }
    }
}
