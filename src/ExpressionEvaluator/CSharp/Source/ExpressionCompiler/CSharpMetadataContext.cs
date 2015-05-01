// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ExpressionEvaluator;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal struct CSharpMetadataContext
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly ImmutableArray<Alias> Aliases;
        internal readonly CSharpCompilation Compilation;
        internal readonly EvaluationContext EvaluationContext;

        internal CSharpMetadataContext(
            ImmutableArray<MetadataBlock> metadataBlocks, 
            ImmutableArray<Alias> aliases,
            CSharpCompilation compilation)
        {
            this.MetadataBlocks = metadataBlocks;
            this.Aliases = aliases;
            this.Compilation = compilation;
            this.EvaluationContext = null;
        }

        internal CSharpMetadataContext(
            ImmutableArray<MetadataBlock> metadataBlocks,
            ImmutableArray<Alias> aliases, 
            EvaluationContext evaluationContext)
        {
            this.MetadataBlocks = metadataBlocks;
            this.Aliases = aliases;
            this.Compilation = evaluationContext.Compilation;
            this.EvaluationContext = evaluationContext;
        }

        internal bool Matches(ImmutableArray<MetadataBlock> metadataBlocks, ImmutableArray<Alias> aliases)
        {
            return !this.MetadataBlocks.IsDefault &&
                this.MetadataBlocks.SequenceEqual(metadataBlocks) &&
                !this.Aliases.IsDefault &&
                this.Aliases.SequenceEqual(aliases);
        }
    }
}
