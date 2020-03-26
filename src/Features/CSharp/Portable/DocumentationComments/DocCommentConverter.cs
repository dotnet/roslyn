// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.PooledObjects;
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
                using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var newLeadingTrivia);

                // Skip past anything up to the actual doc comment.  We don't want to add these the final comments we're
                // adding as they can throw off the formatting engine.
                foreach (var trivia in node.GetLeadingTrivia().SkipWhile(t => !t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)))
                {
                    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
                    {
                        ConvertDocCommentToRegularComment(
                            (DocumentationCommentTriviaSyntax)trivia.GetStructure(), newLeadingTrivia);
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

        private void ConvertDocCommentToRegularComment(
            DocumentationCommentTriviaSyntax structuredTrivia, ArrayBuilder<SyntaxTrivia> builder)
        {
            var xmlFragment = DocumentationCommentUtilities.ExtractXMLFragment(structuredTrivia.ToFullString(), "///");
            var docComment = DocumentationComment.FromXmlFragment(xmlFragment);
            var commentLines = AbstractMetadataAsSourceService.DocCommentFormatter.Format(_formattingService, docComment);

            if (commentLines.Length > 0)
            {
                builder.Add(SyntaxFactory.Comment("//"));
                builder.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
            }

            foreach (var line in commentLines)
            {
                builder.Add(SyntaxFactory.Comment(string.IsNullOrWhiteSpace(line)
                    ? "//" : "// " + line));
                builder.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
            }
        }
    }
}
