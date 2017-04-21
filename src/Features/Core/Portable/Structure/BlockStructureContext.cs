// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockStructureContext
    {
        private readonly ImmutableArray<BlockSpan>.Builder _spans = ImmutableArray.CreateBuilder<BlockSpan>();

        public Document Document { get; }
        public CancellationToken CancellationToken { get; }

        internal ImmutableArray<BlockSpan> Spans => _spans.ToImmutable();

        public BlockStructureContext(Document document, CancellationToken cancellationToken)
        {
            Document = document;
            CancellationToken = cancellationToken;
        }

        public void AddBlockSpan(BlockSpan span)
        {
            _spans.Add(span);
        }
    }
}
