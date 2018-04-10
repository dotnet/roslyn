// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // Wrapper around Guid to ensure callers have asked for the correct id
    // rather than simply using the ModuleVersionId (which is unnecessary
    // when the Compilation references all loaded assemblies).
    internal struct MetadataContextId : IEquatable<MetadataContextId>
    {
        internal readonly Guid ModuleVersionId;

        internal MetadataContextId(Guid moduleVersionId)
        {
            ModuleVersionId = moduleVersionId;
        }

        public bool Equals(MetadataContextId other)
        {
            return ModuleVersionId.Equals(other.ModuleVersionId);
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataContextId && this.Equals((MetadataContextId)obj);
        }

        public override int GetHashCode()
        {
            return ModuleVersionId.GetHashCode();
        }

        internal static MetadataContextId GetContextId(Guid moduleVersionId, MakeAssemblyReferencesKind kind)
        {
            switch (kind)
            {
                case MakeAssemblyReferencesKind.AllAssemblies:
                    return default;
                case MakeAssemblyReferencesKind.AllReferences:
                    return new MetadataContextId(moduleVersionId);
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }
    }

    internal struct MetadataContext<TAssemblyContext>
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
