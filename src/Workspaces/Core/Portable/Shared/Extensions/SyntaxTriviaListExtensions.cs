// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
