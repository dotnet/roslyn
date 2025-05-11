// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class ArgumentListStructureProvider : AbstractSyntaxNodeStructureProvider<ArgumentListSyntax>
{
    protected override void CollectBlockSpans(SyntaxToken previousToken, ArgumentListSyntax node, ArrayBuilder<BlockSpan> spans, BlockStructureOptions options, CancellationToken cancellationToken)
    {
        if (!IsCandidate(node, cancellationToken))
        {
            return;
        }

        spans.Add(new BlockSpan(
            type: BlockTypes.Expression,
            isCollapsible: true,
            node.Span));
    }

    private static bool IsCandidate(ArgumentListSyntax node, CancellationToken cancellationToken)
    {
        var openToken = node.OpenParenToken;
        var closeToken = node.CloseParenToken;
        if (openToken.IsMissing || closeToken.IsMissing)
        {
            return false;
        }

        var text = node.SyntaxTree.GetText(cancellationToken);
        var start = text.Lines.GetLinePosition(openToken.SpanStart).Line;
        var end = text.Lines.GetLinePosition(closeToken.SpanStart).Line;
        return end - start >= 2;
    }
}
