// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal sealed class StringLiteralExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<LiteralExpressionSyntax>
    {
        protected override void CollectBlockSpans(LiteralExpressionSyntax node, ArrayBuilder<BlockSpan> spans, OptionSet options, CancellationToken cancellationToken)
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
