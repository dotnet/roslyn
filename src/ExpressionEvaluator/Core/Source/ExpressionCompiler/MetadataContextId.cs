// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            => ModuleVersionId.Equals(other.ModuleVersionId);

        public override bool Equals(object obj)
            => obj is MetadataContextId && Equals((MetadataContextId)obj);

        public override int GetHashCode()
            => ModuleVersionId.GetHashCode();

        internal static MetadataContextId GetContextId(Guid moduleVersionId, MakeAssemblyReferencesKind kind)
        {
            return kind switch
            {
                MakeAssemblyReferencesKind.AllAssemblies => default,
                MakeAssemblyReferencesKind.AllReferences => new MetadataContextId(moduleVersionId),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }
}
