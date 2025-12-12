// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class MultilineCommentBlockStructureProvider : AbstractSyntaxTriviaStructureProvider
{
    public override void CollectBlockSpans(
        SyntaxTrivia trivia,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var span = new BlockSpan(
            isCollapsible: true,
            textSpan: trivia.Span,
            hintSpan: trivia.Span,
            type: BlockTypes.Comment,
            bannerText: CSharpStructureHelpers.GetCommentBannerText(trivia),
            autoCollapse: true);
        spans.Add(span);
    }
}
