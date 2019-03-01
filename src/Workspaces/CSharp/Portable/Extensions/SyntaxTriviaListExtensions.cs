// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class SyntaxTriviaListExtensions
    {
        public static bool Any(this SyntaxTriviaList triviaList, params SyntaxKind[] kinds)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.MatchesKind(kinds))
                {
                    return true;
                }
            }

            return false;
        }

        public static SyntaxTrivia? GetFirstNewLine(this SyntaxTriviaList triviaList)
        {
            return triviaList
                .Where(t => t.Kind() == SyntaxKind.EndOfLineTrivia)
                .FirstOrNullable();
        }

        public static SyntaxTrivia? GetLastComment(this SyntaxTriviaList triviaList)
        {
            return triviaList
                .Where(t => t.IsRegularComment())
                .LastOrNullable();
        }

        public static SyntaxTrivia? GetLastCommentOrWhitespace(this SyntaxTriviaList triviaList)
        {
            return triviaList
                .Where(t => t.MatchesKind(SyntaxKind.SingleLineCommentTrivia, SyntaxKind.MultiLineCommentTrivia, SyntaxKind.WhitespaceTrivia))
                .LastOrNullable();
        }

        public static IEnumerable<SyntaxTrivia> SkipInitialWhitespace(this SyntaxTriviaList triviaList)
        {
            return triviaList.SkipWhile(t => t.Kind() == SyntaxKind.WhitespaceTrivia);
        }

        private static IEnumerable<List<SyntaxTrivia>> GetLines(this SyntaxTriviaList triviaList)
        {
            var currentLine = new List<SyntaxTrivia>();
            foreach (var trivia in triviaList)
            {
                currentLine.Add(trivia);
                if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    yield return currentLine;
                    currentLine = new List<SyntaxTrivia>();
                }
            }

            if (currentLine.Count > 0)
            {
                yield return currentLine;
            }
        }

        public static IEnumerable<SyntaxTrivia> SkipInitialWhiteLines(this SyntaxTriviaList triviaList)
        {
            return triviaList.GetLines()
                .SkipWhile(l => l.All(t =>
                    t.Kind() == SyntaxKind.EndOfLineTrivia ||
                    t.Kind() == SyntaxKind.WhitespaceTrivia))
                .SelectMany(l => l);
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
