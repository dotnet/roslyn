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
               IndentStyle indentStyle, CancellationToken cancellationToken)
            {
                _document = document;
                _cursorPosition = position;
                _root = root;
                _sourceText = sourceText;
                _useTabs = useTabs;
                _tabSize = tabSize;
                _trivia = trivia;
                _indentStyle = indentStyle;
                _cancellationToken = cancellationToken;
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

            protected override SyntaxTriviaList CreateSplitComment(string indentString)
            {
                var prefix = _sourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, _cursorPosition)).ToString();
                var suffix = _sourceText.GetSubText(TextSpan.FromBounds(_cursorPosition, _trivia.Span.End)).ToString();

                var firstTrivia = SyntaxFactory.Comment(prefix);
                var secondTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
                var thirdTrivia = SyntaxFactory.Comment(indentString + CommentCharacter + suffix);

                return SyntaxFactory.TriviaList(firstTrivia, secondTrivia, thirdTrivia);
            }
        }
    }
}
