// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal class UnprocessedDocumentationCommentFinder : CSharpSyntaxWalker
    {
        private readonly DiagnosticBag diagnostics;
        private readonly CancellationToken cancellationToken;
        private readonly TextSpan? filterSpanWithinTree;

        private bool IsValidLocation;

        private UnprocessedDocumentationCommentFinder(DiagnosticBag diagnostics, TextSpan? filterSpanWithinTree, CancellationToken cancellationToken)
            : base(SyntaxWalkerDepth.Trivia)
        {
            this.diagnostics = diagnostics;
            this.filterSpanWithinTree = filterSpanWithinTree;
            this.cancellationToken = cancellationToken;
        }

        public static void ReportUnprocessed(SyntaxTree tree, TextSpan? filterSpanWithinTree, DiagnosticBag diagnostics, CancellationToken cancellationToken)
        {
            if (tree.ReportDocumentationCommentDiagnostics())
            {
                UnprocessedDocumentationCommentFinder finder = new UnprocessedDocumentationCommentFinder(diagnostics, filterSpanWithinTree, cancellationToken);
                finder.Visit(tree.GetRoot());
            }
        }

        private bool IsSyntacticallyFilteredOut(TextSpan fullSpan)
        {
            return filterSpanWithinTree.HasValue && !filterSpanWithinTree.Value.Contains(fullSpan);
        }

        public override void DefaultVisit(SyntaxNode node)
        {
            cancellationToken.ThrowIfCancellationRequested();

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
                    IsValidLocation = false; //would have seen a token
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
                IsValidLocation = true;
            }

            base.DefaultVisit(node);
        }

        public override void VisitLeadingTrivia(SyntaxToken token)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsSyntacticallyFilteredOut(token.FullSpan))
            {
                return;
            }

            base.VisitLeadingTrivia(token);
            IsValidLocation = false;
        }

        public override void VisitTrivia(SyntaxTrivia trivia)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsSyntacticallyFilteredOut(trivia.FullSpan))
            {
                return;
            }

            if (!IsValidLocation && SyntaxFacts.IsDocumentationCommentTrivia(trivia.CSharpKind()))
            {
                int start = trivia.Position; // FullSpan start to include /** or ///
                const int length = 1; //Match dev11: span is just one character
                diagnostics.Add(ErrorCode.WRN_UnprocessedXMLComment, new SourceLocation(trivia.SyntaxTree, new TextSpan(start, length)));
            }
            base.VisitTrivia(trivia);
        }
    }
}