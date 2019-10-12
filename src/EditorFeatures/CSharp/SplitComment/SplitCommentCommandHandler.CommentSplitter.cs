// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    internal partial class SplitCommentCommandHandler
    {
        private abstract class CommentSplitter
        {
            protected static readonly SyntaxAnnotation RightNodeAnnotation = new SyntaxAnnotation();

            protected static readonly SyntaxToken NewLineCommentToken = SyntaxFactory.Token(
                leading: default,
                SyntaxFactory.ElasticCarriageReturnLineFeed.Kind(),
                SyntaxFactory.TriviaList(SyntaxFactory.Comment(string.Empty)));

            protected readonly Document Document;
            protected readonly int CursorPosition;
            protected readonly SourceText SourceText;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            private readonly IndentStyle _indentStyle;

            public CommentSplitter(
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

            public static CommentSplitter Create(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize, IndentStyle indentStyle,
                CancellationToken cancellationToken)
            {
                var trivia = root.FindTrivia(position);

                return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    ? new SimpleCommentSplitter(
                        document, position, root,
                        sourceText, trivia, useTabs, tabSize,
                        indentStyle, cancellationToken)
                    : null;
            }

            protected abstract int CommentTokenLength();

            protected abstract SyntaxNode GetNodeToReplace();

            protected abstract SyntaxTriviaList CreateSplitComment();

            public int? TrySplit()
            {
                var nodeToReplace = GetNodeToReplace();

                if (CursorPosition <= nodeToReplace.SpanStart || CursorPosition >= nodeToReplace.Span.End)
                {
                    return null;
                }

                return SplitWorker();
            }

            private int SplitWorker()
            {
                var (newDocument, finalCaretPosition) = SplitComment();

                var workspace = Document.Project.Solution.Workspace;
                workspace.TryApplyChanges(newDocument.Project.Solution);

                return finalCaretPosition;
            }

            private (Document document, int caretPosition) SplitComment()
            {
                var splitComment = CreateSplitComment();

                var nodeToRemove = GetNodeToReplace();
                var commentToReplace = nodeToRemove.FindTrivia(CursorPosition);
                var newRoot = Root.ReplaceTrivia(commentToReplace, splitComment);

                var indentString = GetIndentString(newRoot);
                var newSplitComment = SyntaxFactory.Comment(SyntaxFactory.ElasticWhitespace(indentString).ToFullString() + splitComment.ToFullString());
                var newRoot2 = newRoot.ReplaceTrivia(splitComment.First(), newSplitComment);
                var newDocument2 = Document.WithSyntaxRoot(newRoot2);

                return (newDocument2, newSplitComment.Span.Start + indentString.Length + CommentTokenLength());
            }

            private string GetIndentString(SyntaxNode newRoot)
            {
                var newDocument = Document.WithSyntaxRoot(newRoot);

                var indentationService = newDocument.GetLanguageService<Indentation.IIndentationService>();
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
