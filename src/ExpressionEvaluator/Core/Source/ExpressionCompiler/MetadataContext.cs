// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct MetadataContext<TAssemblyContext>
        where TAssemblyContext : struct
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly Guid ModuleVersionId;
        internal readonly TAssemblyContext AssemblyContext;

        internal MetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId, TAssemblyContext assemblyContext)
        {
            Debug.Assert(moduleVersionId != default);
            this.MetadataBlocks = metadataBlocks;
            this.ModuleVersionId = moduleVersionId;
            this.AssemblyContext = assemblyContext;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId)
        {
            return !this.MetadataBlocks.IsDefault &&
                this.ModuleVersionId == moduleVersionId &&
                this.MetadataBlocks.SequenceEqual(metadataBlocks);
        }
    }
}
