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
    public readonly ArrayBuilder<BlockSpan> Spans = ArrayBuilder<BlockSpan>.GetInstance();

    public readonly SyntaxTree SyntaxTree = syntaxTree;
    public readonly BlockStructureOptions Options = options;
    public readonly CancellationToken CancellationToken = cancellationToken;

    public void Dispose()
        => Spans.Free();
}
