// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Structure
{
    internal sealed class BlockStructureContext
    {
        private readonly ImmutableArray<BlockSpan>.Builder _spans = ImmutableArray.CreateBuilder<BlockSpan>();

        public SyntaxTree SyntaxTree { get; }
        public BlockStructureOptions Options { get; }
        public CancellationToken CancellationToken { get; }

        internal ImmutableArray<BlockSpan> Spans => _spans.ToImmutable();

        public BlockStructureContext(SyntaxTree syntaxTree, BlockStructureOptions options, CancellationToken cancellationToken)
        {
            SyntaxTree = syntaxTree;
            Options = options;
            CancellationToken = cancellationToken;
        }

        public void AddBlockSpan(BlockSpan span)
            => _spans.Add(span);
    }
}
