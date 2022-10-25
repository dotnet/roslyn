// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using static Microsoft.CodeAnalysis.UseConditionalExpression.UseConditionalExpressionHelpers;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal static class UseConditionalExpressionCodeFixHelpers
    {
        public static readonly SyntaxAnnotation SpecializedFormattingAnnotation = new();

        public static SyntaxRemoveOptions GetRemoveOptions(
            ISyntaxFactsService syntaxFacts, SyntaxNode syntax)
        {
            var removeOptions = SyntaxGenerator.DefaultRemoveOptions;
            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetLeadingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepLeadingTrivia;
            }

            if (HasRegularCommentTrivia(syntaxFacts, syntax.GetTrailingTrivia()))
            {
                removeOptions |= SyntaxRemoveOptions.KeepTrailingTrivia;
            }

            return removeOptions;
        }
    }
}
