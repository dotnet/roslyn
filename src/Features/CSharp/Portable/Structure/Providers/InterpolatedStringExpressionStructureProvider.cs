// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal sealed class InterpolatedStringExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<InterpolatedStringExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            InterpolatedStringExpressionSyntax node,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            if (node.StringStartToken.IsMissing ||
                node.StringEndToken.IsMissing)
            {
                return;
            }

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: node.Span,
                hintSpan: node.Span,
                type: BlockTypes.Expression,
                autoCollapse: true,
                isDefaultCollapsed: false));
        }
    }
}
