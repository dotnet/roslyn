// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        public static SyntaxNode WithPrependedNonIndentationTriviaFrom(
            this SyntaxNode to, SyntaxNode from)
        {
            // get all the preceding trivia from the 'from' node, not counting the leading
            // indentation trivia is has.
            var finalTrivia = from.GetLeadingTrivia().ToList();
            while (finalTrivia.Count > 0 && finalTrivia.Last().Kind() == SyntaxKind.WhitespaceTrivia)
            {
                finalTrivia.RemoveAt(finalTrivia.Count - 1);
            }

            // Also, add on the trailing trivia if there are trailing comments.
            var hasTrailingComments = from.GetTrailingTrivia().Any(t => t.IsRegularComment());
            if (hasTrailingComments)
            {
                finalTrivia.AddRange(from.GetTrailingTrivia());
            }

            // Merge this trivia with the existing trivia on the node.  Format in case
            // we added comments and need them indented properly.
            return to.WithPrependedLeadingTrivia(finalTrivia)
                     .WithAdditionalAnnotations(Formatter.Annotation);
        }
    }
}
