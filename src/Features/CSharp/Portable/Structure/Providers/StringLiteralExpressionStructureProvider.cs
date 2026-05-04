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
        if (node.IsKind(SyntaxKind.StringLiteralExpression) && !node.ContainsDiagnostics)
        {
            var type = GetStringLiteralType();
            if (type != null)
            {
                spans.Add(new BlockSpan(
                    type,
                    isCollapsible: true,
                    textSpan: node.Span,
                    hintSpan: node.Span,
                    autoCollapse: true,
                    isDefaultCollapsed: false));
            }
        }

        return;

        string? GetStringLiteralType()
        {
            // We explicitly pick non-structural here as we don't want 'structure' guides shown for raw string literals.
            // We already have a specialized tagger for those showing the user the left side of it.  So having a
            // structure guide as well is redundant.
            if (node.Token.Kind() is SyntaxKind.MultiLineRawStringLiteralToken or SyntaxKind.Utf8MultiLineRawStringLiteralToken)
                return BlockTypes.Nonstructural;

            if (node.Token.IsVerbatimStringLiteral())
            {
                var span = node.Span;
                var sourceText = node.SyntaxTree.GetText(cancellationToken);
                if (sourceText.Lines.GetLineFromPosition(span.Start).LineNumber !=
                    sourceText.Lines.GetLineFromPosition(span.End).LineNumber)
                {
                    return BlockTypes.Expression;
                }
            }

            return null;
        }
    }
}
