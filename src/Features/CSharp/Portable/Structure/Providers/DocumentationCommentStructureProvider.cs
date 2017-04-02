// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class DocumentationCommentStructureProvider : AbstractSyntaxNodeStructureProvider<DocumentationCommentTriviaSyntax>
    {
        private static string GetDocumentationCommentPrefix(DocumentationCommentTriviaSyntax documentationComment)
        {
            Contract.ThrowIfNull(documentationComment);

            var leadingTrivia = documentationComment.GetLeadingTrivia();
            var exteriorTrivia = leadingTrivia.Where(t => t.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
                                              .FirstOrNullable();

            return exteriorTrivia != null ? exteriorTrivia.Value.ToString() : string.Empty;
        }

        private static void AddSpaceIfNotAlreadyThere(StringBuilder sb)
        {
            if (sb.Length > 0 && sb[sb.Length - 1] != ' ')
            {
                sb.Append(' ');
            }
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
                    if (node.HasLeadingTrivia)
                    {
                        // Collapse all trailing trivia to a single space.
                        AddSpaceIfNotAlreadyThere(sb);
                    }

                    if (node is XmlTextSyntax xmlText)
                    {
                        var textTokens = xmlText.TextTokens;
                        AppendTextTokens(sb, textTokens);
                    }
                    else if (node is XmlEmptyElementSyntax xmlEmpty)
                    {
                        foreach (var attribute in xmlEmpty.Attributes)
                        {
                            if (attribute is XmlCrefAttributeSyntax xmlCref)
                            {
                                AddSpaceIfNotAlreadyThere(sb);
                                sb.Append(xmlCref.Cref.ToString());
                            }
                            else if (attribute is XmlNameAttributeSyntax xmlName)
                            {
                                AddSpaceIfNotAlreadyThere(sb);
                                sb.Append(xmlName.Identifier.Identifier.Text);
                            }
                            else if (attribute is XmlTextAttributeSyntax xmlTextAttribute)
                            {
                                AddSpaceIfNotAlreadyThere(sb);
                                AppendTextTokens(sb, xmlTextAttribute.TextTokens);
                            }
                            else
                            {
                                Debug.Assert(false, $"Unexpected XML syntax kind {attribute.Kind()}");
                            }
                        }
                    }

                    if (node.HasTrailingTrivia)
                    {
                        // Collapse all trailing trivia to a single space.
                        AddSpaceIfNotAlreadyThere(sb);
                    }
                }

                text = sb.ToString().Trim();
            }
            else
            {
                // If a summary element isn't found, use the first line of the XML doc comment.
                var syntaxTree = documentationComment.SyntaxTree;
                var spanStart = documentationComment.SpanStart;
                var line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(spanStart);
                text = prefix + " " + line.ToString().Substring(spanStart - line.Start).Trim() + " " + CSharpStructureHelpers.Ellipsis;
            }

            if (text.Length > CSharpStructureHelpers.MaxXmlDocCommentBannerLength)
            {
                text = text.Substring(0, CSharpStructureHelpers.MaxXmlDocCommentBannerLength) + " " + CSharpStructureHelpers.Ellipsis;
            }

            return text;
        }

        private static void AppendTextTokens(StringBuilder sb, SyntaxTokenList textTokens)
        {
            foreach (var token in textTokens)
            {
                var trimmed = token.ToString().Trim();

                if (trimmed == string.Empty)
                {
                    // If it's all whitespace, then just add a single whitespace.
                    AddSpaceIfNotAlreadyThere(sb);
                }
                else
                {
                    // Collapse all preceding trivia for this token to a single space.
                    if (token.LeadingTrivia.Count > 0)
                    {
                        AddSpaceIfNotAlreadyThere(sb);
                    }

                    sb.Append(trimmed);
                }
            }
        }

        protected override void CollectBlockSpans(
            DocumentationCommentTriviaSyntax documentationComment,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var startPos = documentationComment.FullSpan.Start;

            // The trailing newline is included in XmlDocCommentSyntax, so we need to strip it.
            var endPos = documentationComment.SpanStart + documentationComment.ToString().TrimEnd().Length;

            var span = TextSpan.FromBounds(startPos, endPos);

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                type: BlockTypes.Comment,
                bannerText: GetBannerText(documentationComment, cancellationToken),
                autoCollapse: true));
        }
    }
}
