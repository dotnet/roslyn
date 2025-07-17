// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class CollectionExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<CollectionExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        CollectionExpressionSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (node.Parent?.Parent is CollectionExpressionSyntax)
        {
            // We have something like:
            //
            //      List<List<int>> v = 
            //      [
            //          ...
            //          [
            //              ...
            //          ],
            //          ...
            //      ];
            //
            //  In this case, we want to collapse the "[ ... ]," (including the comma).

            var nextToken = node.CloseBracketToken.GetNextToken();
            var end = nextToken.Kind() == SyntaxKind.CommaToken
                ? nextToken.Span.End
                : node.Span.End;

            var textSpan = TextSpan.FromBounds(node.SpanStart, end);

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: textSpan,
                hintSpan: textSpan,
                type: BlockTypes.Expression));
        }
        else
        {
            // Parent is something like:
            //
            //      List<int> v = 
            //      [
            //          ...
            //      ];
            //
            // The collapsed textspan should be from the   =   to the   ]

            var textSpan = TextSpan.FromBounds(previousToken.Span.End, node.Span.End);

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: textSpan,
                hintSpan: textSpan,
                type: BlockTypes.Expression));
        }
    }
}
