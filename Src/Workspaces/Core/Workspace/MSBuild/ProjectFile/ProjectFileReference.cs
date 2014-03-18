// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a reference to another project file.
    /// </summary>
    internal sealed class ProjectFileReference
    {
        /// <summary>
        /// The unique GUID of the referenced project.
        /// </summary>
        public Guid Guid { get; private set; }

        /// <summary>
        /// The path on disk to the other project file. 
        /// This path may be relative to the referencing project's file or an absolute path.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// The alias assigned to this reference, if any.
        /// </summary>
        public string Alias { get; private set; }

        public ProjectFileReference(Guid guid, string path, string alias = null)
        {
            this.Guid = guid;
            this.Path = path;
            this.Alias = alias;
        }
    }
}