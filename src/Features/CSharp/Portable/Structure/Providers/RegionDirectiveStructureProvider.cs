// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class RegionDirectiveStructureProvider : AbstractSyntaxNodeStructureProvider<RegionDirectiveTriviaSyntax>
    {
        private static string GetBannerText(DirectiveTriviaSyntax simpleDirective)
        {
            var kw = simpleDirective.DirectiveNameToken;
            var prefixLength = kw.Span.End - simpleDirective.Span.Start;
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
                var autoCollapse = options.GetOption(
                    BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);
                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End),
                    type: BlockTypes.PreprocessorRegion,
                    bannerText: GetBannerText(regionDirective),
                    autoCollapse: autoCollapse,
                    isDefaultCollapsed: true));
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return kind != WorkspaceKind.MetadataAsSource;
        }
    }
}
