// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SyntaxTriviaExtensions
    {
        public static int Width(this SyntaxTrivia trivia)
        {
            return trivia.Span.Length;
        }

        public static int FullWidth(this SyntaxTrivia trivia)
        {
            return trivia.FullSpan.Length;
        }

        public static bool IsElastic(this SyntaxTrivia trivia)
        {
            return trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation);
        }

        public static SyntaxTrivia AsElastic(this SyntaxTrivia trivia)
            => trivia.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
    }
}
