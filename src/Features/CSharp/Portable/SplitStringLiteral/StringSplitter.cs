// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        protected readonly IndentationOptions Options;
        protected readonly int CursorPosition;
        protected readonly SourceText SourceText;
        protected readonly SyntaxNode Root;
        protected readonly int TabSize;
        protected readonly bool UseTabs;

        private readonly FormattingOptions.IndentStyle _indentStyle;

        public StringSplitter(
            Document document, int position,
            SyntaxNode root, SourceText sourceText,
            bool useTabs, int tabSize,
            FormattingOptions.IndentStyle indentStyle,
            IndentationOptions options)
        {
            Document = document;
            CursorPosition = position;
            Root = root;
            SourceText = sourceText;
            UseTabs = useTabs;
            TabSize = tabSize;
            Options = options;
            _indentStyle = indentStyle;
        }

        public static (Document? document, int caretPosition) TrySplitSynchronously(
            Document document, int position,
            bool useTabs, int tabSize, FormattingOptions.IndentStyle indentStyle,
            IndentationOptions options,
            CancellationToken cancellationToken)
        {
            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            Contract.ThrowIfNull(root);

            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var token = root.FindToken(position);

            StringSplitter? splitter = null;
            if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                splitter = new SimpleStringSplitter(
                    document, position, root,
                    sourceText, token, useTabs, tabSize,
                    indentStyle, options);
            }

            var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
            if (interpolatedStringExpression != null)
            {
                splitter = new InterpolatedStringSplitter(
                    document, position, root,
                    sourceText, interpolatedStringExpression,
                    useTabs, tabSize, indentStyle, options);
            }

            if (splitter == null)
            {
                return default;
            }

            return splitter.TrySplitSynchronously(cancellationToken);
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

        private (Document? document, int caretPosition) TrySplitSynchronously(CancellationToken cancellationToken)
        {
            var nodeToReplace = GetNodeToReplace();

            if (CursorPosition <= nodeToReplace.SpanStart || CursorPosition >= nodeToReplace.Span.End)
            {
                return default;
            }

            if (!CheckToken())
            {
                return default;
            }

            return SplitStringSynchronously(cancellationToken);
        }

        private (Document document, int caretPosition) SplitStringSynchronously(CancellationToken cancellationToken)
        {
            var splitString = CreateSplitString();

            var nodeToReplace = GetNodeToReplace();
            var newRoot = Root.ReplaceNode(nodeToReplace, splitString);
            var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

            var indentString = GetIndentStringSynchronously(newRoot, cancellationToken);
            var newRightExpression = rightExpression.WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(indentString));
            var newRoot2 = newRoot.ReplaceNode(rightExpression, newRightExpression);
            var newDocument2 = Document.WithSyntaxRoot(newRoot2);

            return (newDocument2, rightExpression.Span.Start + indentString.Length + StringOpenQuoteLength());
        }

        private string GetIndentStringSynchronously(SyntaxNode newRoot, CancellationToken cancellationToken)
        {
            var newDocument = Document.WithSyntaxRoot(newRoot);
            var newSyntacticDocument = SyntacticDocument.CreateSynchronously(newDocument, cancellationToken);

            var indentationService = newDocument.GetRequiredLanguageService<IIndentationService>();
            var originalLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber;

            var desiredIndentation = indentationService.GetIndentation(newSyntacticDocument, originalLineNumber + 1, _indentStyle, Options, cancellationToken);

            var baseLine = newSyntacticDocument.Text.Lines.GetLineFromPosition(desiredIndentation.BasePosition);
            var baseOffsetInLineInPositions = desiredIndentation.BasePosition - baseLine.Start;
            var baseOffsetInLineInColumns = baseLine.GetColumnFromLineOffset(baseOffsetInLineInPositions, TabSize);

            var indent = baseOffsetInLineInColumns + desiredIndentation.Offset;
            var indentString = indent.CreateIndentationString(UseTabs, TabSize);
            return indentString;
        }
    }
}
