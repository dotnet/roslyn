// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    // Remove this class and use MetadataContext<CSharpCompilation, EvaluationContext> directly.
    internal sealed class CSharpMetadataContext : MetadataContext<CSharpCompilation, EvaluationContext>
    {
        internal CSharpMetadataContext(CSharpCompilation compilation, EvaluationContext evaluationContext = null) :
            base(compilation, evaluationContext)
        {
        }

        // TODO: Remove metadataBlocks parameter.
        internal CSharpMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, EvaluationContext evaluationContext) :
            base(evaluationContext.Compilation, evaluationContext)
        {
        }
    }
}
