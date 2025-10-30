// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class DocumentationCommentStructureProvider : AbstractSyntaxNodeStructureProvider<DocumentationCommentTriviaSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        DocumentationCommentTriviaSyntax documentationComment,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        // In metadata as source we want to treat documentation comments slightly differently, and collapse them
        // to just "..." in front of the decalaration they're attached to. That happens in CSharpStructureHelper.CollectCommentBlockSpans
        // so we don't need to do anything here
        if (options.IsMetadataAsSource)
        {
            return;
        }

        var startPos = documentationComment.FullSpan.Start;

        // The trailing newline is included in XmlDocCommentSyntax, so we need to strip it.
        var endPos = documentationComment.SpanStart + documentationComment.ToString().TrimEnd().Length;

        var span = TextSpan.FromBounds(startPos, endPos);

        var bannerLength = options.MaximumBannerLength;
        var bannerText = CSharpFileBannerFacts.Instance.GetBannerText(
            documentationComment, bannerLength, cancellationToken);

        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: span,
            type: BlockTypes.Comment,
            bannerText: bannerText,
            autoCollapse: true));
    }
}
