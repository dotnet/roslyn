using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    internal partial class SplitCommentCommandHandler
    {
        private class SimpleCommentSplitter : CommentSplitter
        {
            private const string CommentCharacter = "//";
            private readonly SyntaxTrivia _trivia;

            public SimpleCommentSplitter(
                Document document, int position,
                SyntaxNode root, SourceText sourceText, SyntaxTrivia trivia,
                bool useTabs, int tabSize, IndentStyle indentStyle, CancellationToken cancellationToken)
                : base(document, position, root, sourceText, useTabs, tabSize, indentStyle, cancellationToken)
            {
                _trivia = trivia;
            }

            protected override SyntaxNode GetNodeToReplace() => _trivia.Token.Parent;

            protected override SyntaxTriviaList CreateSplitComment(string indentString)
            {
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_trivia.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _trivia.Span.End)).ToString();

                var firstTrivia = SyntaxFactory.Comment(prefix);
                var secondTrivia = SyntaxFactory.ElasticCarriageReturnLineFeed;
                var thirdTrivia = SyntaxFactory.Comment(indentString + CommentCharacter + suffix);

                return SyntaxFactory.TriviaList(firstTrivia, secondTrivia, thirdTrivia);
            }

            protected override int CommentTokenLength() => "//".Length;
        }
    }
}
