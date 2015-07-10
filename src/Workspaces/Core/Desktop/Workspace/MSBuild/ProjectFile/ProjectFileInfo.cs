// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a project file loaded from disk.
    /// </summary>
    internal sealed class ProjectFileInfo
    {
        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        public string OutputFilePath { get; }

        /// <summary>
        /// The assembly name of the output.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// The command line args used to compile the project.
        /// </summary>
        public IReadOnlyList<string> CommandLineArgs { get; }

        /// <summary>
        /// The source documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> Documents { get; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public IReadOnlyList<DocumentFileInfo> AdditionalDocuments { get; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public IReadOnlyList<ProjectFileReference> ProjectReferences { get; }

        public ProjectFileInfo(
            string outputPath,
            string assemblyName,
            IEnumerable<string> commandLineArgs,
            IEnumerable<DocumentFileInfo> documents,
            IEnumerable<DocumentFileInfo> additionalDocuments,
            IEnumerable<ProjectFileReference> projectReferences)
        {
            this.OutputFilePath = outputPath;
            this.AssemblyName = assemblyName;
            this.CommandLineArgs = commandLineArgs.ToImmutableArrayOrEmpty();
            this.Documents = documents.ToImmutableReadOnlyListOrEmpty();
            this.AdditionalDocuments = additionalDocuments.ToImmutableArrayOrEmpty();
            this.ProjectReferences = projectReferences.ToImmutableReadOnlyListOrEmpty();
        }
    }
}
