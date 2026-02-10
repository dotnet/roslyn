// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Provides information about a project that has been loaded from disk and
/// built with MSBuild. If the project is multi-targeting, this represents
/// the information from a single target framework.
/// </summary>
[DataContract]
internal sealed class ProjectFileInfo
{
    [DataMember]
    public bool IsEmpty { get; init; }

    /// <summary>
    /// The language of this project.
    /// </summary>
    [DataMember]
    public required string Language { get; init; }

    /// <summary>
    /// The path to the project file for this project.
    /// </summary>
    [DataMember]
    public string? FilePath { get; init; }

    /// <summary>
    /// The path to the output file this project generates.
    /// </summary>
    [DataMember]
    public string? OutputFilePath { get; init; }

    /// <summary>
    /// The path to the reference assembly output file this project generates.
    /// </summary>
    [DataMember]
    public string? OutputRefFilePath { get; init; }

    /// <summary>
    /// The path to the intermediate output file this project generates.
    /// </summary>
    [DataMember]
    public string? IntermediateOutputFilePath { get; init; }

    [DataMember]
    public string? GeneratedFilesOutputDirectory { get; init; }

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
    [DataMember]
    public string? DefaultNamespace { get; init; }

    /// <summary>
    /// The target framework of this project.
    /// This takes the form of the 'short name' form used by NuGet (e.g. net46, netcoreapp2.0, etc.)
    /// </summary>
    [DataMember]
    public string? TargetFramework { get; init; }

    /// <summary>
    /// The target framework identifier of this project.
    /// Used to determine if a project is targeting .net core.
    /// </summary>
    [DataMember]
    public string? TargetFrameworkIdentifier { get; init; }

    /// <summary>
    /// The command line args used to compile the project.
    /// </summary>
    [DataMember]
    public string[] CommandLineArgs { get; init; } = [];

    /// <summary>
    /// The source documents.
    /// </summary>
    [DataMember]
    public DocumentFileInfo[] Documents { get; init; } = [];

    /// <summary>
    /// The additional documents.
    /// </summary>
    [DataMember]
    public DocumentFileInfo[] AdditionalDocuments { get; init; } = [];

    /// <summary>
    /// The analyzer config documents.
    /// </summary>
    [DataMember]
    public DocumentFileInfo[] AnalyzerConfigDocuments { get; init; } = [];

    /// <summary>
    /// References to other projects.
    /// </summary>
    [DataMember]
    public ProjectFileReference[] ProjectReferences { get; init; } = [];

    /// <summary>
    /// The msbuild project capabilities.
    /// </summary>
    [DataMember]
    public string[] ProjectCapabilities { get; init; } = [];

    /// <summary>
    /// The paths to content files included in the project.
    /// </summary>
    [DataMember]
    public string[] ContentFilePaths { get; init; } = [];

    /// <summary>
    /// The path to the project.assets.json path in obj/.
    /// </summary>
    [DataMember]
    public string? ProjectAssetsFilePath { get; init; }

    /// <summary>
    /// Any package references defined on the project.
    /// </summary>
    [DataMember]
    public PackageReference[] PackageReferences { get; init; } = [];

    /// <summary>
    /// Target framework version (for .net framework projects)
    /// </summary>
    [DataMember]
    public string? TargetFrameworkVersion { get; init; }

    [DataMember]
    public FileGlobs[] FileGlobs { get; init; } = [];

    public override string ToString()
        => string.IsNullOrWhiteSpace(TargetFramework)
            ? FilePath ?? string.Empty
            : $"{FilePath} ({TargetFramework})";

    public static ProjectFileInfo CreateEmpty(string language, string? filePath)
        => new()
        {
            IsEmpty = true,
            Language = language,
            FilePath = filePath,
            CommandLineArgs = [],
            Documents = [],
            AdditionalDocuments = [],
            AnalyzerConfigDocuments = [],
            ProjectReferences = [],
            PackageReferences = [],
            ProjectCapabilities = [],
            ContentFilePaths = [],
            FileGlobs = []
        };
}
