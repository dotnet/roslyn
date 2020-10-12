﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class CSharpTriviaFormatter
    {
        private class DocumentationCommentExteriorCommentRewriter : CSharpSyntaxRewriter
        {
            private readonly bool _forceIndentation;
            private readonly int _indentation;
            private readonly int _indentationDelta;
            private readonly AnalyzerConfigOptions _options;

            public DocumentationCommentExteriorCommentRewriter(
                bool forceIndentation,
                int indentation,
                int indentationDelta,
                AnalyzerConfigOptions options,
                bool visitStructuredTrivia = true)
                : base(visitIntoStructuredTrivia: visitStructuredTrivia)
            {
                _forceIndentation = forceIndentation;
                _indentation = indentation;
                _indentationDelta = indentationDelta;
                _options = options;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                {
                    if (IsBeginningOrEndOfDocumentComment(trivia))
                    {
                        return base.VisitTrivia(trivia);
                    }
                    else
                    {
                        var triviaText = trivia.ToFullString();

                        var newTriviaText = triviaText.AdjustIndentForXmlDocExteriorTrivia(
                                                _forceIndentation,
                                                _indentation,
                                                _indentationDelta,
                                                _options.GetOption(FormattingOptions2.UseTabs),
                                                _options.GetOption(FormattingOptions2.TabSize));

                        if (triviaText == newTriviaText)
                        {
                            return base.VisitTrivia(trivia);
                        }

                        var parsedNewTrivia = SyntaxFactory.DocumentationCommentExterior(newTriviaText);

                        return parsedNewTrivia;
                    }
                }

                return base.VisitTrivia(trivia);
            }

            private static bool IsBeginningOrEndOfDocumentComment(SyntaxTrivia trivia)
            {
                var currentParent = trivia.Token.Parent;

                while (currentParent != null)
                {
                    if (currentParent.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                        currentParent.Kind() == SyntaxKind.MultiLineDocumentationCommentTrivia)
                    {
                        if (trivia.Span.End == currentParent.SpanStart ||
                            trivia.Span.End == currentParent.Span.End)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    currentParent = currentParent.Parent;
                }

                return false;
            }
        }
    }
}
