// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockStructureContext
    {
        private readonly ImmutableArray<BlockSpan>.Builder _spans = ImmutableArray.CreateBuilder<BlockSpan>();

        public SyntaxTree SyntaxTree { get; }
        public BlockStructureOptionProvider OptionProvider { get; }
        public CancellationToken CancellationToken { get; }

        internal ImmutableArray<BlockSpan> Spans => _spans.ToImmutable();

        public BlockStructureContext(SyntaxTree syntaxTree, BlockStructureOptionProvider optionProvider, CancellationToken cancellationToken)
        {
            SyntaxTree = syntaxTree;
            OptionProvider = optionProvider;
            CancellationToken = cancellationToken;
        }

        public void AddBlockSpan(BlockSpan span)
            => _spans.Add(span);
    }
}
