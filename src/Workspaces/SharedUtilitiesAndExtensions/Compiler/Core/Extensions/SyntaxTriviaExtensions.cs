// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class SyntaxTriviaExtensions
{
    public static int Width(this SyntaxTrivia trivia)
        => trivia.Span.Length;

    public static int FullWidth(this SyntaxTrivia trivia)
        => trivia.FullSpan.Length;

    public static bool IsElastic(this SyntaxTrivia trivia)
        => trivia.HasAnnotation(SyntaxAnnotation.ElasticAnnotation);

    public static SyntaxTrivia AsElastic(this SyntaxTrivia trivia)
        => trivia.WithAdditionalAnnotations(SyntaxAnnotation.ElasticAnnotation);
}
