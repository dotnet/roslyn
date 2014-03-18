// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CommonSyntaxTriviaExtensions
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
    }
}