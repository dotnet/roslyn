// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal class MetadataRegionDirectiveStructureProvider : AbstractSyntaxNodeStructureProvider<RegionDirectiveTriviaSyntax>
    {
        private static string GetBannerText(DirectiveTriviaSyntax simpleDirective)
        {
            var kw = simpleDirective.DirectiveNameToken;
            var prefixLength = simpleDirective.HashToken.Span.Length + kw.Span.Length;
            var text = simpleDirective.ToString().Substring(prefixLength).Trim();

            if (text.Length == 0)
            {
                return simpleDirective.HashToken.ToString() + kw.ToString();
            }
            else
            {
                return text;
            }
        }

        protected override void CollectBlockSpans(
            RegionDirectiveTriviaSyntax regionDirective,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var match = regionDirective.GetMatchingDirective(cancellationToken);
            if (match != null)
            {
                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End),
                    type: BlockTypes.PreprocessorRegion,
                    bannerText: GetBannerText(regionDirective),
                    autoCollapse: true));
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return kind == WorkspaceKind.MetadataAsSource;
        }
    }
}
