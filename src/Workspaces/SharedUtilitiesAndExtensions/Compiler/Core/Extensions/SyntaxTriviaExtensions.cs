// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SyntaxTriviaExtensions
{
    extension(SyntaxTrivia trivia)
    {
        public int Width()
        => trivia.Span.Length;

        public int FullWidth()
            => trivia.FullSpan.Length;

        public bool IsElastic()
            => trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation);

        public SyntaxTrivia AsElastic()
            => trivia.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
    }
}
