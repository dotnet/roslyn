using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    internal partial class SplitStringLiteralCommandHandler
    {
        private class SimpleStringSplitter : StringSplitter
        {
            private const char QuoteCharacter = '"';
            private readonly SyntaxToken _token;

            public SimpleStringSplitter(
                Document document, int position,
                SyntaxNode root, SourceText sourceText, SyntaxToken token,
                bool useTabs, int tabSize, IndentStyle indentStyle, CancellationToken cancellationToken)
                : base(document, position, root, sourceText, useTabs, tabSize, indentStyle, cancellationToken)
            {
                _token = token;
            }

            // Don't split @"" strings.  They already support directly embedding newlines.
            protected override bool CheckToken()
                => !_token.IsVerbatimStringLiteral();

            protected override SyntaxNode GetNodeToReplace() => _token.Parent;

            protected override BinaryExpressionSyntax CreateSplitString()
            {
                // TODO(cyrusn): Deal with the positoin being after a \ character
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_token.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _token.Span.End)).ToString();

                var firstToken = SyntaxFactory.Token(
                    _token.LeadingTrivia,
                    _token.Kind(),
                    text: prefix + QuoteCharacter,
                    valueText: "",
                    trailing: SyntaxFactory.TriviaList(SyntaxFactory.ElasticSpace));

                var secondToken = SyntaxFactory.Token(
                    default,
                    _token.Kind(),
                    text: QuoteCharacter + suffix,
                    valueText: "",
                    trailing: _token.TrailingTrivia);

                var leftExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, firstToken);
                var rightExpression = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, secondToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    leftExpression,
                    PlusNewLineToken,
                    rightExpression.WithAdditionalAnnotations(RightNodeAnnotation));
            }

            protected override int StringOpenQuoteLength() => "\"".Length;
        }
    }
}
