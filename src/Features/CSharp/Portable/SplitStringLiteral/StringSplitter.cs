// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SplitStringLiteral
{
    internal abstract partial class StringSplitter
    {
        protected readonly SyntaxAnnotation RightNodeAnnotation = new();

        protected static readonly SyntaxToken PlusNewLineToken = SyntaxFactory.Token(
            leading: default,
            SyntaxKind.PlusToken,
            SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

        protected readonly ParsedDocument Document;
        protected readonly int CursorPosition;
        protected readonly IndentationOptions Options;
        protected readonly CancellationToken CancellationToken;

        public StringSplitter(
            ParsedDocument document, int position,
            in IndentationOptions options,
            CancellationToken cancellationToken)
        {
            Document = document;
            CursorPosition = position;
            Options = options;
            CancellationToken = cancellationToken;
        }

        protected int TabSize => Options.FormattingOptions.TabSize;
        protected bool UseTabs => Options.FormattingOptions.UseTabs;

        public static StringSplitter? TryCreate(
            ParsedDocument document, int position,
            in IndentationOptions options,
            CancellationToken cancellationToken)
        {
            var token = document.Root.FindToken(position);

            if (token.IsKind(SyntaxKind.StringLiteralToken) ||
                token.IsKind(SyntaxKind.Utf8StringLiteralToken))
            {
                return new SimpleStringSplitter(
                    document, position,
                    token, options,
                    cancellationToken);
            }

            var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
            if (interpolatedStringExpression != null)
            {
                return new InterpolatedStringSplitter(
                    document, position,
                    interpolatedStringExpression,
                    options, cancellationToken);
            }

            return null;
        }

        private static InterpolatedStringExpressionSyntax? TryGetInterpolatedStringExpression(
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

        public bool TrySplit([NotNullWhen(true)] out SyntaxNode? newRoot, out int newPosition)
        {
            var nodeToReplace = GetNodeToReplace();

            if (CursorPosition <= nodeToReplace.SpanStart || CursorPosition >= nodeToReplace.Span.End)
            {
                newRoot = null;
                newPosition = 0;
                return false;
            }

            if (!CheckToken())
            {
                newRoot = null;
                newPosition = 0;
                return false;
            }

            (newRoot, newPosition) = SplitString();
            return true;
        }

        private (SyntaxNode root, int caretPosition) SplitString()
        {
            var splitString = CreateSplitString();

            var nodeToReplace = GetNodeToReplace();
            var newRoot = Document.Root.ReplaceNode(nodeToReplace, splitString);
            var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

            var indentString = GetIndentString(newRoot);
            var newRightExpression = rightExpression.WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(indentString));
            var newRoot2 = newRoot.ReplaceNode(rightExpression, newRightExpression);

            return (newRoot2, rightExpression.Span.Start + indentString.Length + StringOpenQuoteLength());
        }

        private string GetIndentString(SyntaxNode newRoot)
        {
            var indentationService = Document.LanguageServices.GetRequiredService<IIndentationService>();
            var originalLineNumber = Document.Text.Lines.GetLineFromPosition(CursorPosition).LineNumber;

            var newDocument = Document.WithChangedRoot(newRoot, CancellationToken);
            var desiredIndentation = indentationService.GetIndentation(
                newDocument, originalLineNumber + 1, Options, CancellationToken);

            var newSourceText = newDocument.Text;
            var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition);

            var baseOffsetInLineInPositions = desiredIndentation.BasePosition - baseLine.Start;
            var baseOffsetInLineInColumns = baseLine.GetColumnFromLineOffset(baseOffsetInLineInPositions, TabSize);

            var indent = baseOffsetInLineInColumns + desiredIndentation.Offset;
            var indentString = indent.CreateIndentationString(UseTabs, TabSize);
            return indentString;
        }
    }
}
