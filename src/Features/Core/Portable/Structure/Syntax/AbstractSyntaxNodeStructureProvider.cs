// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Structure;

internal abstract class AbstractSyntaxNodeStructureProvider<TSyntaxNode> : AbstractSyntaxStructureProvider
    where TSyntaxNode : SyntaxNode
{
    public sealed override void CollectBlockSpans(
        SyntaxTrivia trivia,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public sealed override void CollectBlockSpans(
        SyntaxToken previousToken,
        SyntaxNode node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken)
    {
        if (node is TSyntaxNode tSyntax)
        {
            CollectBlockSpans(previousToken, tSyntax, spans, options, cancellationToken);
        }
    }

    protected abstract void CollectBlockSpans(
        SyntaxToken previousToken,
        TSyntaxNode node,
        ArrayBuilder<BlockSpan> spans,
        BlockStructureOptions options,
        CancellationToken cancellationToken);
}
