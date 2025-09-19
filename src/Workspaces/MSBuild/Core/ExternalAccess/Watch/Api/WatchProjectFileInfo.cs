// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.ExternalAccess.Watch.Api;

/// <summary>
/// Provides information about a project that has been loaded from disk and
/// built with MSBuild. If the project is multi-targeting, this represents
/// the information from a single target framework.
/// </summary>
internal readonly struct WatchProjectFileInfo
{
    internal ProjectFileInfo UnderlyingObject { get; }

    internal WatchProjectFileInfo(ProjectFileInfo underlyingObject)
    {
        UnderlyingObject = underlyingObject;
    }

    public WatchProjectFileInfo(
        bool isEmpty, string language, string? filePath, string? outputFilePath, string? outputRefFilePath,
        string? generatedFilesOutputDirectory, string? intermediateOutputFilePath, string? defaultNamespace,
        string? targetFramework, string? targetFrameworkIdentifier, string? targetFrameworkVersion, string? projectAssetsFilePath,
        ImmutableArray<string> commandLineArgs, ImmutableArray<WatchDocumentFileInfo> documents,
        ImmutableArray<WatchDocumentFileInfo> additionalDocuments, ImmutableArray<WatchDocumentFileInfo> analyzerConfigDocuments,
        ImmutableArray<WatchProjectFileReference> projectReferences, ImmutableArray<WatchPackageReference> packageReferences,
        ImmutableArray<string> projectCapabilities, ImmutableArray<string> contentFilePaths, ImmutableArray<WatchFileGlobs> fileGlobs)
        : this(new ProjectFileInfo()
        {
            IsEmpty = isEmpty,
            Language = language,
            FilePath = filePath,
            OutputFilePath = outputFilePath,
            OutputRefFilePath = outputRefFilePath,
            GeneratedFilesOutputDirectory = generatedFilesOutputDirectory,
            IntermediateOutputFilePath = intermediateOutputFilePath,
            DefaultNamespace = defaultNamespace,
            TargetFramework = targetFramework,
            TargetFrameworkIdentifier = targetFrameworkIdentifier,
            TargetFrameworkVersion = targetFrameworkVersion,
            ProjectAssetsFilePath = projectAssetsFilePath,
            CommandLineArgs = commandLineArgs,
            Documents = documents.SelectAsArray(static x => x.UnderlyingObject),
            AdditionalDocuments = additionalDocuments.SelectAsArray(static x => x.UnderlyingObject),
            AnalyzerConfigDocuments = analyzerConfigDocuments.SelectAsArray(static x => x.UnderlyingObject),
            ProjectReferences = projectReferences.SelectAsArray(static x => x.UnderlyingObject),
            PackageReferences = packageReferences.SelectAsArray(static x => x.UnderlyingObject),
            ProjectCapabilities = projectCapabilities,
            ContentFilePaths = contentFilePaths,
            FileGlobs = fileGlobs.SelectAsArray(static x => x.UnderlyingObject)
        })
    {
    }

    public WatchProjectFileInfo(
        bool isEmpty, string language, string? filePath, ImmutableArray<string> commandLineArgs,
        ImmutableArray<WatchDocumentFileInfo> documents, ImmutableArray<WatchDocumentFileInfo> additionalDocuments,
        ImmutableArray<WatchDocumentFileInfo> analyzerConfigDocuments, ImmutableArray<WatchProjectFileReference> projectReferences,
        ImmutableArray<WatchPackageReference> packageReferences, ImmutableArray<string> projectCapabilities,
        ImmutableArray<string> contentFilePaths, ImmutableArray<WatchFileGlobs> fileGlobs)
        : this(new ProjectFileInfo()
        {
            IsEmpty = isEmpty,
            Language = language,
            FilePath = filePath,
            CommandLineArgs = commandLineArgs,
            Documents = documents.SelectAsArray(static x => x.UnderlyingObject),
            AdditionalDocuments = additionalDocuments.SelectAsArray(static x => x.UnderlyingObject),
            AnalyzerConfigDocuments = analyzerConfigDocuments.SelectAsArray(static x => x.UnderlyingObject),
            ProjectReferences = projectReferences.SelectAsArray(static x => x.UnderlyingObject),
            PackageReferences = packageReferences.SelectAsArray(static x => x.UnderlyingObject),
            ProjectCapabilities = projectCapabilities,
            ContentFilePaths = contentFilePaths,
            FileGlobs = fileGlobs.SelectAsArray(static x => x.UnderlyingObject)
        })
    {
    }

    public bool IsEmpty => UnderlyingObject.IsEmpty;

    /// <summary>
    /// The language of this project.
    /// </summary>
    public string Language => UnderlyingObject.Language;

    /// <summary>
    /// The path to the project file for this project.
    /// </summary>
    public string? FilePath => UnderlyingObject.FilePath;

    /// <summary>
    /// The path to the output file this project generates.
    /// </summary>
    public string? OutputFilePath => UnderlyingObject.OutputFilePath;

    /// <summary>
    /// The path to the reference assembly output file this project generates.
    /// </summary>
    public string? OutputRefFilePath => UnderlyingObject.OutputRefFilePath;

    /// <summary>
    /// The path to the intermediate output file this project generates.
    /// </summary>
    public string? IntermediateOutputFilePath => UnderlyingObject.IntermediateOutputFilePath;

    public string? GeneratedFilesOutputDirectory => UnderlyingObject.GeneratedFilesOutputDirectory;

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
    public string? DefaultNamespace => UnderlyingObject.DefaultNamespace;

    /// <summary>
    /// The target framework of this project.
    /// This takes the form of the 'short name' form used by NuGet (e.g. net46, netcoreapp2.0, etc.)
    /// </summary>
    public string? TargetFramework => UnderlyingObject.TargetFramework;

    /// <summary>
    /// The target framework identifier of this project.
    /// Used to determine if a project is targeting .net core.
    /// </summary>
    public string? TargetFrameworkIdentifier => UnderlyingObject.TargetFrameworkIdentifier;

    /// <summary>
    /// The command line args used to compile the project.
    /// </summary>
    public ImmutableArray<string> CommandLineArgs => UnderlyingObject.CommandLineArgs;

    /// <summary>
    /// The source documents.
    /// </summary>
    public ImmutableArray<WatchDocumentFileInfo> Documents { get; init; }

    /// <summary>
    /// The additional documents.
    /// </summary>
    public ImmutableArray<WatchDocumentFileInfo> AdditionalDocuments { get; init; }

    /// <summary>
    /// The analyzer config documents.
    /// </summary>
    public ImmutableArray<WatchDocumentFileInfo> AnalyzerConfigDocuments { get; init; }

    /// <summary>
    /// References to other projects.
    /// </summary>
    public ImmutableArray<WatchProjectFileReference> ProjectReferences { get; init; }

    /// <summary>
    /// The msbuild project capabilities.
    /// </summary>
    public ImmutableArray<string> ProjectCapabilities => UnderlyingObject.ProjectCapabilities;

    /// <summary>
    /// The paths to content files included in the project.
    /// </summary>
    public ImmutableArray<string> ContentFilePaths => UnderlyingObject.ContentFilePaths;

    /// <summary>
    /// The path to the project.assets.json path in obj/.
    /// </summary>
    public string? ProjectAssetsFilePath => ProjectAssetsFilePath;

    /// <summary>
    /// Any package references defined on the project.
    /// </summary>
    public ImmutableArray<WatchPackageReference> PackageReferences { get; init; }

    /// <summary>
    /// Target framework version (for .net framework projects)
    /// </summary>
    public string? TargetFrameworkVersion => UnderlyingObject.TargetFrameworkVersion;

    public ImmutableArray<WatchFileGlobs> FileGlobs { get; init; }

    public override string ToString()
        => RoslynString.IsNullOrWhiteSpace(TargetFramework)
            ? FilePath ?? string.Empty
            : $"{FilePath} ({TargetFramework})";

    public static WatchProjectFileInfo CreateEmpty(string language, string? filePath)
        => new(
            isEmpty: true, language, filePath, commandLineArgs: [], documents: [],
            additionalDocuments: [], analyzerConfigDocuments: [], projectReferences: [],
            packageReferences: [], projectCapabilities: [], contentFilePaths: [], fileGlobs: []);
}
