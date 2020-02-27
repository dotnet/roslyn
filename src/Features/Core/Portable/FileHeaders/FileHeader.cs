// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    /// <summary>
    /// Class containing the parsed file header information.
    /// </summary>
    internal sealed class FileHeader
    {
        private readonly int _fileHeaderStart;
        private readonly int _commentPrefixLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeader"/> class.
        /// </summary>
        /// <param name="copyrightText">The copyright string, as parsed from the header.</param>
        /// <param name="fileHeaderStart">The offset within the file at which the header started.</param>
        /// <param name="fileHeaderEnd">The offset within the file at which the header ended.</param>
        internal FileHeader(string copyrightText, int fileHeaderStart, int fileHeaderEnd, int commentPrefixLength)
            : this(isMissing: false)
        {
            // Currently unused
            _ = fileHeaderEnd;

            CopyrightText = copyrightText;
            _fileHeaderStart = fileHeaderStart;
            _commentPrefixLength = commentPrefixLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeader"/> class.
        /// </summary>
        private FileHeader(bool isMissing)
        {
            IsMissing = isMissing;
            CopyrightText = "";
        }

        /// <summary>
        /// Gets a <see cref="FileHeader"/> instance representing a missing file header.
        /// </summary>
        /// <value>
        /// A <see cref="FileHeader"/> instance representing a missing file header.
        /// </value>
        internal static FileHeader MissingFileHeader { get; } = new FileHeader(isMissing: true);

        /// <summary>
        /// Gets a value indicating whether the file header is missing.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the file header is missing; otherwise, <see langword="false"/>.
        /// </value>
        internal bool IsMissing { get; }

        /// <summary>
        /// Gets the copyright text, as parsed from the header.
        /// </summary>
        /// <value>
        /// The copyright text, as parsed from the header.
        /// </value>
        internal string CopyrightText { get; }

        /// <summary>
        /// Gets the location representing the start of the file header.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree to use for generating the location.</param>
        /// <returns>The location representing the start of the file header.</returns>
        internal Location GetLocation(SyntaxTree syntaxTree)
        {
            if (IsMissing)
            {
                return Location.Create(syntaxTree, new TextSpan(0, 0));
            }

            return Location.Create(syntaxTree, TextSpan.FromBounds(_fileHeaderStart, _fileHeaderStart + _commentPrefixLength));
        }
    }
}
