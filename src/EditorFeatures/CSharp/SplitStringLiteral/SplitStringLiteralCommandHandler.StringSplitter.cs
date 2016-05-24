using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            protected readonly SyntaxTree SyntaxTree;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            public StringSplitter(Document document, int position, SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText, bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                Document = document;
                CursorPosition = position;
                SyntaxTree = syntaxTree;
                Root = root;
                SourceText = sourceText;
                UseTabs = useTabs;
                TabSize = tabSize;
                CancellationToken = cancellationToken;
            }

            public static StringSplitter Create(
                Document document, int position,
                SyntaxTree syntaxTree, SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize, CancellationToken cancellationToken)
            {
                var token = root.FindToken(position);

                if (token.IsKind(SyntaxKind.StringLiteralToken))
                {
                    return new SimpleStringSplitter(
                        document, position, syntaxTree, root,
                        sourceText, token, useTabs, tabSize,
                        cancellationToken);
                }

                var interpolatedStringExpression = TryGetInterpolatedStringExpression(token, position);
                if (interpolatedStringExpression != null)
                {
                    return new InterpolatedStringSplitter(
                        document, position, syntaxTree, root,
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

            protected abstract BinaryExpressionSyntax CreateSplitString(string indentString);

            public async Task<int?> TrySplitAsync()
            {
                if (!CheckToken())
                {
                    return null;
                }

                return await TrySplitWorkerAsync().ConfigureAwait(false);
            }

            private async Task<int?> TrySplitWorkerAsync()
            {
                var indentation = await DetermineIndentationAsync().ConfigureAwait(false);
                if (indentation == null)
                {
                    return null;
                }

                var indentString = indentation.Value.CreateIndentationString(UseTabs, TabSize);

                var newDocumentAndCaretPosition = SplitString(indentString);
                var newDocument = newDocumentAndCaretPosition.Item1;
                var finalCaretPosition = newDocumentAndCaretPosition.Item2;

                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return finalCaretPosition;
            }

            protected async Task<int?> DetermineIndentationAsync()
            {
                var newDocumentAndCaretPosition = SplitString(indentString: null);
                var newDocument = newDocumentAndCaretPosition.Item1;

                var indentationService = newDocument.GetLanguageService<IIndentationService>();
                var currentLine = SourceText.Lines.GetLineFromPosition(CursorPosition);
                var indentation = await indentationService.GetDesiredIndentationAsync(
                    newDocument, currentLine.LineNumber + 1, CancellationToken).ConfigureAwait(false);

                if (indentation == null)
                {
                    return null;
                }

                var newSourceText = await newDocument.GetTextAsync(CancellationToken).ConfigureAwait(false);
                var baseLine = newSourceText.Lines.GetLineFromPosition(indentation.Value.BasePosition);
                var baseOffsetInLine = indentation.Value.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + indentation.Value.Offset;

                return indent;
            }

            protected static SyntaxToken GetPlusToken()
            {
                return SyntaxFactory.Token(
                    default(SyntaxTriviaList),
                    SyntaxKind.PlusToken,
                    SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
            }

            private Tuple<Document, int> SplitString(string indentString)
            {
                var splitString = CreateSplitString(indentString);

                var newRoot = Root.ReplaceNode(GetNodeToReplace(), splitString);
                var rightExpression = newRoot.GetAnnotatedNodes(RightNodeAnnotation).Single();

                var newDocument = Document.WithSyntaxRoot(newRoot);
                return Tuple.Create(newDocument, rightExpression.Span.Start + StringOpenQuoteLength());
            }

            protected static SyntaxTriviaList GetLeadingIndentationTrivia(string indentString)
            {
                return indentString == null
                    ? default(SyntaxTriviaList)
                    : SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(indentString));
            }
        }
    }
}