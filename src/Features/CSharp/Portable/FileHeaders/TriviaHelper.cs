// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp.FileHeaders
{
    /// <summary>
    /// Provides helper methods to work with trivia (lists).
    /// </summary>
    internal static class TriviaHelper
    {
        /// <summary>
        /// Returns the index of the first non-whitespace trivia in the given trivia list.
        /// </summary>
        /// <param name="triviaList">The trivia list to process.</param>
        /// <param name="endOfLineIsWhitespace"><see langword="true"/> to treat <see cref="SyntaxKind.EndOfLineTrivia"/>
        /// as whitespace; otherwise, <see langword="false"/>.</param>
        /// <typeparam name="T">The type of the trivia list.</typeparam>
        /// <returns>The index where the non-whitespace starts, or -1 if there is no non-whitespace trivia.</returns>
        internal static int IndexOfFirstNonWhitespaceTrivia<T>(T triviaList, bool endOfLineIsWhitespace = true)
            where T : IReadOnlyList<SyntaxTrivia>
        {
            for (var index = 0; index < triviaList.Count; index++)
            {
                var currentTrivia = triviaList[index];
                switch (currentTrivia.Kind())
                {
                    case SyntaxKind.EndOfLineTrivia:
                        if (!endOfLineIsWhitespace)
                        {
                            return index;
                        }

                        break;

                    case SyntaxKind.WhitespaceTrivia:
                        break;

                    default:
                        // encountered non-whitespace trivia -> the search is done.
                        return index;
                }
            }

            return -1;
        }
    }
}
