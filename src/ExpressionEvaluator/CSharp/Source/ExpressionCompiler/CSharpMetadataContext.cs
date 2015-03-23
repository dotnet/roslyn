// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        internal readonly Guid ModuleVersionId;

        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, CSharpCompilation compilation, Guid moduleVersionId)
        {
            this.MetadataBlocks = metadataBlocks;
            this.Compilation = compilation;
            this.EvaluationContext = null;
            this.ModuleVersionId = moduleVersionId;
        }

        internal CSharpMetadataContext(EvaluationContext evaluationContext)
        {
            this.MetadataBlocks = evaluationContext.MetadataBlocks;
            this.Compilation = evaluationContext.Compilation;
            this.EvaluationContext = evaluationContext;
            this.ModuleVersionId = evaluationContext.ModuleVersionId;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId)
        {
            return !this.MetadataBlocks.IsDefault &&
                this.ModuleVersionId == moduleVersionId &&
                this.MetadataBlocks.SequenceEqual(metadataBlocks);
        }
    }
}
