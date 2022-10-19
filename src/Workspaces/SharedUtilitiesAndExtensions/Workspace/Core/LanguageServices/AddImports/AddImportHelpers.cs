// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal static class AddImportHelpers
    {
        public static TRootSyntax MoveTrivia<TRootSyntax, TImportDirectiveSyntax>(
            ISyntaxFacts syntaxFacts,
            TRootSyntax root,
            SyntaxList<TImportDirectiveSyntax> existingImports,
            List<TImportDirectiveSyntax> newImports)
            where TRootSyntax : SyntaxNode
            where TImportDirectiveSyntax : SyntaxNode
        {
            if (existingImports.Count == 0)
            {
                // We don't have any existing usings. Move any trivia on the first token 
                // of the file to the first using.
                // 
                // Don't do this for doc comments, as they belong to the first element
                // already in the file (like a class) and we don't want it to move to
                // the using.
                var firstToken = root.GetFirstToken();

                // Remove the leading directives from the first token.
                var newFirstToken = firstToken.WithLeadingTrivia(
                    firstToken.LeadingTrivia.Where(t => IsDocCommentOrElastic(syntaxFacts, t)));

                root = root.ReplaceToken(firstToken, newFirstToken);

                // Move the leading trivia from the first token to the first using.
                var newFirstUsing = newImports[0].WithLeadingTrivia(
                    firstToken.LeadingTrivia.Where(t => !IsDocCommentOrElastic(syntaxFacts, t)));
                newImports[0] = newFirstUsing;
            }
            else
            {
                var originalFirstUsing = existingImports[0];
                if (newImports[0] != originalFirstUsing)
                {
                    // We added a new first-using.  Take the trivia on the existing first using
                    // And move it to the new using.
                    var originalFirstUsingCurrentIndex = newImports.IndexOf(originalFirstUsing);

                    newImports[0] = newImports[0].WithLeadingTrivia(originalFirstUsing.GetLeadingTrivia());

                    var trailingTrivia = newImports[0].GetTrailingTrivia();
                    if (!syntaxFacts.IsEndOfLineTrivia(trailingTrivia.Count == 0 ? default : trailingTrivia[^1]))
                    {
                        newImports[0] = newImports[0].WithAppendedTrailingTrivia(syntaxFacts.ElasticCarriageReturnLineFeed);
                    }

                    newImports[originalFirstUsingCurrentIndex] = originalFirstUsing.WithoutLeadingTrivia();
                }
            }

            return root;
        }

        private static bool IsDocCommentOrElastic(ISyntaxFacts syntaxFacts, SyntaxTrivia t)
            => syntaxFacts.IsDocumentationComment(t) || syntaxFacts.IsElastic(t);
    }
}
