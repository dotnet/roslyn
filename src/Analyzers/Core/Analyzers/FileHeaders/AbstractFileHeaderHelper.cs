// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.FileHeaders;

internal abstract class AbstractFileHeaderHelper
{
    protected AbstractFileHeaderHelper(ISyntaxKinds syntaxKinds)
    {
        SingleLineCommentTriviaKind = syntaxKinds.SingleLineCommentTrivia;
        MultiLineCommentTriviaKind = syntaxKinds.MultiLineCommentTrivia;
        WhitespaceTriviaKind = syntaxKinds.WhitespaceTrivia;
        EndOfLineTriviaKind = syntaxKinds.EndOfLineTrivia;
    }

    /// <summary>
    /// Gets the text prefix indicating a single-line comment.
    /// </summary>
    public abstract string CommentPrefix { get; }

    protected abstract ReadOnlyMemory<char> GetTextContextOfComment(SyntaxTrivia commentTrivia);

    /// <inheritdoc cref="ISyntaxKinds.SingleLineCommentTrivia"/>
    private int SingleLineCommentTriviaKind { get; }

    /// <inheritdoc cref="ISyntaxKinds.MultiLineCommentTrivia"/>
    private int? MultiLineCommentTriviaKind { get; }

    /// <inheritdoc cref="ISyntaxKinds.WhitespaceTrivia"/>
    private int WhitespaceTriviaKind { get; }

    /// <inheritdoc cref="ISyntaxKinds.EndOfLineTrivia"/>
    private int EndOfLineTriviaKind { get; }

    public FileHeader ParseFileHeader(SyntaxNode root)
    {
        var firstToken = root.GetFirstToken(includeZeroWidth: true);
        var firstNonWhitespaceTrivia = IndexOfFirstNonWhitespaceTrivia(firstToken.LeadingTrivia);

        if (firstNonWhitespaceTrivia == -1)
        {
            return FileHeader.MissingFileHeader(0);
        }

        using var _ = PooledStringBuilder.GetInstance(out var sb);
        var endOfLineCount = 0;
        var missingHeaderOffset = 0;
        var fileHeaderStart = int.MaxValue;
        var fileHeaderEnd = int.MinValue;

        for (var i = firstNonWhitespaceTrivia; i < firstToken.LeadingTrivia.Count; i++)
        {
            var trivia = firstToken.LeadingTrivia[i];

            if (trivia.RawKind == WhitespaceTriviaKind)
            {
                endOfLineCount = 0;
            }
            else if (trivia.RawKind == SingleLineCommentTriviaKind)
            {
                endOfLineCount = 0;

                var commentText = GetTextContextOfComment(trivia).Span.Trim();

                fileHeaderStart = Math.Min(trivia.FullSpan.Start, fileHeaderStart);
                fileHeaderEnd = trivia.FullSpan.End;

#if NETCOREAPP
                sb.Append(commentText).AppendLine();
#else
                sb.AppendLine(commentText.ToString());
#endif
            }
            else if (trivia.RawKind == MultiLineCommentTriviaKind)
            {
                // only process a MultiLineCommentTrivia if no SingleLineCommentTrivia have been processed
                if (sb.Length == 0)
                {
                    var commentText = GetTextContextOfComment(trivia);
                    var triviaStringParts = commentText.Span.Trim().ToString().Replace("\r\n", "\n").Split('\n');

                    foreach (var part in triviaStringParts)
                    {
                        var trimmedPart = part.TrimStart(' ', '*');
                        sb.AppendLine(trimmedPart);
                    }

                    fileHeaderStart = trivia.FullSpan.Start;
                    fileHeaderEnd = trivia.FullSpan.End;
                }

                break;
            }
            else if (trivia.RawKind == EndOfLineTriviaKind)
            {
                endOfLineCount++;
                if (endOfLineCount > 1)
                {
                    break;
                }
            }
            else
            {
                if (trivia.IsDirective)
                {
                    missingHeaderOffset = trivia.FullSpan.End;
                }

                if ((fileHeaderStart < fileHeaderEnd) || !trivia.IsDirective)
                {
                    break;
                }
            }
        }

        if (fileHeaderStart > fileHeaderEnd)
        {
            return FileHeader.MissingFileHeader(missingHeaderOffset);
        }

        if (sb.Length > 0)
        {
            // remove the final newline
            var eolLength = Environment.NewLine.Length;
            sb.Remove(sb.Length - eolLength, eolLength);
        }

        return new FileHeader(sb.ToString(), fileHeaderStart, fileHeaderEnd, CommentPrefix.Length);
    }

    /// <summary>
    /// Returns the index of the first non-whitespace trivia in the given trivia list.
    /// </summary>
    /// <param name="triviaList">The trivia list to process.</param>
    /// <typeparam name="T">The type of the trivia list.</typeparam>
    /// <returns>The index where the non-whitespace starts, or -1 if there is no non-whitespace trivia.</returns>
    private int IndexOfFirstNonWhitespaceTrivia<T>(T triviaList)
        where T : IReadOnlyList<SyntaxTrivia>
    {
        for (var index = 0; index < triviaList.Count; index++)
        {
            var currentTrivia = triviaList[index];
            if (currentTrivia.RawKind != EndOfLineTriviaKind
                && currentTrivia.RawKind != WhitespaceTriviaKind)
            {
                // encountered non-whitespace trivia -> the search is done.
                return index;
            }
        }

        return -1;
    }
}
