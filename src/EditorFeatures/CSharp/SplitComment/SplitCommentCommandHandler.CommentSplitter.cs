// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    internal partial class SplitCommentCommandHandler
    {
        private class CommentSplitter : AbstractCommentSplitter
        {
            internal const string CommentCharacter = "//";

            private CommentSplitter(
               Document document, int position,
               SyntaxNode root, SourceText sourceText,
               bool useTabs, int tabSize, SyntaxTrivia trivia,
               IndentStyle indentStyle,
               bool hasSpaceAfterComment, CancellationToken cancellationToken)
            {
                _document = document;
                _cursorPosition = position;
                _root = root;
                _sourceText = sourceText;
                _useTabs = useTabs;
                _tabSize = tabSize;
                _trivia = trivia;
                _indentStyle = indentStyle;
                _hasSpaceAfterComment = hasSpaceAfterComment;
                _cancellationToken = cancellationToken;
            }

            public static CommentSplitter Create(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                bool useTabs, int tabSize, IndentStyle indentStyle,
                bool hasSpaceAfterComment,
                CancellationToken cancellationToken)
            {
                var trivia = root.FindTrivia(position);

                return trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                    ? new CommentSplitter(
                        document, position, root,
                        sourceText, useTabs, tabSize,
                        trivia, indentStyle,
                        hasSpaceAfterComment, cancellationToken)
                    : null;
            }

            protected override SyntaxTriviaList CreateSplitComment(string indentString)
            {
                var prefix = _sourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, _cursorPosition)).ToString().Trim(' ');
                var suffix = _sourceText.GetSubText(TextSpan.FromBounds(_cursorPosition, _trivia.Span.End)).ToString().Trim(' ');

                var firstTrivia = SyntaxFactory.Comment(prefix);
                var secondTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
                var thirdTrivia = _hasSpaceAfterComment ?
                    SyntaxFactory.Comment(indentString + CommentCharacter + " " + suffix) :
                    SyntaxFactory.Comment(indentString + CommentCharacter + suffix);

                return SyntaxFactory.TriviaList(firstTrivia, secondTrivia, thirdTrivia);
            }

            protected override string GetIndentString(SyntaxNode newRoot)
            {
                var newDocument = _document.WithSyntaxRoot(newRoot);

                var indentationService = newDocument.GetLanguageService<Indentation.IIndentationService>();
                var originalLineNumber = _sourceText.Lines.GetLineFromPosition(_cursorPosition).LineNumber;

                var desiredIndentation = indentationService.GetIndentation(
                    newDocument, originalLineNumber, _indentStyle, _cancellationToken);

                var newSourceText = newDocument.GetSyntaxRootSynchronously(_cancellationToken).SyntaxTree.GetText(_cancellationToken);
                var baseLine = newSourceText.Lines.GetLineFromPosition(desiredIndentation.BasePosition);
                var baseOffsetInLine = desiredIndentation.BasePosition - baseLine.Start;

                var indent = baseOffsetInLine + desiredIndentation.Offset;
                var indentString = indent.CreateIndentationString(_useTabs, _tabSize);

                return indentString;
            }
        }
    }
}
