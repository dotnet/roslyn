﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure.MetadataAsSource
{
    internal abstract class AbstractMetadataAsSourceStructureProvider<TSyntaxNode> : AbstractSyntaxNodeStructureProvider<TSyntaxNode>
        where TSyntaxNode : SyntaxNode
    {
        protected override void CollectBlockSpans(
            TSyntaxNode node,
            ArrayBuilder<BlockSpan> spans,
            OptionSet options,
            CancellationToken cancellationToken)
        {
            var startToken = node.GetFirstToken();
            var endToken = GetEndToken(node);

            if (startToken.Kind() == SyntaxKind.None || endToken.Kind() == SyntaxKind.None)
            {
                // if valid tokens can't be found then a meaningful span can't be generated
                return;
            }

            var firstComment = startToken.LeadingTrivia.FirstOrNull(t => t.Kind() == SyntaxKind.SingleLineCommentTrivia);

            var startPosition = firstComment.HasValue ? firstComment.Value.SpanStart : startToken.SpanStart;
            var endPosition = endToken.SpanStart;

            // TODO (tomescht): Mark the regions to be collapsed by default.
            if (startPosition != endPosition)
            {
                var hintTextEndToken = GetHintTextEndToken(node);

                spans.Add(new BlockSpan(
                    isCollapsible: true,
                    type: BlockTypes.Comment,
                    textSpan: TextSpan.FromBounds(startPosition, endPosition),
                    hintSpan: TextSpan.FromBounds(startPosition, hintTextEndToken.Span.End),
                    bannerText: CSharpStructureHelpers.Ellipsis,
                    autoCollapse: true));
            }
        }

        protected override bool SupportedInWorkspaceKind(string kind)
        {
            return kind == WorkspaceKind.MetadataAsSource;
        }

        /// <summary>
        /// Returns the last token to be included in the regions hint text.
        /// </summary>
        /// <remarks>
        /// Note that the text of this token is included in the hint text, but not its
        /// trailing trivia. See also <seealso cref="GetEndToken"/>.
        /// </remarks>
        protected virtual SyntaxToken GetHintTextEndToken(TSyntaxNode node)
        {
            return node.GetLastToken();
        }

        /// <summary>
        /// Returns the token that marks the end of the collapsible region for the given node.
        /// </summary>
        /// <remarks>
        /// Note that the text of the returned token is *not* included in the region, but any
        /// leading trivia will be. This allows us to put the banner text for the collapsed
        /// region immediately before the type or member.
        /// See also <seealso cref="GetHintTextEndToken"/>.
        /// </remarks>
        protected abstract SyntaxToken GetEndToken(TSyntaxNode node);
    }
}
