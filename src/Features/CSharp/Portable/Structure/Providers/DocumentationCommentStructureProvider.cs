// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class DocumentationCommentStructureProvider : AbstractSyntaxNodeStructureProvider<DocumentationCommentTriviaSyntax>
    {
        protected override void CollectBlockSpans(
            DocumentationCommentTriviaSyntax documentationComment,
            ArrayBuilder<BlockSpan> spans,
            bool isMetadataAsSource,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var startPos = documentationComment.FullSpan.Start;

            // The trailing newline is included in XmlDocCommentSyntax, so we need to strip it.
            var endPos = documentationComment.SpanStart + documentationComment.ToString().TrimEnd().Length;

            var span = TextSpan.FromBounds(startPos, endPos);

            var bannerLength = options.GetOption(BlockStructureOptions.MaximumBannerLength, LanguageNames.CSharp);
            var bannerText = CSharpSyntaxFacts.Instance.GetBannerText(
                documentationComment, bannerLength, cancellationToken);

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                type: BlockTypes.Comment,
                bannerText: bannerText,
                autoCollapse: true));
        }
    }
}
