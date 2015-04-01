// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal struct CSharpMetadataContext
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly CSharpCompilation Compilation;
        internal readonly EvaluationContext EvaluationContext;

        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, CSharpCompilation compilation)
        {
            this.MetadataBlocks = metadataBlocks;
            this.Compilation = compilation;
            this.EvaluationContext = null;
        }

        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, EvaluationContext evaluationContext)
        {
            this.MetadataBlocks = metadataBlocks;
            this.Compilation = evaluationContext.Compilation;
            this.EvaluationContext = evaluationContext;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks)
        {
            return !this.MetadataBlocks.IsDefault &&
                this.MetadataBlocks.SequenceEqual(metadataBlocks);
        }
    }
}
