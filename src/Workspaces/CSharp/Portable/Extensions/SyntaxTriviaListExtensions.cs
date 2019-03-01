// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
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

        private static ImmutableArray<ImmutableArray<SyntaxTrivia>> BreakIntoLines(this SyntaxTriviaList triviaList)
        {
            var result = ArrayBuilder<ImmutableArray<SyntaxTrivia>>.GetInstance();
            var currentLine = ArrayBuilder<SyntaxTrivia>.GetInstance();
            foreach (var trivia in triviaList)
            {
                currentLine.Add(trivia);
                if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
                {
                    result.Add(currentLine.ToImmutableAndFree());
                    currentLine = ArrayBuilder<SyntaxTrivia>.GetInstance();
                }
            }

            if (currentLine.Count > 0)
            {
                result.Add(currentLine.ToImmutableAndFree());
            }

            return result.ToImmutableAndFree();
        }

        public static SyntaxTriviaList WithoutLeadingBlankLines(this SyntaxTriviaList triviaList)
        {
            return new SyntaxTriviaList(triviaList.BreakIntoLines()
                .SkipWhile(l => l.All(t =>
                    t.Kind() == SyntaxKind.EndOfLineTrivia ||
                    t.Kind() == SyntaxKind.WhitespaceTrivia))
                .SelectMany(l => l));
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
