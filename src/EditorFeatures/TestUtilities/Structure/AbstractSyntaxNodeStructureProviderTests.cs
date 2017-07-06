// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Structure
{
    public abstract class AbstractSyntaxNodeStructureProviderTests<TSyntaxNode> : AbstractSyntaxStructureProviderTests
        where TSyntaxNode : SyntaxNode
    {
        internal abstract AbstractSyntaxStructureProvider CreateProvider();

        internal sealed override async Task<ImmutableArray<BlockSpan>> GetBlockSpansWorkerAsync(Document document, int position)
        {
            var root = await document.GetSyntaxRootAsync(CancellationToken.None);
            var token = root.FindToken(position, findInsideTrivia: true);
            var node = token.Parent.FirstAncestorOrSelf<TSyntaxNode>();
            Assert.NotNull(node);

            // We prefer ancestor nodes if the position is on the edge of the located node's span.
            while (node.Parent is TSyntaxNode)
            {
                if ((position == node.SpanStart && position == node.Parent.SpanStart) ||
                    (position == node.Span.End && position == node.Parent.Span.End))
                {
                    node = (TSyntaxNode)node.Parent;
                }
                else
                {
                    break;
                }
            }

            var outliner = CreateProvider();
            var actualRegions = ArrayBuilder<BlockSpan>.GetInstance();
            outliner.CollectBlockSpans(document, node, actualRegions, CancellationToken.None);

            // TODO: Determine why we get null outlining spans.
            return actualRegions.ToImmutableAndFree();
        }
    }
}
