// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxTriviaListExtensions
    {
        public static SyntaxTrivia? GetFirstNewLine(this SyntaxTriviaList triviaList)
        {
            return triviaList
                .Where(t => t.Kind() == SyntaxKind.EndOfLineTrivia)
                .FirstOrNull();
        }

        public static SyntaxTrivia? GetLastComment(this SyntaxTriviaList triviaList)
        {
            return triviaList
                .Where(t => t.IsRegularComment())
                .LastOrNull();
        }

        public static SyntaxTrivia? GetLastCommentOrWhitespace(this SyntaxTriviaList triviaList)
        {
            if (triviaList.Count == 0)
                return null;

            return triviaList
                .Where(t => t is (kind: SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or SyntaxKind.WhitespaceTrivia))
                .LastOrNull();
        }

        public static IEnumerable<SyntaxTrivia> SkipInitialWhitespace(this SyntaxTriviaList triviaList)
            => triviaList.SkipWhile(t => t.Kind() == SyntaxKind.WhitespaceTrivia);

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

        public static SyntaxTriviaList WithoutLeadingBlankLines(this SyntaxTriviaList triviaList)
        {
            var triviaInLeadingBlankLines = GetLeadingBlankLines(triviaList).SelectMany(l => l);
            return new SyntaxTriviaList(triviaList.Skip(triviaInLeadingBlankLines.Count()));
        }

        /// <summary>
        /// Takes an INCLUSIVE range of trivia from the trivia list. 
        /// </summary>
        public static IEnumerable<SyntaxTrivia> TakeRange(this SyntaxTriviaList triviaList, int start, int end)
        {
            while (start <= end)
            {
                yield return triviaList[start++];
            }
        }
    }
}
