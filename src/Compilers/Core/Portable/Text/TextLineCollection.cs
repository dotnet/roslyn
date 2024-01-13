// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
            if (position.Line >= this.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(position.Line), string.Format(CodeAnalysisResources.LineCannotBeGreaterThanEnd, position.Line, this.Count));
            }

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

        [SuppressMessage("Performance", "CA1067", Justification = "Equality not actually implemented")]
        public struct Enumerator : IEnumerator<TextLine>, IEnumerator
        {
            private readonly TextLineCollection _lines;
            private int _index;

            internal Enumerator(TextLineCollection lines, int index = -1)
            {
                _lines = lines;
                _index = index;
            }

            public TextLine Current
            {
                get
                {
                    var ndx = _index;
                    if (ndx >= 0 && ndx < _lines.Count)
                    {
                        return _lines[ndx];
                    }
                    else
                    {
                        return default(TextLine);
                    }
                }
            }

            public bool MoveNext()
            {
                if (_index < _lines.Count - 1)
                {
                    _index = _index + 1;
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

            public override bool Equals(object? obj)
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
