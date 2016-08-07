using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    internal partial class SplitStringLiteralCommandHandler
    {
        private abstract class StringSplitter
        {
            protected static readonly SyntaxAnnotation RightNodeAnnotation = new SyntaxAnnotation();

            protected readonly Document Document;
            protected readonly int CursorPosition;
            protected readonly SourceText SourceText;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            public StringSplitter(Document document, int position, SyntaxNode root, SourceText sourceText, bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                Document = document;
                CursorPosition = position;
                Root = root;
                SourceText = sourceText;
                UseTabs = useTabs;
                TabSize = tabSize;
                CancellationToken = cancellationToken;
            }

            public static StringSplitter Create(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                var token = root.FindToken(position);

                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    return new SimpleStringSplitter(
                        document, position, root,
                        sourceText, token, useTabs, tabSize,
                        cancellationToken);
                }

                var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
                if (interpolatedStringExpression != null)
                {
                    return new InterpolatedStringSplitter(
                        document, position, root,
                        sourceText, interpolatedStringExpression,
                        useTabs, tabSize, cancellationToken);
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

            public int? TrySplit()
            {
                if (!CheckToken())
                {
                    return null;
                }

                return TrySplitWorker();
            }

            private int? TrySplitWorker()
            {
                var newDocumentAndCaretPosition = SplitString();
                if (newDocumentAndCaretPosition == null)
                {
                    return null;
                }

                var newDocument = newDocumentAndCaretPosition.Item1;
                var finalCaretPosition = newDocumentAndCaretPosition.Item2;

                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return finalCaretPosition;
            }

            protected static SyntaxToken GetPlusToken()
            {
                return SyntaxFactory.Token(
                    default(SyntaxTriviaList),
                    SyntaxKind.PlusToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            private Tuple<Document, int> SplitString()
            {
                var splitString = CreateSplitString();

                var nodeToReplace = GetNodeToReplace();
                var newRoot = Root.ReplaceNode(nodeToReplace, splitString);
                var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

                var indentString = GetIndentString(newRoot);
                if (indentString == null)
                {
                    return null;
                }

                var newRightExpression = rightExpression.WithLeadingTrivia(SyntaxFactory.ElasticWhitespace(indentString));
                var newRoot2 = newRoot.ReplaceNode(rightExpression, newRightExpression);
                var newDocument2 = Document.WithSyntaxRoot(newRoot2);

                return Tuple.Create(newDocument2, rightExpression.Span.Start + indentString.Length + StringOpenQuoteLength());
            }

            private string GetIndentString(SyntaxNode newRoot)
            {
                var newDocument = Document.WithSyntaxRoot(newRoot);

                var indentationService = newDocument.GetLanguageService<ISynchronousIndentationService>();
                var originalLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber;
                var desiredIndentation = indentationService.GetDesiredIndentation(
                    newDocument, originalLineNumber + 1, CancellationToken);

                if (desiredIndentation == null)
                {
                    return null;
                }

                var newSourceText = newDocument.GetSyntaxRootSynchronously(CancellationToken).SyntaxTree.GetText(CancellationToken);
                var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.Value.BasePosition);
                var baseOffsetInLine = desiredIndentation.Value.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + desiredIndentation.Value.Offset;
                var indentString = indent.CreateIndentationString(UseTabs, TabSize);
                return indentString;
            }
        }
    }
}