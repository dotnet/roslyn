// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class InitializerExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<InitializerExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            InitializerExpressionSyntax node,
            ref TemporaryArray<BlockSpan> spans,
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
                // The collapsed textspan should be from the   >   to the   }
                //
                // However, the hint span should be the entire object creation.

                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(previousToken.Span.End, node.Span.End),
                    hintSpan: node.Parent.Span,
                    type: BlockTypes.Expression));
            }
        }
    }
}
