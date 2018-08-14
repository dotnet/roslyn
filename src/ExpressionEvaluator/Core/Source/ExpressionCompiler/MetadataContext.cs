// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct MetadataContext<TAssemblyContext>
        where TAssemblyContext : struct
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly ImmutableDictionary<MetadataContextId, TAssemblyContext> AssemblyContexts;

        internal MetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, ImmutableDictionary<MetadataContextId, TAssemblyContext> assemblyContexts)
        {
            this.MetadataBlocks = metadataBlocks;
            this.AssemblyContexts = assemblyContexts;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks)
        {
            return !this.MetadataBlocks.IsDefault &&
                this.MetadataBlocks.SequenceEqual(metadataBlocks);
        }
    }
}
