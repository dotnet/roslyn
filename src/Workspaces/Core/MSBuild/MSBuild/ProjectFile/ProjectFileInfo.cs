// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.MSBuild.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Provides information about a project that has been loaded from disk and
    /// built with MSBuild. If the project is multi-targeting, this represents
    /// the information from a single target framework.
    /// </summary>
    internal sealed class ProjectFileInfo
    {
        public bool IsEmpty { get; }

        /// <summary>
        /// The language of this project.
        /// </summary>
        public string Language { get; }

        /// <summary>
        /// The path to the project file for this project.
        /// </summary>
        public string? FilePath { get; }

        /// <summary>
        /// The path to the intermediate output file this project generates.
        /// </summary>
        public string? IntermediateOutputFilePath { get; }

        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        public string? OutputFilePath { get; }

        /// <summary>
        /// The path to the reference assembly output file this project generates.
        /// </summary>
        public string? OutputRefFilePath { get; }

        /// <summary>
        /// The default namespace of the project ("" if not defined, which means global namespace),
        /// or null if it is unknown or not applicable. 
        /// </summary>
        /// <remarks>
        /// Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
        /// by assigning the value of the project's root namespace to it. So various feature can choose to 
        /// use it for their own purpose.
        /// In the future, we might consider officially exposing "default namespace" for VB project 
        /// (e.g. through a "defaultnamespace" msbuild property)
        /// </remarks>
        public string? DefaultNamespace { get; }

        /// <summary>
        /// The target framework of this project.
        /// This takes the form of the 'short name' form used by NuGet (e.g. net46, netcoreapp2.0, etc.)
        /// </summary>
        public string? TargetFramework { get; }

        /// <summary>
        /// The target framework identifier of this project.
        /// Used to determine if a project is targeting .net core.
        /// </summary>
        public string? TargetFrameworkIdentifier { get; }

        /// <summary>
        /// The command line args used to compile the project.
        /// </summary>
        public ImmutableArray<string> CommandLineArgs { get; }

        /// <summary>
        /// The source documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> Documents { get; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> AdditionalDocuments { get; }

        /// <summary>
        /// The analyzer config documents.
        /// </summary>
        public ImmutableArray<DocumentFileInfo> AnalyzerConfigDocuments { get; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        public ImmutableArray<ProjectFileReference> ProjectReferences { get; }

        /// <summary>
        /// The error message produced when a failure occurred attempting to get the info. 
        /// If a failure occurred some or all of the information may be inaccurate or incomplete.
        /// </summary>
        public DiagnosticLog Log { get; }

        public override string ToString()
            => RoslynString.IsNullOrWhiteSpace(TargetFramework)
                ? FilePath ?? string.Empty
                : $"{FilePath} ({TargetFramework})";

        private ProjectFileInfo(
            bool isEmpty,
            string language,
            string? filePath,
            string? outputFilePath,
            string? outputRefFilePath,
            string? intermediateOutputFilePath,
            string? defaultNamespace,
            string? targetFramework,
            string? targetFrameworkIdentifier,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences,
            DiagnosticLog log)
        {
            RoslynDebug.Assert(filePath != null);

            this.IsEmpty = isEmpty;
            this.Language = language;
            this.FilePath = filePath;
            this.OutputFilePath = outputFilePath;
            this.OutputRefFilePath = outputRefFilePath;
            this.IntermediateOutputFilePath = intermediateOutputFilePath;
            this.DefaultNamespace = defaultNamespace;
            this.TargetFramework = targetFramework;
            this.TargetFrameworkIdentifier = targetFrameworkIdentifier;
            this.CommandLineArgs = commandLineArgs;
            this.Documents = documents;
            this.AdditionalDocuments = additionalDocuments;
            this.AnalyzerConfigDocuments = analyzerConfigDocuments;
            this.ProjectReferences = projectReferences;
            this.Log = log;
        }

        public static ProjectFileInfo Create(
            string language,
            string? filePath,
            string? outputFilePath,
            string? outputRefFilePath,
            string? intermediateOutputFilePath,
            string? defaultNamespace,
            string? targetFramework,
            string? targetFrameworkIdentifier,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences,
            DiagnosticLog log)
            => new(
                isEmpty: false,
                language,
                filePath,
                outputFilePath,
                outputRefFilePath,
                intermediateOutputFilePath,
                defaultNamespace,
                targetFramework,
                targetFrameworkIdentifier,
                commandLineArgs,
                documents,
                additionalDocuments,
                analyzerConfigDocuments,
                projectReferences,
                log);

        public static ProjectFileInfo CreateEmpty(string language, string? filePath, DiagnosticLog log)
            => new(
                isEmpty: true,
                language,
                filePath,
                outputFilePath: null,
                outputRefFilePath: null,
                intermediateOutputFilePath: null,
                defaultNamespace: null,
                targetFramework: null,
                targetFrameworkIdentifier: null,
                commandLineArgs: ImmutableArray<string>.Empty,
                documents: ImmutableArray<DocumentFileInfo>.Empty,
                additionalDocuments: ImmutableArray<DocumentFileInfo>.Empty,
                analyzerConfigDocuments: ImmutableArray<DocumentFileInfo>.Empty,
                projectReferences: ImmutableArray<ProjectFileReference>.Empty,
                log);
    }
}
