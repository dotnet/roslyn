// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.FileHeaders;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Helper class used for working with file headers.
    /// </summary>
    internal static class FileHeaderHelpers
    {
        /// <summary>
        /// Parses a comment-only file header.
        /// </summary>
        /// <param name="root">The root of the syntax tree.</param>
        /// <returns>The copyright string, as parsed from the file header.</returns>
        internal static FileHeader ParseFileHeader(SyntaxNode root)
        {
            var firstToken = root.GetFirstToken(includeZeroWidth: true);
            var firstNonWhitespaceTrivia = TriviaHelper.IndexOfFirstNonWhitespaceTrivia(firstToken.LeadingTrivia, true);

            if (firstNonWhitespaceTrivia == -1)
            {
                return FileHeader.MissingFileHeader(0);
            }

            var sb = StringBuilderPool.Allocate();
            var endOfLineCount = 0;
            var done = false;
            var missingHeaderOffset = 0;
            var fileHeaderStart = int.MaxValue;
            var fileHeaderEnd = int.MinValue;

            for (var i = firstNonWhitespaceTrivia; !done && (i < firstToken.LeadingTrivia.Count); i++)
            {
                var trivia = firstToken.LeadingTrivia[i];

                switch (trivia.Kind())
                {
                    case SyntaxKind.WhitespaceTrivia:
                        endOfLineCount = 0;
                        break;
                    case SyntaxKind.SingleLineCommentTrivia:
                        endOfLineCount = 0;

                        var commentString = trivia.ToFullString();

                        fileHeaderStart = Math.Min(trivia.FullSpan.Start, fileHeaderStart);
                        fileHeaderEnd = trivia.FullSpan.End;

                        sb.AppendLine(commentString.Substring(2).Trim());
                        break;
                    case SyntaxKind.MultiLineCommentTrivia:
                        // only process a MultiLineCommentTrivia if no SingleLineCommentTrivia have been processed
                        if (sb.Length == 0)
                        {
                            var triviaString = trivia.ToFullString();

                            var startIndex = triviaString.IndexOf("/*", StringComparison.Ordinal) + 2;
                            var endIndex = triviaString.LastIndexOf("*/", StringComparison.Ordinal);
                            if (endIndex == -1)
                            {
                                // While editing, it is possible to have a multiline comment trivia that does not contain the closing '*/' yet.
                                return FileHeader.MissingFileHeader(missingHeaderOffset);
                            }

                            var commentContext = triviaString.Substring(startIndex, endIndex - startIndex).Trim();

                            var triviaStringParts = commentContext.Replace("\r\n", "\n").Split('\n');

                            foreach (var part in triviaStringParts)
                            {
                                var trimmedPart = part.TrimStart(' ', '*');
                                sb.AppendLine(trimmedPart);
                            }

                            fileHeaderStart = trivia.FullSpan.Start;
                            fileHeaderEnd = trivia.FullSpan.End;
                        }

                        done = true;
                        break;
                    case SyntaxKind.EndOfLineTrivia:
                        endOfLineCount++;
                        done = endOfLineCount > 1;
                        break;
                    default:
                        if (trivia.IsDirective)
                        {
                            missingHeaderOffset = trivia.FullSpan.End;
                        }

                        done = (fileHeaderStart < fileHeaderEnd) || !trivia.IsDirective;
                        break;
                }
            }

            if (fileHeaderStart > fileHeaderEnd)
            {
                StringBuilderPool.Free(sb);
                return FileHeader.MissingFileHeader(missingHeaderOffset);
            }

            if (sb.Length > 0)
            {
                // remove the final newline
                var eolLength = Environment.NewLine.Length;
                sb.Remove(sb.Length - eolLength, eolLength);
            }

            return new FileHeader(StringBuilderPool.ReturnAndFree(sb), fileHeaderStart, fileHeaderEnd, commentPrefixLength: 2);
        }
    }
}
