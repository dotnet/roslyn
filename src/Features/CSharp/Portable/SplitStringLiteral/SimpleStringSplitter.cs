// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.SplitStringLiteral
{
    internal abstract partial class StringSplitter
    {
        private sealed class SimpleStringSplitter : StringSplitter
        {
            private const char QuoteCharacter = '"';
            private readonly SyntaxToken _token;

            public SimpleStringSplitter(
                Document document, int position,
                SyntaxNode root, SourceText sourceText, SyntaxToken token,
                in IndentationOptions options, bool useTabs, int tabSize, CancellationToken cancellationToken)
                : base(document, position, root, sourceText, options, useTabs, tabSize, cancellationToken)
            {
                _token = token;
            }

            // Don't split @"" strings.  They already support directly embedding newlines.
            // Don't split UTF8 strings if the cursor is after the quote.
            protected override bool CheckToken()
                => !_token.IsVerbatimStringLiteral() && !CursorIsAfterQuotesInUTF8String();

            private bool CursorIsAfterQuotesInUTF8String()
            {
                return _token.IsKind(SyntaxKind.UTF8StringLiteralToken) && CursorPosition >= _token.Span.End - "u8".Length;
            }

            protected override SyntaxNode GetNodeToReplace() => _token.Parent;

            protected override BinaryExpressionSyntax CreateSplitString()
            {
                // TODO(cyrusn): Deal with the positoin being after a \ character
                var prefix = SourceText.GetSubText(TextSpan.FromBounds(_token.SpanStart, CursorPosition)).ToString();
                var suffix = SourceText.GetSubText(TextSpan.FromBounds(CursorPosition, _token.Span.End)).ToString();

                // If we're spliting a UTF8 string we need to keep the u8 suffix on the first part. We copy whatever
                // the user had on the second part, for consistency.
                var firstTokenSuffix = _token.Kind() == SyntaxKind.UTF8StringLiteralToken
                    ? SourceText.GetSubText(TextSpan.FromBounds(_token.Span.End - "u8".Length, _token.Span.End)).ToString()
                    : "";

                var firstToken = SyntaxFactory.Token(
                    _token.LeadingTrivia,
                    _token.Kind(),
                    text: prefix + QuoteCharacter + firstTokenSuffix,
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
