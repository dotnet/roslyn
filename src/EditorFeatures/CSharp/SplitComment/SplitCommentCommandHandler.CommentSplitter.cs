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
        private class CommentSplitter
        {
            protected static readonly SyntaxAnnotation RightNodeAnnotation = new SyntaxAnnotation();

            protected readonly Document Document;
            protected readonly int CursorPosition;
            protected readonly SourceText SourceText;
            protected readonly SyntaxNode Root;
            protected readonly int TabSize;
            protected readonly bool UseTabs;
            protected readonly CancellationToken CancellationToken;

            private const string CommentCharacter = "//";
            private readonly SyntaxTrivia _trivia;

            private readonly IndentStyle _indentStyle;

            public CommentSplitter(
               Document document, int position,
               SyntaxNode root, SourceText sourceText,
               bool useTabs, int tabSize, SyntaxTrivia trivia,
               IndentStyle indentStyle, CancellationToken cancellationToken)
            {
                Document = document;
                CursorPosition = position;
                Root = root;
                SourceText = sourceText;
                UseTabs = useTabs;
                TabSize = tabSize;
                _trivia = trivia;
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
                    ? new CommentSplitter(
                        document, position, root,
                        sourceText, useTabs, tabSize,
                        trivia, indentStyle, cancellationToken)
                    : null;
            }

            protected SyntaxNode GetNodeToReplace() => _trivia.SyntaxTree.GetRoot();

            protected SyntaxTriviaList CreateSplitComment(string indentString)
            {
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _trivia.Span.End)).ToString();

                var firstTrivia = SyntaxFactory.Comment(prefix);
                var secondTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
                var thirdTrivia = SyntaxFactory.Comment(indentString + CommentCharacter + suffix);

                return SyntaxFactory.TriviaList(firstTrivia, secondTrivia, thirdTrivia);
            }

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
                var indentString = GetIndentString(Root);
                var nodeToRemove = GetNodeToReplace();

                var splitComment = CreateSplitComment(indentString);
                var commentToReplace = nodeToRemove.FindTrivia(CursorPosition);
                var newRoot = Root.ReplaceTrivia(commentToReplace, splitComment);

                var newLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber + 1;
                var newPosition = SourceText.Lines[newLineNumber].GetLastNonWhitespacePosition();
                var newDocument = Document.WithSyntaxRoot(newRoot);

                return (newDocument, newPosition.GetValueOrDefault());
            }

            private string GetIndentString(SyntaxNode newRoot)
            {
                var newDocument = Document.WithSyntaxRoot(newRoot);

                var indentationService = newDocument.GetLanguageService<Indentation.IIndentationService>();
                var originalLineNumber = SourceText.Lines.GetLineFromPosition(CursorPosition).LineNumber;

                var desiredIndentation = indentationService.GetIndentation(
                    newDocument, originalLineNumber, _indentStyle, CancellationToken);

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
