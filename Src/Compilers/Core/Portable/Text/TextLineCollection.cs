// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Abstract base class for <see cref="TextLine"/> collections.
    /// </summary>
    public abstract class TextLineCollection : IReadOnlyList<TextLine>
    {
        /// <summary>
        /// The count of <see cref="TextLine"/> items in the collection
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Gets the <see cref="TextLine"/> item at the specified index.
        /// </summary>
        public abstract TextLine this[int index] { get; }

        /// <summary>
        /// The index of the TextLine that encompasses the character position.
        /// </summary>
        public abstract int IndexOf(int position);

        /// <summary>
        /// Gets a <see cref="TextLine"/> that encompasses the character position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public virtual TextLine GetLineFromPosition(int position)
        {
            return this[this.IndexOf(position)];
        }

        /// <summary>
        /// Gets a <see cref="LinePosition"/> corresponding to a character position.
        /// </summary>
        public virtual LinePosition GetLinePosition(int position)
        {
            var line = GetLineFromPosition(position);
            return new LinePosition(line.LineNumber, position - line.Start);
        }

        /// <summary>
        /// Convert a <see cref="TextSpan"/> to a <see cref="LinePositionSpan"/>.
        /// </summary>
        public LinePositionSpan GetLinePositionSpan(TextSpan span)
        {
            return new LinePositionSpan(GetLinePosition(span.Start), GetLinePosition(span.End));
        }

        /// <summary>
        /// Convert a <see cref="LinePosition"/> to a position.
        /// </summary>
        public int GetPosition(LinePosition position)
        {
            return this[position.Line].Start + position.Character;
        }

        /// <summary>
        /// Convert a <see cref="LinePositionSpan"/> to <see cref="TextSpan"/>.
        /// </summary>
        public TextSpan GetTextSpan(LinePositionSpan span)
        {
            return TextSpan.FromBounds(GetPosition(span.Start), GetPosition(span.End));
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<TextLine> IEnumerable<TextLine>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public struct Enumerator : IEnumerator<TextLine>, IEnumerator
        {
            private readonly TextLineCollection lines;
            private int index;

            internal Enumerator(TextLineCollection lines, int index = -1)
            {
                this.lines = lines;
                this.index = index;
            }

            public TextLine Current
            {
                get
                {
                    var ndx = this.index;
                    if (ndx >= 0 && ndx < this.lines.Count)
                    {
                        return this.lines[ndx];
                    }
                    else
                    {
                        return default(TextLine);
                    }
                }
            }

            public bool MoveNext()
            {
                if (index < lines.Count - 1)
                {
                    index = index + 1;
                    return true;
                }

                return false;
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            bool IEnumerator.MoveNext()
            {
                return this.MoveNext();
            }

            void IEnumerator.Reset()
            {
            }

            void IDisposable.Dispose()
            {
            }

            public override bool Equals(object obj)
            {
                throw new NotSupportedException();
            }

            public override int GetHashCode()
            {
                throw new NotSupportedException();
            }
        }
    }
}