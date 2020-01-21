// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a reference to another project file.
    /// </summary>
    internal sealed class ProjectFileReference
    {
        /// <summary>
        /// The path on disk to the other project file. 
        /// This path may be relative to the referencing project's file or an absolute path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The aliases assigned to this reference, if any.
        /// </summary>
        public ImmutableArray<string> Aliases { get; }

        public ProjectFileReference(string path, ImmutableArray<string> aliases)
        {
            Debug.Assert(!aliases.IsDefault);

            this.Path = path;
            this.Aliases = aliases;
        }
    }
}
