// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class UnprocessedDocumentationCommentFinder : CSharpSyntaxWalker
    {
        private readonly DiagnosticBag _diagnostics;
        private readonly CancellationToken _cancellationToken;
        private readonly TextSpan? _filterSpanWithinTree;

        private bool _isValidLocation;

        private UnprocessedDocumentationCommentFinder(DiagnosticBag diagnostics, TextSpan? filterSpanWithinTree, CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.Trivia)
        {
            _diagnostics = diagnostics;
            _filterSpanWithinTree = filterSpanWithinTree;
            _cancellationToken = cancellationToken;
        }

        public static void ReportUnprocessed(SyntaxTree tree, TextSpan? filterSpanWithinTree, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            if (tree.ReportDocumentationCommentDiagnostics())
            {
                UnprocessedDocumentationCommentFinder finder = new UnprocessedDocumentationCommentFinder(diagnostics, filterSpanWithinTree, cancellationToken);
                finder.Visit(tree.GetRoot(cancellationToken));
            }
        }

        private bool IsSyntacticallyFilteredOut(TextSpan fullSpan)
        {
            return _filterSpanWithinTree.HasValue && !_filterSpanWithinTree.Value.Contains(fullSpan);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (IsSyntacticallyFilteredOut(node.FullSpan))
            {
                return;
            }

            // Short-circuit traversal if we know there are no documentation comments below.
            // This should prevent us from descending into method bodies in correct programs.
            if (!node.HasStructuredTrivia)
            {
                if (node.Span.Length > 0)
                {
                    _isValidLocation = false; //would have seen a token
                }
                return;
            }

            if (node is BaseTypeDeclarationSyntax ||
                node is DelegateDeclarationSyntax ||
                node is EnumMemberDeclarationSyntax ||
                node is BaseMethodDeclarationSyntax ||
                node is BasePropertyDeclarationSyntax || //includes EventDeclarationSyntax
                node is BaseFieldDeclarationSyntax) //includes EventFieldDeclarationSyntax
            {
                // Will be cleared the next time we visit a token,
                // after the leading trivia, if there is any.
                _isValidLocation = true;
            }

            base.DefaultVisit(node);
        }

        public override void VisitLeadingTrivia(SyntaxToken token)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (IsSyntacticallyFilteredOut(token.FullSpan))
            {
                return;
            }

            base.VisitLeadingTrivia(token);
            _isValidLocation = false;
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (IsSyntacticallyFilteredOut(trivia.FullSpan))
            {
                return;
            }

            if (!_isValidLocation && SyntaxFacts.IsDocumentationCommentTrivia(trivia.Kind()))
            {
                int start = trivia.Position; // FullSpan start to include /** or ///
                const int length = 1; //Match dev11: span is just one character
                _diagnostics.Add(ErrorCode.WRN_UnprocessedXMLComment, new SourceLocation(trivia.SyntaxTree, new TextSpan(start, length)));
            }
            base.VisitTrivia(trivia);
        }
    }
}
