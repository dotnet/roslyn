// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class SwitchExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<SwitchExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        SwitchExpressionSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: TextSpan.FromBounds(node.SwitchKeyword.Span.End, node.CloseBraceToken.Span.End),
            hintSpan: node.Span,
            type: BlockTypes.Conditional));
    }
}
