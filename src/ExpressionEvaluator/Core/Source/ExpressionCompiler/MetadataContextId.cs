// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

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

        internal static MetadataContextId GetContextId(ModuleId moduleId, MakeAssemblyReferencesKind kind)
        {
            return kind switch
            {
                MakeAssemblyReferencesKind.AllAssemblies => default,
                MakeAssemblyReferencesKind.AllReferences => new MetadataContextId(moduleId.Id),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };
        }
    }
}
