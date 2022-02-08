// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class ArrowExpressionClauseStructureProvider : AbstractSyntaxNodeStructureProvider<ArrowExpressionClauseSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            ArrowExpressionClauseSyntax node,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(previousToken.Span.End, node.Parent.Span.End),
                hintSpan: node.Parent.Span,
                type: BlockTypes.Nonstructural,
                autoCollapse: !node.IsParentKind(SyntaxKind.LocalFunctionStatement)));
        }
    }
}
