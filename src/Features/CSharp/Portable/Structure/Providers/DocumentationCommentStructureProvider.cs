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
        private static string GetBannerText(DocumentationCommentTriviaSyntax documentationComment, CancellationToken cancellationToken)
            => CSharpSyntaxFactsService.Instance.GetBannerText(documentationComment, cancellationToken);

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
