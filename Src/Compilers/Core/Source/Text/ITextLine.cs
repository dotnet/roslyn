using Microsoft.CodeAnalysis.Text;
//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.CodeAnalysis.Text
{
    /// <summary>
    /// Immutable representation of a line in an IText instance.
    /// </summary>
    public interface ITextLine
    {
        /// <summary>
        /// Start of the line.
        /// </summary>
        int Start { get; }

        /// <summary>
        /// End of the line not including the line break.
        /// </summary>
        int End { get; }

        /// <summary>
        /// End of the line including the line break.
        /// </summary>
        int EndIncludingLineBreak { get; }

        /// <summary>
        /// Extent of the line not including the line break.
        /// </summary>
        TextSpan Extent { get; }

        /// <summary>
        /// Extent of the line including the line break.
        /// </summary>
        TextSpan ExtentIncludingLineBreak { get; }

        /// <summary>
        /// Gets the text of the line excluding the line break.
        /// </summary>
        string ToString();

        /// <summary>
        /// Gets the line number for this line.
        /// </summary>
        int LineNumber { get; }
    }
}