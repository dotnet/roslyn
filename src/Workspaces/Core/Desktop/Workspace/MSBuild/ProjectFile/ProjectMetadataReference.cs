// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a reference to another project file.
    /// </summary>
    [Serializable]
    internal sealed class ProjectMetadataReference
    {
        /// <summary>
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The aliases assigned to this reference, if any.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        public bool EmbedInteropTypes { get; }

        public ProjectMetadataReference(string path, IEnumerable<string> aliases, bool embedInteropTypes)
        {
            this.Path = path;
            this.Aliases = aliases.ToList().AsReadOnly();
            this.EmbedInteropTypes = embedInteropTypes;
        }
    }
}