// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class MetadataContext : DkmDataItem
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;

        internal MetadataContext(ImmutableArray<MetadataBlock> metadataBlocks)
        {
            this.MetadataBlocks = metadataBlocks;
        }
    }
}
