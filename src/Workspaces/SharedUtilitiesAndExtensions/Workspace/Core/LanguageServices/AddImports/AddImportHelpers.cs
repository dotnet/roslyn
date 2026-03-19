// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal static class AddImportHelpers
{
    public static (TRootSyntax root, bool addBlankLine) MoveTrivia<TRootSyntax, TImportDirectiveSyntax>(
        ISyntaxFacts syntaxFacts,
        TRootSyntax root,
        SyntaxList<TImportDirectiveSyntax> existingImports,
        List<TImportDirectiveSyntax> newImports)
        where TRootSyntax : SyntaxNode
        where TImportDirectiveSyntax : SyntaxNode
    {
        var addBlankLine = false;
        if (existingImports.Count == 0)
        {
            // We add a blank line after a brand new using group.
            addBlankLine = newImports.Count > 0;

            // We don't have any existing usings. Move any trivia on the first token 
            // of the file to the first using.
            // 
            // Don't do this for doc comments, as they belong to the first element
            // already in the file (like a class) and we don't want it to move to
            // the using.
            var firstToken = root.GetFirstToken();
            var endOfLine = root.DescendantTrivia().FirstOrNull((trivia, syntaxFacts) => syntaxFacts.IsEndOfLineTrivia(trivia), syntaxFacts) ?? syntaxFacts.ElasticCarriageReturnLineFeed;

            // Remove the leading directives from the first token.
            var newFirstToken = firstToken.WithLeadingTrivia(
                firstToken.LeadingTrivia.Where(t => IsDocCommentOrElastic(syntaxFacts, t)));

            root = root.ReplaceToken(firstToken, newFirstToken);

            // Move the leading trivia from the first token to the first using.
            var newFirstUsing = newImports[0].WithLeadingTrivia(
                firstToken.LeadingTrivia.Where(t => !IsDocCommentOrElastic(syntaxFacts, t)));
            newImports[0] = newFirstUsing;

            for (var i = 0; i < newImports.Count; i++)
            {
                var trailingTrivia = newImports[i].GetTrailingTrivia();
                if (!trailingTrivia.Any() || !syntaxFacts.IsEndOfLineTrivia(trailingTrivia[^1]))
                {
                    newImports[i] = newImports[i].WithAppendedTrailingTrivia(endOfLine);
                }
            }
        }
        else
        {
            var originalFirstUsing = existingImports[0];
            var originalFirstUsingCurrentIndex = newImports.IndexOf(originalFirstUsing);
            var originalLastUsing = existingImports[^1];
            var originalLastUsingCurrentIndex = newImports.IndexOf(originalLastUsing);

            var originalFirstUsingTrailingTrivia = originalFirstUsing.GetTrailingTrivia();
            var originalFirstUsingLineEnding = originalFirstUsingTrailingTrivia.Any() && syntaxFacts.IsEndOfLineTrivia(originalFirstUsingTrailingTrivia[^1])
                ? originalFirstUsingTrailingTrivia[^1]
                : syntaxFacts.ElasticCarriageReturnLineFeed;

            if (originalFirstUsingCurrentIndex != 0)
            {
                // We added a new first-using.  Take the trivia on the existing first using
                // And move it to the new using.
                newImports[0] = newImports[0].WithLeadingTrivia(originalFirstUsing.GetLeadingTrivia());
                newImports[originalFirstUsingCurrentIndex] = originalFirstUsing.WithoutLeadingTrivia();
            }

            if (originalLastUsingCurrentIndex != newImports.Count - 1)
            {
                // We added a new last-using.  Take the trailing trivia on the existing last using
                // And move it to the new using.
                newImports[^1] = newImports[^1].WithTrailingTrivia(originalLastUsing.GetTrailingTrivia());
                newImports[originalLastUsingCurrentIndex] = originalLastUsing.WithoutTrailingTrivia();
            }

            for (var i = 0; i < newImports.Count; i++)
            {
                var trailingTrivia = newImports[i].GetTrailingTrivia();
                if (!trailingTrivia.Any() || !syntaxFacts.IsEndOfLineTrivia(trailingTrivia[^1]))
                {
                    newImports[i] = newImports[i].WithAppendedTrailingTrivia(originalFirstUsingLineEnding);
                }
            }
        }

        return (root, addBlankLine);
    }

    private static bool IsDocCommentOrElastic(ISyntaxFacts syntaxFacts, SyntaxTrivia t)
        => syntaxFacts.IsDocumentationComment(t) || syntaxFacts.IsElastic(t);
}
