// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal sealed class StringLiteralExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<LiteralExpressionSyntax>
    {
        protected override void CollectBlockSpans(LiteralExpressionSyntax node, ArrayBuilder<BlockSpan> spans, bool isMetadataAsSource, OptionSet options, CancellationToken cancellationToken)
        {
            if (node.IsKind(SyntaxKind.StringLiteralExpression) &&
                !node.ContainsDiagnostics)
            {
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
}
