// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Resolves metadata references specified in source code (#r directives).
    /// </summary>
    public abstract class ScriptMetadataReferenceResolver : MetadataReferenceResolver
    {
        /// <summary>
        /// Search paths used when resolving metadata references.
        /// </summary>
        /// <remarks>
        /// All search paths are absolute.
        /// </remarks>
        public abstract ImmutableArray<string> SearchPaths { get; }

        /// <summary>
        /// Directory used for resolution of relative paths.
        /// A full directory path or null if not available.
        /// </summary>
        public abstract string BaseDirectory { get; }

        public abstract ScriptMetadataReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths);
        public abstract ScriptMetadataReferenceResolver WithBaseDirectory(string baseDirectory);
    }
}
