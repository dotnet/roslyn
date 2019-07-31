using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Formatting.FormattingOptions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    internal partial class SplitStringLiteralCommandHandler
    {
        private class InterpolatedStringSplitter : StringSplitter
        {
            private readonly InterpolatedStringExpressionSyntax _interpolatedStringExpression;

            public InterpolatedStringSplitter(
                Document document, int position,
                SyntaxNode root, SourceText sourceText,
                InterpolatedStringExpressionSyntax interpolatedStringExpression,
                bool useTabs, int tabSize, IndentStyle indentStyle,
                CancellationToken cancellationToken)
                : base(document, position, root, sourceText, useTabs, tabSize, indentStyle, cancellationToken)
            {
                _interpolatedStringExpression = interpolatedStringExpression;
            }

            protected override SyntaxNode GetNodeToReplace() => _interpolatedStringExpression;

            // Don't offer on $@"" strings.  They support newlines directly in their content.
            protected override bool CheckToken()
                => _interpolatedStringExpression.StringStartToken.Kind() != SyntaxKind.InterpolatedVerbatimStringStartToken;

            protected override BinaryExpressionSyntax CreateSplitString()
            {
                var contents = _interpolatedStringExpression.Contents.ToList();

                var beforeSplitContents = new List<InterpolatedStringContentSyntax>();
                var afterSplitContents = new List<InterpolatedStringContentSyntax>();

                foreach (var content in contents)
                {
                    if (content.Span.End <= CursorPosition)
                    {
                        // Content is entirely before the cursor.  Nothing needs to be done to it.
                        beforeSplitContents.Add(content);
                    }
                    else if (content.Span.Start >= CursorPosition)
                    {
                        // Content is entirely after the cursor.  Nothing needs to be done to it.
                        afterSplitContents.Add(content);
                    }
                    else
                    {
                        // Content crosses the cursor.  Need to split it.
                        beforeSplitContents.Add(CreateInterpolatedStringText(content.SpanStart, CursorPosition));
                        afterSplitContents.Insert(0, CreateInterpolatedStringText(CursorPosition, content.Span.End));
                    }
                }

                var leftExpression = SyntaxFactory.InterpolatedStringExpression(
                    _interpolatedStringExpression.StringStartToken,
                    SyntaxFactory.List(beforeSplitContents),
                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringEndToken)
                                 .WithTrailingTrivia(SyntaxFactory.ElasticSpace));


                var rightExpression = SyntaxFactory.InterpolatedStringExpression(
                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                    SyntaxFactory.List(afterSplitContents),
                    _interpolatedStringExpression.StringEndToken);

                return SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    leftExpression,
                    PlusNewLineToken,
                    rightExpression.WithAdditionalAnnotations(RightNodeAnnotation));
            }

            private InterpolatedStringTextSyntax CreateInterpolatedStringText(int start, int end)
            {
                var content = SourceText.ToString(TextSpan.FromBounds(start, end));
                return SyntaxFactory.InterpolatedStringText(
                    SyntaxFactory.Token(
                        leading: default,
                        kind: SyntaxKind.InterpolatedStringTextToken,
                        text: content,
                        valueText: "",
                        trailing: default));
            }

            protected override int StringOpenQuoteLength() => "$\"".Length;
        }
    }
}
