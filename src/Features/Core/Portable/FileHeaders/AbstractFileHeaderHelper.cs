// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderHelper
    {
        protected AbstractFileHeaderHelper(ISyntaxKinds syntaxKinds)
        {
            SingleLineCommentTriviaKind = syntaxKinds.SingleLineCommentTrivia;
            MultiLineCommentTriviaKind = syntaxKinds.MultiLineCommentTrivia;
            WhitespaceTriviaKind = syntaxKinds.WhitespaceTrivia;
            EndOfLineTriviaKind = syntaxKinds.EndOfLineTrivia;
        }

        public abstract string CommentPrefix { get; }

        private int SingleLineCommentTriviaKind { get; }
        private int MultiLineCommentTriviaKind { get; }
        private int WhitespaceTriviaKind { get; }
        private int EndOfLineTriviaKind { get; }

        protected abstract string GetTextContextOfComment(SyntaxTrivia commentTrivia);

        public FileHeader ParseFileHeader(SyntaxNode root)
        {
            var firstToken = root.GetFirstToken(includeZeroWidth: true);
            var firstNonWhitespaceTrivia = IndexOfFirstNonWhitespaceTrivia(firstToken.LeadingTrivia, true);

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

                if (trivia.RawKind == WhitespaceTriviaKind)
                {
                    endOfLineCount = 0;
                }
                else if (trivia.RawKind == SingleLineCommentTriviaKind)
                {
                    endOfLineCount = 0;

                    var commentText = GetTextContextOfComment(trivia);

                    fileHeaderStart = Math.Min(trivia.FullSpan.Start, fileHeaderStart);
                    fileHeaderEnd = trivia.FullSpan.End;

                    sb.AppendLine(commentText.Trim());
                }
                else if (trivia.RawKind == MultiLineCommentTriviaKind)
                {
                    // only process a MultiLineCommentTrivia if no SingleLineCommentTrivia have been processed
                    if (sb.Length == 0)
                    {
                        var commentText = GetTextContextOfComment(trivia);
                        var triviaStringParts = commentText.Trim().Replace("\r\n", "\n").Split('\n');

                        foreach (var part in triviaStringParts)
                        {
                            var trimmedPart = part.TrimStart(' ', '*');
                            sb.AppendLine(trimmedPart);
                        }

                        fileHeaderStart = trivia.FullSpan.Start;
                        fileHeaderEnd = trivia.FullSpan.End;
                    }

                    done = true;
                }
                else if (trivia.RawKind == EndOfLineTriviaKind)
                {
                    endOfLineCount++;
                    done = endOfLineCount > 1;
                }
                else
                {
                    if (trivia.IsDirective)
                    {
                        missingHeaderOffset = trivia.FullSpan.End;
                    }

                    done = (fileHeaderStart < fileHeaderEnd) || !trivia.IsDirective;
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

            return new FileHeader(StringBuilderPool.ReturnAndFree(sb), fileHeaderStart, fileHeaderEnd, CommentPrefix.Length);
        }

        /// <summary>
        /// Returns the index of the first non-whitespace trivia in the given trivia list.
        /// </summary>
        /// <param name="triviaList">The trivia list to process.</param>
        /// <param name="endOfLineIsWhitespace"><see langword="true"/> to treat <see cref="EndOfLineTriviaKind"/>
        /// as whitespace; otherwise, <see langword="false"/>.</param>
        /// <typeparam name="T">The type of the trivia list.</typeparam>
        /// <returns>The index where the non-whitespace starts, or -1 if there is no non-whitespace trivia.</returns>
        private int IndexOfFirstNonWhitespaceTrivia<T>(T triviaList, bool endOfLineIsWhitespace = true)
            where T : IReadOnlyList<SyntaxTrivia>
        {
            for (var index = 0; index < triviaList.Count; index++)
            {
                var currentTrivia = triviaList[index];
                if (currentTrivia.RawKind == EndOfLineTriviaKind)
                {
                    if (!endOfLineIsWhitespace)
                    {
                        return index;
                    }
                }
                else if (currentTrivia.RawKind != WhitespaceTriviaKind)
                {
                    // encountered non-whitespace trivia -> the search is done.
                    return index;
                }
            }

            return -1;
        }
    }
}
