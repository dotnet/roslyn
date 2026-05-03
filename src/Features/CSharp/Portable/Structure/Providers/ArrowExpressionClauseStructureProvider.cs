// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal sealed class ArrowExpressionClauseStructureProvider : AbstractSyntaxNodeStructureProvider<ArrowExpressionClauseSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        ArrowExpressionClauseSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var parent = node.GetRequiredParent();
        var end = GetEndPoint();
        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: TextSpan.FromBounds(previousToken.Span.End, end),
            hintSpan: TextSpan.FromBounds(parent.Span.Start, end),
            type: BlockTypes.Nonstructural,
            autoCollapse: parent.Kind() != SyntaxKind.LocalFunctionStatement));

        int GetEndPoint()
        {
            // If we have a directive that starts within the node we're collapsing, but ends outside of it, then we want
            // to collapse all the directives along with the node itself since we're collapsing the node.
            var endToken = parent.GetLastToken();
            var nextToken = endToken.GetNextToken();
            if (nextToken != default)
            {
                foreach (var trivia in nextToken.LeadingTrivia)
                {
                    if (trivia.IsDirective)
                    {
                        var directive = (DirectiveTriviaSyntax)trivia.GetStructure()!;
                        var matchingDirectives = directive.GetMatchingConditionalDirectives(cancellationToken);

                        // Check that:
                        // 1. The first directive is within the node we're collapsing.
                        // 2. All the directives end before the next construct starts.
                        //
                        // In that case, we want to collapse all the directives along with the node itself.
                        if (matchingDirectives.Length > 0 &&
                            matchingDirectives[0].Span.Start >= parent.Span.Start &&
                            matchingDirectives.All(d => d.Span.End <= nextToken.Span.Start))
                        {
                            var lastDirective = matchingDirectives.Last();
                            return lastDirective.Span.End;
                        }
                    }
                }
            }

            return parent.Span.End;
        }
    }
}
