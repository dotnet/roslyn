// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves references to metadata specified in the source (#r directives).
    /// </summary>
    public abstract class MetadataReferenceResolver
    {
        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
        public abstract ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string baseFilePath, MetadataReferenceProperties properties);

        /// <summary>
        /// True to instruct the compiler to invoke <see cref="ResolveMissingAssembly(MetadataReference, AssemblyIdentity)"/> for each assembly reference that
        /// doesn't match any of the assemblies explicitly referenced by the <see cref="Compilation"/> (via <see cref="Compilation.ExternalReferences"/>, or #r directives.
        /// </summary>
        public virtual bool ResolveMissingAssemblies => false;

        /// <summary>
        /// Resolves a missing assembly reference.
        /// </summary>
        /// <param name="definition">The metadata definition (assembly or module) that declares assembly reference <paramref name="referenceIdentity"/> in its list of dependencies.</param>
        /// <param name="referenceIdentity">Identity of the assembly reference that couldn't be resolved against metadata references explicitly specified to in the compilation.</param>
        /// <returns>Resolved reference or null if the identity can't be resolved.</returns>
        public virtual PortableExecutableReference ResolveMissingAssembly(MetadataReference definition, AssemblyIdentity referenceIdentity) => null;
    }
}
