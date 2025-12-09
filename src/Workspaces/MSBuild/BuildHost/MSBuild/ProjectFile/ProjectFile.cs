// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectFile
{
    private readonly ProjectCommandLineProvider _commandLineProvider;
    public readonly MSB.Evaluation.Project? Project;

    private readonly string _projectDirectory;

    public string FilePath => Project?.FullPath ?? string.Empty;

    public ProjectFile(ProjectCommandLineProvider commandLineReader, MSB.Evaluation.Project? project)
    {
        _commandLineProvider = commandLineReader;
        Project = project;
        var directory = project?.DirectoryPath ?? string.Empty;
        _projectDirectory = PathUtilities.EnsureTrailingSeparator(directory);
    }

    public string Language
        => _commandLineProvider.Language;

    public ProjectFileInfo CreateProjectFileInfo(MSB.Execution.ProjectInstance project)
    {
        var commandLineArgs = GetCommandLineArgs(project);

        var outputFilePath = project.ReadPropertyString(PropertyNames.TargetPath);
        if (!RoslynString.IsNullOrWhiteSpace(outputFilePath))
        {
            outputFilePath = GetAbsolutePathRelativeToProject(outputFilePath);
        }

        var outputRefFilePath = project.ReadPropertyString(PropertyNames.TargetRefPath);
        if (!RoslynString.IsNullOrWhiteSpace(outputRefFilePath))
        {
            outputRefFilePath = GetAbsolutePathRelativeToProject(outputRefFilePath);
        }

        var generatedFilesOutputDirectory = project.ReadPropertyString(PropertyNames.CompilerGeneratedFilesOutputPath);
        generatedFilesOutputDirectory = RoslynString.IsNullOrWhiteSpace(generatedFilesOutputDirectory)
            ? null
            : GetAbsolutePathRelativeToProject(generatedFilesOutputDirectory);

        var intermediateOutputFilePath = project.GetItems(ItemNames.IntermediateAssembly).FirstOrDefault()?.EvaluatedInclude;
        if (!RoslynString.IsNullOrWhiteSpace(intermediateOutputFilePath))
        {
            intermediateOutputFilePath = GetAbsolutePathRelativeToProject(intermediateOutputFilePath);
        }

        var projectAssetsFilePath = project.ReadPropertyString(PropertyNames.ProjectAssetsFile);

        // Right now VB doesn't have the concept of "default namespace". But we conjure one in workspace 
        // by assigning the value of the project's root namespace to it. So various feature can choose to 
        // use it for their own purpose.
        // In the future, we might consider officially exposing "default namespace" for VB project 
        // (e.g. through a <defaultnamespace> msbuild property)
        var defaultNamespace = project.ReadPropertyString(PropertyNames.RootNamespace) ?? string.Empty;

        var targetFramework = project.ReadPropertyString(PropertyNames.TargetFramework);
        if (RoslynString.IsNullOrWhiteSpace(targetFramework))
        {
            targetFramework = null;
        }

        var targetFrameworkIdentifier = project.ReadPropertyString(PropertyNames.TargetFrameworkIdentifier);

        var targetFrameworkVersion = project.ReadPropertyString(PropertyNames.TargetFrameworkVersion);

        var docs = project.GetDocuments().SelectAsArray(
            predicate: IsNotTemporaryGeneratedFile,
            selector: MakeDocumentFileInfo);

        var additionalDocs = project.GetAdditionalFiles()
            .SelectAsArray(MakeNonSourceFileDocumentFileInfo);

        var analyzerConfigDocs = project.GetEditorConfigFiles()
            .SelectAsArray(MakeNonSourceFileDocumentFileInfo);

        var packageReferences = project.GetPackageReferences();

        var projectCapabilities = project.GetItems(ItemNames.ProjectCapability).SelectAsArray(item => item.ToString());
        var contentFileInfo = GetContentFiles(project);

        var fileGlobs = Project?.GetAllGlobs().SelectAsArray(GetFileGlobs) ?? [];

        return new ProjectFileInfo()
        {
            Language = Language,
            FilePath = project.FullPath,
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
            Documents = docs,
            AdditionalDocuments = additionalDocs,
            AnalyzerConfigDocuments = analyzerConfigDocs,
            ProjectReferences = [.. project.GetProjectReferences()],
            PackageReferences = packageReferences,
            ProjectCapabilities = projectCapabilities,
            ContentFilePaths = contentFileInfo,
            FileGlobs = fileGlobs
        };

        static FileGlobs GetFileGlobs(MSB.Evaluation.GlobResult g)
        {
            return new FileGlobs(
                Includes: [.. g.IncludeGlobs.Select(PathUtilities.ExpandAbsolutePathWithRelativeParts)],
                Excludes: [.. g.Excludes.Select(PathUtilities.ExpandAbsolutePathWithRelativeParts)],
                Removes: [.. g.Removes.Select(PathUtilities.ExpandAbsolutePathWithRelativeParts)]);
        }
    }

    private static ImmutableArray<string> GetContentFiles(MSB.Execution.ProjectInstance project)
    {
        var contentFiles = project
            .GetItems(ItemNames.Content)
            .SelectAsArray(item => item.GetMetadataValue(MetadataNames.FullPath));
        return contentFiles;
    }

    private ImmutableArray<string> GetCommandLineArgs(MSB.Execution.ProjectInstance project)
    {
        var commandLineArgs = _commandLineProvider.GetCompilerCommandLineArgs(project)
            .SelectAsArray(item => item.ItemSpec);

        if (commandLineArgs.Length == 0)
        {
            // We didn't get any command-line args, which likely means that the build
            // was not successful. In that case, try to read the command-line args from
            // the ProjectInstance that we have. This is a best effort to provide something
            // meaningful for the user, though it will likely be incomplete.
            commandLineArgs = _commandLineProvider.ReadCommandLineArgs(project);
        }

        return commandLineArgs;
    }

    private static bool IsNotTemporaryGeneratedFile(MSB.Framework.ITaskItem item)
        => !Path.GetFileName(item.ItemSpec).StartsWith("TemporaryGeneratedFile_", StringComparison.Ordinal);

    private DocumentFileInfo MakeDocumentFileInfo(MSB.Framework.ITaskItem documentItem)
    {
        var filePath = GetDocumentFilePath(documentItem);
        var logicalPath = GetDocumentLogicalPath(documentItem, _projectDirectory);
        var isLinked = IsDocumentLinked(documentItem);
        var isGenerated = IsDocumentGenerated(documentItem);

        var folders = GetRelativeFolders(documentItem);
        return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated, folders);
    }

    private DocumentFileInfo MakeNonSourceFileDocumentFileInfo(MSB.Framework.ITaskItem documentItem)
    {
        var filePath = GetDocumentFilePath(documentItem);
        var logicalPath = GetDocumentLogicalPath(documentItem, _projectDirectory);
        var isLinked = IsDocumentLinked(documentItem);
        var isGenerated = IsDocumentGenerated(documentItem);

        var folders = GetRelativeFolders(documentItem);
        return new DocumentFileInfo(filePath, logicalPath, isLinked, isGenerated, folders);
    }

    private ImmutableArray<string> GetRelativeFolders(MSB.Framework.ITaskItem documentItem)
    {
        var linkPath = documentItem.GetMetadata(MetadataNames.Link);
        if (!RoslynString.IsNullOrEmpty(linkPath))
        {
            return [.. PathUtilities.GetDirectoryName(linkPath).Split(PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar)];
        }
        else
        {
            var filePath = documentItem.ItemSpec;
            var relativePath = PathUtilities.GetDirectoryName(PathUtilities.GetRelativePath(_projectDirectory, filePath));
            var folders = relativePath == null ? [] : relativePath.Split([PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
            return folders;
        }
    }

    /// <summary>
    /// Resolves the given path that is possibly relative to the project directory.
    /// </summary>
    /// <remarks>
    /// The resulting path is absolute but might not be normalized.
    /// </remarks>
    private string GetAbsolutePathRelativeToProject(string path)
    {
        // TODO (tomat): should we report an error when drive-relative path (e.g. "C:goo.cs") is encountered?
        var absolutePath = FileUtilities.ResolveRelativePath(path, _projectDirectory) ?? path;
        return FileUtilities.TryNormalizeAbsolutePath(absolutePath) ?? absolutePath;
    }

    private string GetDocumentFilePath(MSB.Framework.ITaskItem documentItem)
        => GetAbsolutePathRelativeToProject(documentItem.ItemSpec);

    private static bool IsDocumentLinked(MSB.Framework.ITaskItem documentItem)
        => !RoslynString.IsNullOrEmpty(documentItem.GetMetadata(MetadataNames.Link));

    private IDictionary<string, MSB.Evaluation.ProjectItem>? _documents;

    private bool IsDocumentGenerated(MSB.Framework.ITaskItem documentItem)
    {
        if (_documents == null)
        {
            _documents = new Dictionary<string, MSB.Evaluation.ProjectItem>();
            if (Project is null)
            {
                return false;
            }

            foreach (var item in Project.GetItems(ItemNames.Compile))
            {
                _documents[GetAbsolutePathRelativeToProject(item.EvaluatedInclude)] = item;
            }
        }

        return !_documents.ContainsKey(GetAbsolutePathRelativeToProject(documentItem.ItemSpec));
    }

    private static string GetDocumentLogicalPath(MSB.Framework.ITaskItem documentItem, string projectDirectory)
    {
        var link = documentItem.GetMetadata(MetadataNames.Link);
        if (!RoslynString.IsNullOrEmpty(link))
        {
            // if a specific link is specified in the project file then use it to form the logical path.
            return link;
        }
        else
        {
            var filePath = documentItem.ItemSpec;

            if (!PathUtilities.IsAbsolute(filePath))
            {
                return filePath;
            }

            var normalizedPath = FileUtilities.TryNormalizeAbsolutePath(filePath);
            if (normalizedPath == null)
            {
                return filePath;
            }

            // If the document is within the current project directory (or subdirectory), then the logical path is the relative path 
            // from the project's directory.
            if (normalizedPath.StartsWith(projectDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath[projectDirectory.Length..];
            }
            else
            {
                // if the document lies outside the project's directory (or subdirectory) then place it logically at the root of the project.
                // if more than one document ends up with the same logical name then so be it (the workspace will survive.)
                return PathUtilities.GetFileName(normalizedPath);
            }
        }
    }
}
