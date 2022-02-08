// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
{
    internal class DocCommentConverter : CSharpSyntaxRewriter
    {
        private readonly IDocumentationCommentFormattingService _formattingService;
        private readonly CancellationToken _cancellationToken;

        public static SyntaxNode ConvertToRegularComments(SyntaxNode node, IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
        {
            var converter = new DocCommentConverter(formattingService, cancellationToken);

            return converter.Visit(node);
        }

        private DocCommentConverter(IDocumentationCommentFormattingService formattingService, CancellationToken cancellationToken)
            : base(visitIntoStructuredTrivia: false)
        {
            _formattingService = formattingService;
            _cancellationToken = cancellationToken;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            if (node == null)
            {
                return node;
            }

            // Process children first
            node = base.Visit(node);

            // Check the leading trivia for doc comments.
            if (node.GetLeadingTrivia().Any(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                var newLeadingTrivia = new List<SyntaxTrivia>();

                foreach (var trivia in node.GetLeadingTrivia())
                {
                    if (trivia.Kind() == SyntaxKind.SingleLineDocumentationCommentTrivia)
                    {
                        var structuredTrivia = (DocumentationCommentTriviaSyntax)trivia.GetStructure();
                        var commentLines = ConvertDocCommentToRegularComment(structuredTrivia).ToSyntaxTriviaList();

                        if (commentLines.Count > 0)
                        {
                            newLeadingTrivia.Add(SyntaxFactory.Comment("//"));
                            newLeadingTrivia.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);

                            newLeadingTrivia.AddRange(commentLines);
                        }
                    }
                    else
                    {
                        newLeadingTrivia.Add(trivia);
                    }
                }

                node = node.WithLeadingTrivia(newLeadingTrivia);
            }

            return node;
        }

        private IEnumerable<SyntaxTrivia> ConvertDocCommentToRegularComment(DocumentationCommentTriviaSyntax structuredTrivia)
        {
            var xmlFragment = DocumentationCommentUtilities.ExtractXMLFragment(structuredTrivia.ToFullString(), "///");

            var docComment = DocumentationComment.FromXmlFragment(xmlFragment);

            var commentLines = AbstractMetadataAsSourceService.DocCommentFormatter.Format(_formattingService, docComment);

            foreach (var line in commentLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    yield return SyntaxFactory.Comment("// " + line);
                }
                else
                {
                    yield return SyntaxFactory.Comment("//");
                }

                yield return SyntaxFactory.ElasticCarriageReturnLineFeed;
            }
        }
    }
}
