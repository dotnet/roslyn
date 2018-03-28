// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Structure
{
    internal class BlockStructure
    {
        public ImmutableArray<BlockSpan> Spans { get; }

        public BlockStructure(ImmutableArray<BlockSpan> spans)
        {
            Spans = spans;
        }
    }
}
