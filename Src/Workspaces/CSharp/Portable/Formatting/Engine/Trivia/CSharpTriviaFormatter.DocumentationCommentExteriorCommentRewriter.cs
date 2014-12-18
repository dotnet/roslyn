// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Formatting
{
    internal partial class CSharpTriviaFormatter : AbstractTriviaFormatter<SyntaxTrivia>
    {
        private class DocumentationCommentExteriorCommentRewriter : CSharpSyntaxRewriter
        {
            private bool forceIndentation;
            private int indentation;
            private int indentationDelta;
            private OptionSet optionSet;

            public DocumentationCommentExteriorCommentRewriter(
                bool forceIndentation,
                int indentation,
                int indentationDelta,
                OptionSet optionSet,
                bool visitStructuredTrivia = true)
                : base(visitIntoStructuredTrivia: visitStructuredTrivia)
            {
                this.forceIndentation = forceIndentation;
                this.indentation = indentation;
                this.indentationDelta = indentationDelta;
                this.optionSet = optionSet;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.CSharpKind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                {
                    if (IsBeginningOrEndOfDocumentComment(trivia))
                    {
                        return base.VisitTrivia(trivia);
                    }
                    else
                    {
                        var triviaText = trivia.ToFullString();

                        var newTriviaText = triviaText.AdjustIndentForXmlDocExteriorTrivia(
                                                forceIndentation,
                                                indentation,
                                                indentationDelta,
                                                this.optionSet.GetOption(FormattingOptions.UseTabs, LanguageNames.CSharp),
                                                this.optionSet.GetOption(FormattingOptions.TabSize, LanguageNames.CSharp));

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

            private bool IsBeginningOrEndOfDocumentComment(SyntaxTrivia trivia)
            {
                var currentParent = trivia.Token.Parent;

                while (currentParent != null)
                {
                    if (currentParent.CSharpKind() == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                        currentParent.CSharpKind() == SyntaxKind.MultiLineDocumentationCommentTrivia)
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
