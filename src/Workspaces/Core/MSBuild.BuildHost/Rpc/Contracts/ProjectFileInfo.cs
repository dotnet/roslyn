// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Provides information about a project that has been loaded from disk and
    /// built with MSBuild. If the project is multi-targeting, this represents
    /// the information from a single target framework.
    /// </summary>
    [DataContract]
    internal sealed class ProjectFileInfo
    {
        [DataMember(Order = 0)]
        public bool IsEmpty { get; }

        /// <summary>
        /// The language of this project.
        /// </summary>
        [DataMember(Order = 1)]
        public string Language { get; }

        /// <summary>
        /// The path to the project file for this project.
        /// </summary>
        [DataMember(Order = 2)]
        public string? FilePath { get; }

        /// <summary>
        /// The path to the output file this project generates.
        /// </summary>
        [DataMember(Order = 3)]
        public string? OutputFilePath { get; }

        /// <summary>
        /// The path to the reference assembly output file this project generates.
        /// </summary>
        [DataMember(Order = 4)]
        public string? OutputRefFilePath { get; }

        /// <summary>
        /// The path to the intermediate output file this project generates.
        /// </summary>
        [DataMember(Order = 5)]
        public string? IntermediateOutputFilePath { get; }

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
        [DataMember(Order = 6)]
        public string? DefaultNamespace { get; }

        /// <summary>
        /// The target framework of this project.
        /// This takes the form of the 'short name' form used by NuGet (e.g. net46, netcoreapp2.0, etc.)
        /// </summary>
        [DataMember(Order = 7)]
        public string? TargetFramework { get; }

        /// <summary>
        /// The target framework identifier of this project.
        /// Used to determine if a project is targeting .net core.
        /// </summary>
        [DataMember(Order = 8)]
        public string? TargetFrameworkIdentifier { get; }

        /// <summary>
        /// The command line args used to compile the project.
        /// </summary>
        [DataMember(Order = 9)]
        public ImmutableArray<string> CommandLineArgs { get; }

        /// <summary>
        /// The source documents.
        /// </summary>
        [DataMember(Order = 10)]
        public ImmutableArray<DocumentFileInfo> Documents { get; }

        /// <summary>
        /// The additional documents.
        /// </summary>
        [DataMember(Order = 11)]
        public ImmutableArray<DocumentFileInfo> AdditionalDocuments { get; }

        /// <summary>
        /// The analyzer config documents.
        /// </summary>
        [DataMember(Order = 12)]
        public ImmutableArray<DocumentFileInfo> AnalyzerConfigDocuments { get; }

        /// <summary>
        /// References to other projects.
        /// </summary>
        [DataMember(Order = 13)]
        public ImmutableArray<ProjectFileReference> ProjectReferences { get; }

        /// <summary>
        /// The msbuild project capabilities.
        /// </summary>
        [DataMember(Order = 14)]
        public ImmutableArray<string> ProjectCapabilities { get; }

        /// <summary>
        /// The paths to content files included in the project.
        /// </summary>
        [DataMember(Order = 15)]
        public ImmutableArray<string> ContentFilePaths { get; }

        /// <summary>
        /// The path to the project.assets.json path in obj/.
        /// </summary>
        [DataMember(Order = 16)]
        public string? ProjectAssetsFilePath { get; }

        /// <summary>
        /// Any package references defined on the project.
        /// </summary>
        [DataMember(Order = 17)]
        public ImmutableArray<PackageReference> PackageReferences { get; }

        public override string ToString()
            => RoslynString.IsNullOrWhiteSpace(TargetFramework)
                ? FilePath ?? string.Empty
                : $"{FilePath} ({TargetFramework})";

        public ProjectFileInfo(
            bool isEmpty,
            string language,
            string? filePath,
            string? outputFilePath,
            string? outputRefFilePath,
            string? intermediateOutputFilePath,
            string? defaultNamespace,
            string? targetFramework,
            string? targetFrameworkIdentifier,
            string? projectAssetsFilePath,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences,
            ImmutableArray<PackageReference> packageReferences,
            ImmutableArray<string> projectCapabilities,
            ImmutableArray<string> contentFilePaths)
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
            this.ProjectAssetsFilePath = projectAssetsFilePath;
            this.CommandLineArgs = commandLineArgs;
            this.Documents = documents;
            this.AdditionalDocuments = additionalDocuments;
            this.AnalyzerConfigDocuments = analyzerConfigDocuments;
            this.ProjectReferences = projectReferences;
            this.PackageReferences = packageReferences;
            this.ProjectCapabilities = projectCapabilities;
            this.ContentFilePaths = contentFilePaths;
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
            string? projectAssetsFilePath,
            ImmutableArray<string> commandLineArgs,
            ImmutableArray<DocumentFileInfo> documents,
            ImmutableArray<DocumentFileInfo> additionalDocuments,
            ImmutableArray<DocumentFileInfo> analyzerConfigDocuments,
            ImmutableArray<ProjectFileReference> projectReferences,
            ImmutableArray<PackageReference> packageReferences,
            ImmutableArray<string> projectCapabilities,
            ImmutableArray<string> contentFilePaths)
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
                projectAssetsFilePath,
                commandLineArgs,
                documents,
                additionalDocuments,
                analyzerConfigDocuments,
                projectReferences,
                packageReferences,
                projectCapabilities,
                contentFilePaths);

        public static ProjectFileInfo CreateEmpty(string language, string? filePath)
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
                projectAssetsFilePath: null,
                commandLineArgs: [],
                documents: [],
                additionalDocuments: [],
                analyzerConfigDocuments: [],
                projectReferences: [],
                packageReferences: [],
                projectCapabilities: [],
                contentFilePaths: []);
    }
}
