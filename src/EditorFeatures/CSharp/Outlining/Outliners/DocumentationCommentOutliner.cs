// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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
            var summaryElement = documentationComment.ChildNodes().OfType<XmlElementSyntax>()
                                              .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

            var prefix = GetDocumentationCommentPrefix(documentationComment);

            string text;
            if (summaryElement != null)
            {
                var summaryText = from xmlText in summaryElement.ChildNodes().OfType<XmlTextSyntax>()
                                  from tk in xmlText.TextTokens
                                  let s = tk.ToString().Trim()
                                  where s.Length > 0
                                  select s;
                text = prefix + " <summary> " + string.Join(" ", summaryText);
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
