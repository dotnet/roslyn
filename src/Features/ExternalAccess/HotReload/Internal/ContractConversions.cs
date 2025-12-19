// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias BuildHost;
using Microsoft.CodeAnalysis.MSBuild;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Internal;

internal static class ContractConversions
{
    public static ProjectFileInfo Convert(this BuildHost::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo info)
        => new()
        {
            IsEmpty = info.IsEmpty,
            Language = info.Language,
            FilePath = info.FilePath,
            OutputFilePath = info.OutputFilePath,
            OutputRefFilePath = info.OutputRefFilePath,
            IntermediateOutputFilePath = info.IntermediateOutputFilePath,
            GeneratedFilesOutputDirectory = info.GeneratedFilesOutputDirectory,
            DefaultNamespace = info.DefaultNamespace,
            TargetFramework = info.TargetFramework,
            TargetFrameworkIdentifier = info.TargetFrameworkIdentifier,
            CommandLineArgs = info.CommandLineArgs,
            Documents = info.Documents.SelectAsArray(Convert),
            AdditionalDocuments = info.AdditionalDocuments.SelectAsArray(Convert),
            AnalyzerConfigDocuments = info.AnalyzerConfigDocuments.SelectAsArray(Convert),
            ProjectReferences = info.ProjectReferences.SelectAsArray(Convert),
            ProjectCapabilities = info.ProjectCapabilities,
            ContentFilePaths = info.ContentFilePaths,
            ProjectAssetsFilePath = info.ProjectAssetsFilePath,
            PackageReferences = info.PackageReferences.SelectAsArray(Convert),
            TargetFrameworkVersion = info.TargetFrameworkVersion,
            FileGlobs = info.FileGlobs.SelectAsArray(Convert),
        };

    public static DocumentFileInfo Convert(this BuildHost::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo info)
        => new(info.FilePath, info.LogicalPath, info.IsLinked, info.IsGenerated, info.Folders);

    public static ProjectFileReference Convert(this BuildHost::Microsoft.CodeAnalysis.MSBuild.ProjectFileReference reference)
        => new(reference.Path, reference.Aliases, reference.ReferenceOutputAssembly);

    public static PackageReference Convert(this BuildHost::Microsoft.CodeAnalysis.MSBuild.PackageReference reference)
        => new(reference.Name, reference.VersionRange);

    public static FileGlobs Convert(this BuildHost::Microsoft.CodeAnalysis.MSBuild.FileGlobs globs)
        => new(globs.Includes, globs.Excludes, globs.Removes);
}
