// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class SyntaxTreeExtensions
    {
        public static bool OverlapsHiddenPosition(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken)
        {
            if (tree == null)
            {
                return false;
            }

            var text = tree.GetText(cancellationToken);

            return text.OverlapsHiddenPosition(span, (position, cancellationToken2) =>
                {
                    // implements the ASP.NET IsHidden rule
                    var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
                    return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
                },
                cancellationToken);
        }

        internal static SyntaxTrivia FindTriviaAndAdjustForEndOfFile(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, bool findInsideTrivia = false)
        {
            var root = syntaxTree.GetRoot(cancellationToken);
            var trivia = root.FindTrivia(position, findInsideTrivia);

            // If we ask right at the end of the file, we'll get back nothing.
            // We handle that case specially for now, though SyntaxTree.FindTrivia should
            // work at the end of a file.
            if (position == root.FullWidth())
            {
                var compilationUnit = (ICompilationUnitSyntax)root;
                var endOfFileToken = compilationUnit.EndOfFileToken;
                if (endOfFileToken.HasLeadingTrivia)
                {
                    trivia = endOfFileToken.LeadingTrivia.Last();
                }
                else
                {
                    var token = endOfFileToken.GetPreviousToken(includeSkipped: true);
                    if (token.HasTrailingTrivia)
                    {
                        trivia = token.TrailingTrivia.Last();
                    }
                }
            }

            return trivia;
        }

        public static SyntaxToken FindTokenOrEndToken(
            this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            Debug.Assert(syntaxTree != null);

            var root = syntaxTree.GetRoot(cancellationToken);
            var result = root.FindToken(position, findInsideTrivia: true);
            if (result.RawKind != 0)
            {
                return result;
            }

            // Special cases.  See if we're actually at the end of a:
            // a) doc comment
            // b) pp directive
            // c) file

            var compilationUnit = (ICompilationUnitSyntax)root;
            var triviaList = compilationUnit.EndOfFileToken.LeadingTrivia;
            foreach (var trivia in triviaList.Reverse())
            {
                if (trivia.HasStructure)
                {
                    var token = trivia.GetStructure().GetLastToken(includeZeroWidth: true);
                    if (token.Span.End == position)
                    {
                        return token;
                    }
                }
            }

            if (position == root.FullSpan.End)
            {
                return compilationUnit.EndOfFileToken;
            }

            return default;
        }

        /// <summary>
        /// If the position is inside of token, return that token; otherwise, return the token to the left.
        /// </summary>
        public static SyntaxToken FindTokenOnLeftOfPosition(
            this SyntaxTree syntaxTree,
            int position,
            CancellationToken cancellationToken,
            bool includeSkipped = true,
            bool includeDirectives = false,
            bool includeDocumentationComments = false)
        {
            return syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(
                position, includeSkipped, includeDirectives, includeDocumentationComments);
        }
    }
}
