// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
