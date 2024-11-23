// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Structure;

[NonCopyable]
internal readonly struct BlockStructureContext(SyntaxTree syntaxTree, BlockStructureOptions options, CancellationToken cancellationToken) : IDisposable
{
    // We keep our own ObjectPool of ArrayBuilders as we want to use ArrayBuilders for their ability to efficiently create ImmutableArrays, but don't
    // want the maximum capacity the default pool uses for dropping items from the pool.
    private static readonly ObjectPool<ArrayBuilder<BlockSpan>> _blockSpanArrayBuilderPool = new ObjectPool<ArrayBuilder<BlockSpan>>(() => new ArrayBuilder<BlockSpan>());

    public readonly ArrayBuilder<BlockSpan> Spans = _blockSpanArrayBuilderPool.Allocate();

    public readonly SyntaxTree SyntaxTree = syntaxTree;
    public readonly BlockStructureOptions Options = options;
    public readonly CancellationToken CancellationToken = cancellationToken;

    public void Dispose()
    {
        // Do not call Free on the builder as we are not using the default pool
        Spans.Clear();
        _blockSpanArrayBuilderPool.Free(Spans);
    }
}
