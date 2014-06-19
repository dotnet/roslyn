using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Roslyn.Utilities;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Implementation of IText that exposes a suffix of another IText.
    /// </summary>
    internal sealed class OffsetText : IText
    {
        private readonly IText underlying;
        private readonly int offset;

        public OffsetText(IText underlying, int offset)
        {
            Debug.Assert(offset > 0);
            this.underlying = underlying;
            this.offset = offset;
        }

        public ITextContainer Container
        {
            get { return underlying.Container; }
        }

        public int Length
        {
            get { return underlying.Length - offset; }
        }

        public char this[int position]
        {
            get { return underlying[position + offset]; }
        }

        public string GetText()
        {
            return underlying.GetText().Substring(offset);
        }

        public string GetText(TextSpan span)
        {
            TextSpan offsetSpan = new TextSpan(span.Start + offset, span.Length);
            return underlying.GetText(offsetSpan);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            underlying.CopyTo(sourceIndex + offset, destination, destinationIndex, count);
        }

        public void Write(TextWriter textWriter)
        {
            throw Contract.Unreachable;
        }

        public int LineCount
        {
            get { throw Contract.Unreachable; }
        }

        public IEnumerable<ITextLine> Lines
        {
            get { throw Contract.Unreachable; }
        }

        public ITextLine GetLineFromLineNumber(int lineNumber)
        {
            throw Contract.Unreachable;
        }

        public ITextLine GetLineFromPosition(int position)
        {
            throw Contract.Unreachable;
        }

        public int GetLineNumberFromPosition(int position)
        {
            throw Contract.Unreachable;
        }
    }
}