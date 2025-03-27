// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            MetadataBlocks = metadataBlocks;
            AssemblyContexts = assemblyContexts;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks)
            => !MetadataBlocks.IsDefault &&
                MetadataBlocks.SequenceEqual(metadataBlocks);
    }
}
