// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Formatting
{
    internal readonly struct LineColumnDelta
    {
        public static LineColumnDelta Default = new LineColumnDelta(lines: 0, spaces: 0, whitespaceOnly: true, forceUpdate: false);

        /// <summary>
        /// relative line number between calls
        /// </summary>
        public readonly int Lines;

        /// <summary>
        /// relative spaces between calls
        /// </summary>
        public readonly int Spaces;

        /// <summary>
        /// there is only whitespace in this space
        /// </summary>
        public readonly bool WhitespaceOnly;

        /// <summary>
        /// force text change regardless line and space changes
        /// </summary>
        public readonly bool ForceUpdate;

        public LineColumnDelta(int lines, int spaces)
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
                return new LineColumnDelta(
                    Lines,
                    Spaces + delta.Spaces,
                    WhitespaceOnly && delta.WhitespaceOnly,
                    ForceUpdate || delta.ForceUpdate);
            }

            return new LineColumnDelta(
                Lines + delta.Lines,
                delta.Spaces,
                delta.WhitespaceOnly,
                ForceUpdate || delta.ForceUpdate || Spaces > 0);
        }
    }
}
