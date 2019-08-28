// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    // Wrapper around Guid to ensure callers have asked for the correct id
    // rather than simply using the ModuleVersionId (which is unnecessary
    // when the Compilation references all loaded assemblies).
    internal readonly struct MetadataContextId : IEquatable<MetadataContextId>
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
}
