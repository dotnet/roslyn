// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class ArgumentListStructureProvider : AbstractSyntaxNodeStructureProvider<ArgumentListSyntax>
{
    protected override void CollectBlockSpans(SyntaxToken previousToken, ArgumentListSyntax node, ref TemporaryArray<BlockSpan> spans, BlockStructureOptions options, CancellationToken cancellationToken)
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
        if (end - start < 2)
        {
            return false;
        }

        // For a case like:
        //
        // M1(M2(
        //      "",
        //      "",
        //      ""));
        //
        // We only want to collapse the inner one. So, we want to skip the parent node if it has a child candidate on the same line.
        foreach (var descendant in node.DescendantNodes().OfType<ArgumentListSyntax>())
        {
            var descendantStart = text.Lines.GetLinePosition(descendant.OpenParenToken.SpanStart).Line;
            if (descendantStart > start)
            {
                // We are now on a different line. We can simply break because all next nodes will be on different lines.
                break;
            }

            // We should be on the same line.
            if (descendantStart == start)
            {
                if (IsCandidate(descendant, cancellationToken))
                {
                    return false;
                }
            }
            else
            {
                Debug.Fail($"Found a descendant node on line {descendantStart} while the original node on line {start}. Original node: '{node.ToFullString()}'.");
            }
        }

        return true;
    }
}
