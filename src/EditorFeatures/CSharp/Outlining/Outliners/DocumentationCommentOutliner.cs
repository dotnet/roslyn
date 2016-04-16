// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class DocumentationCommentOutliner : AbstractSyntaxNodeOutliner<DocumentationCommentTriviaSyntax>
    {
        private static string GetDocumentationCommentPrefix(DocumentationCommentTriviaSyntax documentationComment)
        {
            Contract.ThrowIfNull(documentationComment);

            var leadingTrivia = documentationComment.GetLeadingTrivia();
            var exteriorTrivia = leadingTrivia.Where(t => t.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                                              .FirstOrNullable();

            return exteriorTrivia != null ? exteriorTrivia.Value.ToString() : string.Empty;
        }

        private static string GetBannerText(DocumentationCommentTriviaSyntax documentationComment, CancellationToken cancellationToken)
        {
            // TODO: Consider unifying code to extract text from an Xml Documentation Comment (https://github.com/dotnet/roslyn/issues/2290)
            var summaryElement = documentationComment.ChildNodes().OfType<XmlElementSyntax>()
                                              .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            var prefix = GetDocumentationCommentPrefix(documentationComment);

            string text;
            if (summaryElement != null)
            {
                var sb = new StringBuilder(summaryElement.Span.Length);
                sb.Append(prefix);
                sb.Append(" <summary>");
                foreach (var node in summaryElement.ChildNodes())
                {
                    if (node is XmlTextSyntax)
                    {
                        var textTokens = ((XmlTextSyntax)node).TextTokens;
                        AppendTextTokens(sb, textTokens);
                    }
                    else if (node is XmlEmptyElementSyntax)
                    {
                        var e = (XmlEmptyElementSyntax)node;
                        foreach (var attribute in e.Attributes)
                        {
                            if (attribute is XmlCrefAttributeSyntax)
                            {
                                sb.Append(" ");
                                sb.Append(((XmlCrefAttributeSyntax)attribute).Cref.ToString());
                            }
                            else if (attribute is XmlNameAttributeSyntax)
                            {
                                sb.Append(" ");
                                sb.Append(((XmlNameAttributeSyntax)attribute).Identifier.Identifier.Text);
                            }
                            else if (attribute is XmlTextAttributeSyntax)
                            {
                                AppendTextTokens(sb, ((XmlTextAttributeSyntax)attribute).TextTokens);
                            }
                            else
                            {
                                Debug.Fail($"Unexpected XML syntax kind {attribute.Kind()}");
                            }
                        }
                    }
                }

                text = sb.ToString();
            }
            else
            {
                // If a summary element isn't found, use the first line of the XML doc comment.
                var syntaxTree = documentationComment.SyntaxTree;
                var spanStart = documentationComment.SpanStart;
                var line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(spanStart);
                text = prefix + " " + line.ToString().Substring(spanStart - line.Start).Trim() + " " + CSharpOutliningHelpers.Ellipsis;
            }

            if (text.Length > CSharpOutliningHelpers.MaxXmlDocCommentBannerLength)
            {
                text = text.Substring(0, CSharpOutliningHelpers.MaxXmlDocCommentBannerLength) + " " + CSharpOutliningHelpers.Ellipsis;
            }

            return text;
        }

        private static void AppendTextTokens(StringBuilder sb, SyntaxTokenList textTokens)
        {
            foreach (var tk in textTokens)
            {
                var s = tk.ToString().Trim();
                if (s.Length > 0)
                {
                    sb.Append(" ");
                    sb.Append(s);
                }
            }
        }

        protected override void CollectOutliningSpans(
            DocumentationCommentTriviaSyntax documentationComment,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            var startPos = documentationComment.FullSpan.Start;

            // The trailing newline is included in XmlDocCommentSyntax, so we need to strip it.
            var endPos = documentationComment.SpanStart + documentationComment.ToString().TrimEnd().Length;

            var span = TextSpan.FromBounds(startPos, endPos);

            spans.Add(new OutliningSpan(
                span,
                GetBannerText(documentationComment, cancellationToken),
                autoCollapse: true));
        }
    }
}
