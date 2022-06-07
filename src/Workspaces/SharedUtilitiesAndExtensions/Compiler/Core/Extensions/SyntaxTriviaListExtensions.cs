// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxTriviaListExtensions
    {
        public static SyntaxTrivia? FirstOrNull(this SyntaxTriviaList triviaList, Func<SyntaxTrivia, bool> predicate)
        {
            foreach (var trivia in triviaList)
            {
                if (predicate(trivia))
                {
                    return trivia;
                }
            }

            return null;
        }

        public static SyntaxTrivia LastOrDefault(this SyntaxTriviaList triviaList)
            => triviaList.Any() ? triviaList.Last() : default;
    }
}
