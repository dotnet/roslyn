// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class InitializerExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<InitializerExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        InitializerExpressionSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (node.Parent is InitializerExpressionSyntax)
        {
            // We have something like:
            //
            //      new Dictionary<int, string>
            //      {
            //          ...
            //          {
            //              ...
            //          },
            //          ...
            //      }
            //
            //  In this case, we want to collapse the "{ ... }," (including the comma).

            var nextToken = node.CloseBraceToken.GetNextToken();
            var end = nextToken.Kind() == SyntaxKind.CommaToken
                ? nextToken.Span.End
                : node.Span.End;

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(node.SpanStart, end),
                hintSpan: TextSpan.FromBounds(node.SpanStart, end),
                type: BlockTypes.Expression));
        }
        else
        {
            // Parent is something like:
            //
            //      new Dictionary<int, string> {
            //          ...
            //      }
            //
            // The collapsed textspan should be from the   {   to the   }
            // Start at node.SpanStart (the open brace) rather than previousToken.Span.End
            // so the editor's guideline heuristic picks the right column when the argument
            // list spans multiple lines.
            //
            // The hint span is the entire object creation for hover context.

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(node.SpanStart, node.Span.End),
                hintSpan: node.Parent.Span,
                type: BlockTypes.Expression));
        }
    }
}
