//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Represents an immutable snapshot of text.
    /// </summary>
    public interface IText
    {
        /// <summary>
        /// The container for the text.
        /// </summary>
        ITextContainer Container { get; }

        /// <summary>
        /// Total number of characters in the text source.
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Total number of lines in the text.
        /// </summary>
        int LineCount { get; }

        /// <summary>
        /// Returns the collection of line information for the <see cref="T:IText"/> instance.
        /// </summary>
        IEnumerable<ITextLine> Lines { get; }

        /// <summary>
        /// Return the char at position in the IText.
        /// </summary>
        char this[int position] { get; }

        /// <summary>
        /// Gets the line corresponding to the provided line number.
        /// </summary>
        ITextLine GetLineFromLineNumber(int lineNumber);

        /// <summary>
        /// Gets the line which encompasses the provided position.
        /// </summary>
        ITextLine GetLineFromPosition(int position);

        /// <summary>
        /// Gets the number of the line that contains the character at the specified position.
        /// </summary>
        int GetLineNumberFromPosition(int position);

        /// <summary>
        /// Gets a line number, and position within that line, for the character at the 
        /// specified position
        /// </summary>
        LinePosition GetLinePosition(int position);

        /// <summary>
        /// Returns a string representation of the contents of this IText.
        /// </summary>
        string ToString();

        /// <summary>
        /// Returns a string representation of the contents of this IText within the given span.
        /// </summary>
        string ToString(TextSpan span);

        /// <summary>
        /// Gets the a new IText that corresponds to the contents of this IText for the given span.
        /// </summary>
        IText GetSubText(TextSpan span);

        /// <summary>
        /// Copy the count contents of IText starting from sourceIndex to destination starting at
        /// destinationIndex. Parameter validation behavior should follow that of <see cref="System.String.CopyTo"/>
        /// </summary>
        void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count);

        /// <summary>
        /// Write the text to the specified TextWriter.
        /// </summary>
        void Write(TextWriter textWriter);

        /// <summary>
        /// Gets the set of TextChangeRanges that describe how the text changed between this text and
        /// the old version. Some texts keep track of changes between themselves and previous instances
        /// and may report detailed changes. Others many simply report a single change encompassing the
        /// entire text.
        /// </summary>
        IList<TextChangeRange> GetChangeRanges(IText oldText);

        /// <summary>
        /// Construct a new IText with the specified changes.
        /// The changes must be ordered and not overlapping.
        /// </summary>
        IText WithChanges(IEnumerable<TextChange> changes);
    }
}