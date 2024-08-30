// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class StringLiteralExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<LiteralExpressionSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        LiteralExpressionSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (node.IsKind(SyntaxKind.StringLiteralExpression) &&
            !node.ContainsDiagnostics &&
            CouldBeMultiLine())
        {
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: node.Span,
                hintSpan: node.Span,
                type: BlockTypes.Expression,
                autoCollapse: true,
                isDefaultCollapsed: false));
        }

        return;

        bool CouldBeMultiLine()
        {
            if (node.Token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
                return true;

            if (node.Token.IsVerbatimStringLiteral())
            {
                var span = node.Span;
                var sourceText = node.SyntaxTree.GetText(cancellationToken);
                return sourceText.Lines.GetLineFromPosition(span.Start).LineNumber !=
                       sourceText.Lines.GetLineFromPosition(span.End).LineNumber;
            }

            return false;
        }
    }
}
