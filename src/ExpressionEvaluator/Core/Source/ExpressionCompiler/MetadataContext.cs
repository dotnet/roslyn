// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Debugger;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class MetadataContext<TCompilation, TEvaluationContext>
        where TCompilation : Compilation
        where TEvaluationContext : EvaluationContextBase
    {
        internal readonly TCompilation Compilation;
        internal readonly TEvaluationContext EvaluationContext;

        internal MetadataContext(TCompilation compilation, TEvaluationContext evaluationContext)
        {
            this.Compilation = compilation;
            this.EvaluationContext = evaluationContext;
        }
    }

    internal sealed class AppDomainMetadataContext<TCompilation, TEvaluationContext> : DkmDataItem
        where TCompilation : Compilation
        where TEvaluationContext : EvaluationContextBase
    {
        internal readonly ImmutableArray<MetadataBlock> MetadataBlocks;
        internal readonly Guid ModuleVersionId;
        internal readonly MetadataContext<TCompilation, TEvaluationContext> AssemblyContext;

        internal AppDomainMetadataContext(ImmutableArray<MetadataBlock> metadataBlocks, Guid moduleVersionId, MetadataContext<TCompilation, TEvaluationContext> assemblyContext)
        {
            Debug.Assert(moduleVersionId != default);
            this.MetadataBlocks = metadataBlocks;
            this.ModuleVersionId = moduleVersionId;
            this.AssemblyContext = assemblyContext;
        }
    }

    internal static class MetadataContextExtensions
    {
        internal static bool Matches<TCompilation, TEvaluationContext>(this AppDomainMetadataContext<TCompilation, TEvaluationContext> previousOpt, ImmutableArray<MetadataBlock> metadataBlocks)
            where TCompilation : Compilation
            where TEvaluationContext : EvaluationContextBase
        {
            return previousOpt != null && previousOpt.MetadataBlocks.SequenceEqual(metadataBlocks);
        }
    }
}
