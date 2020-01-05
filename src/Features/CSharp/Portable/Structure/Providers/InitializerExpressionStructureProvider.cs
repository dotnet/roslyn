// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class InitializerExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<InitializerExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            InitializerExpressionSyntax node,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
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

                var previousToken = node.OpenBraceToken.GetPreviousToken();
                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    textSpan: TextSpan.FromBounds(previousToken.Span.End, node.Span.End),
                    hintSpan: node.Parent.Span,
                    type: BlockTypes.Expression));
            }
        }
    }
}
