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
    internal class AnonymousObjectCreationExpressionStructureProvider : AbstractSyntaxNodeStructureProvider<AnonymousObjectCreationExpressionSyntax>
    {
        protected override void CollectBlockSpans(
            SyntaxToken previousToken,
            AnonymousObjectCreationExpressionSyntax node,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            // Node is something like:
            //
            //      new
            //      {
            //          Field1 = ...,
            //          Field2 = ...,
            //          ...
            //      }
            //
            // The collapsed textspan should be from the end of new keyword to the end of the whole node
            // And the hint span should be the entire node

            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: TextSpan.FromBounds(node.NewKeyword.Span.End, node.Span.End),
                hintSpan: node.Span,
                type: BlockTypes.Expression));
        }
    }
}
