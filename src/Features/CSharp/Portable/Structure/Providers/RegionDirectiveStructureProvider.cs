// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class RegionDirectiveStructureProvider : AbstractSyntaxNodeStructureProvider<RegionDirectiveTriviaSyntax>
{
    private static string GetBannerText(DirectiveTriviaSyntax simpleDirective)
    {
        var kw = simpleDirective.DirectiveNameToken;
        var prefixLength = kw.Span.End - simpleDirective.Span.Start;
        var text = simpleDirective.ToString()[prefixLength..].Trim();

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
        SyntaxToken previousToken,
        RegionDirectiveTriviaSyntax regionDirective,
        ref TemporaryArray<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var match = regionDirective.GetMatchingDirective(cancellationToken);
        if (match != null)
        {
            // Always auto-collapse regions for Metadata As Source. These generated files only have one region at
            // the top of the file, which has content like the following:
            //
            //   #region Assembly System.Runtime, Version=4.2.2.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
            //   // C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.1.0\ref\netcoreapp3.1\System.Runtime.dll
            //   #endregion
            //
            // For other files, auto-collapse regions based on the user option.
            var autoCollapse = options.IsMetadataAsSource || options.CollapseRegionsWhenCollapsingToDefinitions;

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(regionDirective.SpanStart, match.Span.End),
                type: BlockTypes.PreprocessorRegion,
                bannerText: GetBannerText(regionDirective),
                autoCollapse: autoCollapse,
                isDefaultCollapsed: options.CollapseRegionsWhenFirstOpened));
        }
    }
}
