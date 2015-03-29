// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class CSharpMetadataContext : MetadataContext
    {
        internal readonly CSharpCompilation Compilation;
        internal readonly EvaluationContext EvaluationContext;

        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, CSharpCompilation compilation)
            : base(metadataBlocks)
        {
            this.Compilation = compilation;
        }

        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, EvaluationContext evaluationContext)
            : base(metadataBlocks)
        {
            this.Compilation = evaluationContext.Compilation;
            this.EvaluationContext = evaluationContext;
        }
    }
}
