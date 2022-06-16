// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SplitStringLiteral
{
    internal abstract partial class StringSplitter
    {
        protected static readonly SyntaxAnnotation RightNodeAnnotation = new();

        protected static readonly SyntaxToken PlusNewLineToken = SyntaxFactory.Token(
            leading: default,
            SyntaxKind.PlusToken,
            SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

        protected readonly Document Document;
        protected readonly int CursorPosition;
        protected readonly SourceText SourceText;
        protected readonly SyntaxNode Root;
        protected readonly IndentationOptions Options;
        protected readonly int TabSize;
        protected readonly bool UseTabs;
        protected readonly CancellationToken CancellationToken;

        public StringSplitter(
            Document document, int position,
            SyntaxNode root, SourceText sourceText,
            in IndentationOptions options, bool useTabs, int tabSize,
            CancellationToken cancellationToken)
        {
            Document = document;
            CursorPosition = position;
            Root = root;
            SourceText = sourceText;
            UseTabs = useTabs;
            TabSize = tabSize;
            Options = options;
            CancellationToken = cancellationToken;
        }

        public static StringSplitter TryCreate(
            Document document, int position,
            in IndentationOptions options, bool useTabs, int tabSize,
            CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var token = root.FindToken(position);

            if (token.IsKind(SyntaxKind.StringLiteralToken) ||
                token.IsKind(SyntaxKind.UTF8StringLiteralToken))
            {
                return new SimpleStringSplitter(
                    document, position, root,
                    sourceText, token, options, useTabs, tabSize,
                    cancellationToken);
            }

            var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
            if (interpolatedStringExpression != null)
            {
                return new InterpolatedStringSplitter(
                    document, position, root,
                    sourceText, interpolatedStringExpression,
                    options, useTabs, tabSize, cancellationToken);
            }

            return null;
        }

        private static InterpolatedStringExpressionSyntax TryGetInterpolatedStringExpression(
            SyntaxToken token, int position)
        {
            if (token.IsKind(SyntaxKind.InterpolatedStringTextToken) ||
                token.IsKind(SyntaxKind.InterpolatedStringEndToken) ||
                IsInterpolationOpenBrace(token, position))
            {
                return token.GetAncestor<InterpolatedStringExpressionSyntax>();
            }

            return null;
        }

        private static bool IsInterpolationOpenBrace(SyntaxToken token, int position)
        {
            return token.Kind() == SyntaxKind.OpenBraceToken &&
                token.Parent.IsKind(SyntaxKind.Interpolation) &&
                position == token.SpanStart;
        }

        protected abstract int StringOpenQuoteLength();

        protected abstract bool CheckToken();

        protected abstract SyntaxNode GetNodeToReplace();

        protected abstract BinaryExpressionSyntax CreateSplitString();

        public bool TrySplit(out Document newDocument, out int newPosition)
        {
            var nodeToReplace = GetNodeToReplace();

            if (CursorPosition <= nodeToReplace.SpanStart || CursorPosition >= nodeToReplace.Span.End)
            {
                newDocument = null;
                newPosition = 0;
                return false;
            }

            if (!CheckToken())
            {
                newDocument = null;
                newPosition = 0;
                return false;
            }

            (newDocument, newPosition) = SplitString();
            return true;
        }

        private (Document document, int caretPosition) SplitString()
        {
            var splitString = CreateSplitString();

            var nodeToReplace = GetNodeToReplace();
            var newRoot = Root.ReplaceNode(nodeToReplace, splitString);
            var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

            var indentString = GetIndentString(newRoot);
            var newRightExpression = rightExpression.WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(indentString));
            var newRoot2 = newRoot.ReplaceNode(rightExpression, newRightExpression);
            var newDocument2 = Document.WithSyntaxRoot(newRoot2);

            return (newDocument2, rightExpression.Span.Start + indentString.Length + StringOpenQuoteLength());
        }

        private string GetIndentString(SyntaxNode newRoot)
        {
            var newDocument = Document.WithSyntaxRoot(newRoot);

            var indentationService = newDocument.GetLanguageService<IIndentationService>();
            var originalLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber;

            var desiredIndentation = indentationService.GetIndentation(
                newDocument, originalLineNumber + 1, Options, CancellationToken);

            var newSourceText = newDocument.GetSyntaxRootSynchronously(CancellationToken).SyntaxTree.GetText(CancellationToken);
            var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition);

            var baseOffsetInLineInPositions = desiredIndentation.BasePosition - baseLine.Start;
            var baseOffsetInLineInColumns = baseLine.GetColumnFromLineOffset(baseOffsetInLineInPositions, TabSize);

            var indent = baseOffsetInLineInColumns + desiredIndentation.Offset;
            var indentString = indent.CreateIndentationString(UseTabs, TabSize);
            return indentString;
        }
    }
}
