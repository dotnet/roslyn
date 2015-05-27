// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a project file loaded from disk.
    /// </summary>
    [Serializable]
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
        /// The parse and compilation options for this project.
        /// </summary>
        public BuildOptions BuildOptions { get; }

        /// <summary>
        /// The codepage for this project.
        /// </summary>
        public int CodePage { get; }

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

        /// <summary>
        /// other information represented as command line args
        /// </summary>
        public IReadOnlyList<string> CommandLineArgs { get; }

        public ProjectFileInfo(
            string outputPath,
            string assemblyName,
            BuildOptions buildOptions,
            int codePage,
            IEnumerable<DocumentFileInfo> documents,
            IEnumerable<DocumentFileInfo> additionalDocuments,
            IEnumerable<ProjectFileReference> projectReferences,
            IEnumerable<string> commandLineArgs)
        {
            this.OutputFilePath = outputPath;
            this.AssemblyName = assemblyName;
            this.BuildOptions = buildOptions;
            this.CodePage = codePage;
            this.Documents = documents.ToList().AsReadOnly();
            this.AdditionalDocuments = additionalDocuments.ToList().AsReadOnly();
            this.ProjectReferences = projectReferences.ToList().AsReadOnly();
            this.CommandLineArgs = commandLineArgs.ToList().AsReadOnly();
        }
    }
}
