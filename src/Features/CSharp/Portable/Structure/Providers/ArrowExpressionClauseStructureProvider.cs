// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure;

internal class ArrowExpressionClauseStructureProvider : AbstractSyntaxNodeStructureProvider<ArrowExpressionClauseSyntax>
{
    protected override void CollectBlockSpans(
        SyntaxToken previousToken,
        ArrowExpressionClauseSyntax node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        var parent = node.Parent;
        var end = GetEndPointIfFollowedByDirectives() ?? parent.Span.End;
        spans.Add(new BlockSpan(
            isCollapsible: true,
            textSpan: TextSpan.FromBounds(previousToken.Span.End, end),
            hintSpan: TextSpan.FromBounds(parent.Span.Start, end),
            type: BlockTypes.Nonstructural,
            autoCollapse: parent.Kind() != SyntaxKind.LocalFunctionStatement));

        int? GetEndPointIfFollowedByDirectives()
        {
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
                        if (matchingDirectives.Length > 0 &&
                            matchingDirectives[0].Span.Start >= node.Span.Start &&
                            matchingDirectives.All(d => d.Span.End <= nextToken.Span.Start))
                        {
                            // The directives start within the node we're collapsing, but end outside of it (since we
                            // have at least one on the leading trivia of the next token).  Collapse all the directives
                            // along with the node itself since we're collapsing the node.
                            var lastDirective = matchingDirectives.Last();
                            return lastDirective.Span.End;
                        }
                    }
                }
            }

            return null;
        }
    }
}
