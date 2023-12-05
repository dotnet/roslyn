// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Structure
{
    [NonCopyable]
    internal readonly struct BlockStructureContext : IDisposable
    {
        public readonly ArrayBuilder<BlockSpan> Spans = ArrayBuilder<BlockSpan>.GetInstance();

        public readonly SyntaxTree SyntaxTree;
        public readonly BlockStructureOptions Options;
        public readonly CancellationToken CancellationToken;

        public BlockStructureContext(SyntaxTree syntaxTree, BlockStructureOptions options, CancellationToken cancellationToken)
        {
            SyntaxTree = syntaxTree;
            Options = options;
            CancellationToken = cancellationToken;
        }

        public void Dispose()
            => Spans.Free();
    }
}
