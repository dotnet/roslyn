using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    using Microsoft.CodeAnalysis.Indentation;

    internal partial class SplitStringLiteralCommandHandler
    {
        private abstract class StringSplitter
        {
            protected static readonly SyntaxAnnotation RightNodeAnnotation = new SyntaxAnnotation();

            protected static readonly SyntaxToken PlusNewLineToken = SyntaxFactory.Token(
                leading: default,
                SyntaxKind.PlusToken,
                SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));

            protected readonly Document Document;
            protected readonly int CursorPosition;
            protected readonly SourceText SourceText;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            private readonly IndentStyle _indentStyle;

            public StringSplitter(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize,
                IndentStyle indentStyle, CancellationToken cancellationToken)
            {
                Document = document;
                CursorPosition = position;
                Root = root;
                SourceText = sourceText;
                UseTabs = useTabs;
                TabSize = tabSize;
                _indentStyle = indentStyle;
                CancellationToken = cancellationToken;
            }

            public static StringSplitter Create(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize, IndentStyle indentStyle,
                CancellationToken cancellationToken)
            {
                var token = root.FindToken(position);

                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    return new SimpleStringSplitter(
                        document, position, root,
                        sourceText, token, useTabs, tabSize,
                        indentStyle, cancellationToken);
                }

                var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
                if (interpolatedStringExpression != null)
                {
                    return new InterpolatedStringSplitter(
                        document, position, root,
                        sourceText, interpolatedStringExpression,
                        useTabs, tabSize, indentStyle, cancellationToken);
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
                var nodeToReplace = GetNodeToReplace();

                if (CursorPosition <= nodeToReplace.SpanStart || CursorPosition >= nodeToReplace.Span.End)
                {
                    return null;
                }

                if (!CheckToken())
                {
                    return null;
                }

                return SplitWorker();
            }

            private int SplitWorker()
            {
                var (newDocument, finalCaretPosition) = SplitString();

                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return finalCaretPosition;
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
                    newDocument, originalLineNumber + 1, _indentStyle, CancellationToken);

                var newSourceText = newDocument.GetSyntaxRootSynchronously(CancellationToken).SyntaxTree.GetText(CancellationToken);
                var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition);
                var baseOffsetInLine = desiredIndentation.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + desiredIndentation.Offset;
                var indentString = indent.CreateIndentationString(UseTabs, TabSize);
                return indentString;
            }
        }
    }
}
