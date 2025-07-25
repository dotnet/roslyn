// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class SyntaxTriviaListExtensions
{
    extension(SyntaxTriviaList triviaList)
    {
        public SyntaxTrivia? GetFirstNewLine()
        {
            return triviaList
                .Where(t => t.Kind() == SyntaxKind.EndOfLineTrivia)
                .FirstOrNull();
        }

        public SyntaxTrivia? GetLastComment()
        {
            return triviaList
                .Where(t => t.IsRegularComment())
                .LastOrNull();
        }

        public SyntaxTrivia? GetLastCommentOrWhitespace()
        {
            if (triviaList.Count == 0)
                return null;

            return triviaList
                .Where(t => t is (kind: SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.WhitespaceTrivia))
                .LastOrNull();
        }

        public SyntaxTriviaList WithoutLeadingBlankLines()
        {
            var triviaInLeadingBlankLines = GetLeadingBlankLines(triviaList).SelectMany(l => l);
            return [.. triviaList.Skip(triviaInLeadingBlankLines.Count())];
        }

        /// <summary>
        /// Takes an INCLUSIVE range of trivia from the trivia list. 
        /// </summary>
        public IEnumerable<SyntaxTrivia> TakeRange(int start, int end)
        {
            while (start <= end)
            {
                yield return triviaList[start++];
            }
        }
    }

    extension(IEnumerable<SyntaxTrivia> triviaList)
    {
        public IEnumerable<SyntaxTrivia> SkipInitialWhitespace()
        => triviaList.SkipWhile(t => t.Kind() == SyntaxKind.WhitespaceTrivia);
    }

    private static ImmutableArray<ImmutableArray<SyntaxTrivia>> GetLeadingBlankLines(SyntaxTriviaList triviaList)
    {
        using var result = TemporaryArray<ImmutableArray<SyntaxTrivia>>.Empty;
        using var currentLine = TemporaryArray<SyntaxTrivia>.Empty;
        foreach (var trivia in triviaList)
        {
            currentLine.Add(trivia);
            if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
            {
                var currentLineIsBlank = currentLine.All(static t =>
                    t.Kind() is SyntaxKind.EndOfLineTrivia or
                    SyntaxKind.WhitespaceTrivia);
                if (!currentLineIsBlank)
                {
                    break;
                }

                result.Add(currentLine.ToImmutableAndClear());
            }
        }

        return result.ToImmutableAndClear();
    }
}
