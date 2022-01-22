// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    /// <summary>
    /// Contains the parsed file header information for a syntax tree.
    /// </summary>
    internal readonly struct FileHeader
    {
        /// <summary>
        /// The location in the source where the file header was expected to start.
        /// </summary>
        private readonly int _fileHeaderStart;

        /// <summary>
        /// The length of the prefix indicating the start of a comment. For example:
        /// <list type="bullet">
        ///   <item>
        ///     <term>C#</term>
        ///     <description>2, for the length of <c>//</c>.</description>
        ///   </item>
        ///   <item>
        ///     <term>Visual Basic</term>
        ///     <description>1, for the length of <c>'</c>.</description>
        ///   </item>
        /// </list>
        /// </summary>
        private readonly int _commentPrefixLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeader"/> struct.
        /// </summary>
        /// <param name="copyrightText">The copyright string, as parsed from the header.</param>
        /// <param name="fileHeaderStart">The offset within the file at which the header started.</param>
        /// <param name="fileHeaderEnd">The offset within the file at which the header ended.</param>
        internal FileHeader(string copyrightText, int fileHeaderStart, int fileHeaderEnd, int commentPrefixLength)
            : this(fileHeaderStart, isMissing: false)
        {
            // Currently unused
            _ = fileHeaderEnd;

            CopyrightText = copyrightText;
            _commentPrefixLength = commentPrefixLength;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileHeader"/> struct.
        /// </summary>
        /// <param name="fileHeaderStart">The offset within the file at which the header started, or was expected to start.</param>
        /// <param name="isMissing"><see langword="true"/> if the file header is missing; otherwise, <see langword="false"/>.</param>
        private FileHeader(int fileHeaderStart, bool isMissing)
        {
            _fileHeaderStart = fileHeaderStart;
            _commentPrefixLength = 0;

            IsMissing = isMissing;
            CopyrightText = "";
        }

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
        /// Gets a <see cref="FileHeader"/> instance representing a missing file header starting at the specified
        /// position.
        /// </summary>
        /// <param name="fileHeaderStart">The location at which a file header was expected. This will typically be the
        /// start of the first line after any directive trivia (<see cref="SyntaxTrivia.IsDirective"/>) to account for
        /// source suppressions.</param>
        /// <returns>
        /// A <see cref="FileHeader"/> instance representing a missing file header.
        /// </returns>
        internal static FileHeader MissingFileHeader(int fileHeaderStart)
            => new(fileHeaderStart, isMissing: true);

        /// <summary>
        /// Gets the location representing the start of the file header.
        /// </summary>
        /// <param name="syntaxTree">The syntax tree to use for generating the location.</param>
        /// <returns>The location representing the start of the file header.</returns>
        internal Location GetLocation(SyntaxTree syntaxTree)
        {
            if (IsMissing)
            {
                return Location.Create(syntaxTree, new TextSpan(_fileHeaderStart, 0));
            }

            return Location.Create(syntaxTree, new TextSpan(_fileHeaderStart, _commentPrefixLength));
        }
    }
}
