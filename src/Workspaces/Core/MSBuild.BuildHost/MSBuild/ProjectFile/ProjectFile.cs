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

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal abstract class ProjectFile : IProjectFile
    {
        private readonly ProjectFileLoader _loader;
        private readonly MSB.Evaluation.Project? _loadedProject;
        private readonly ProjectBuildManager _buildManager;
        private readonly string _projectDirectory;

        public DiagnosticLog Log { get; }
        public virtual string FilePath => _loadedProject?.FullPath ?? string.Empty;
        public string Language => _loader.Language;

        protected ProjectFile(ProjectFileLoader loader, MSB.Evaluation.Project? loadedProject, ProjectBuildManager buildManager, DiagnosticLog log)
        {
            _loader = loader;
            _loadedProject = loadedProject;
            _buildManager = buildManager;
            var directory = loadedProject?.DirectoryPath ?? string.Empty;
            _projectDirectory = PathUtilities.EnsureTrailingSeparator(directory);
            Log = log;
        }

        public ImmutableArray<DiagnosticLogItem> GetDiagnosticLogItems() => [.. Log];

        protected abstract IEnumerable<MSB.Framework.ITaskItem> GetCompilerCommandLineArgs(MSB.Execution.ProjectInstance executedProject);
        protected abstract ImmutableArray<string> ReadCommandLineArgs(MSB.Execution.ProjectInstance project);

        /// <summary>
        /// Gets project file information asynchronously. Note that this can produce multiple
        /// instances of <see cref="ProjectFileInfo"/> if the project is multi-targeted: one for
        /// each target framework.
        /// </summary>
        public async Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
        {
            if (_loadedProject is null)
            {
                return [ProjectFileInfo.CreateEmpty(Language, _loadedProject?.FullPath)];
            }

            var targetFrameworkValue = _loadedProject.GetPropertyValue(PropertyNames.TargetFramework);
            var targetFrameworksValue = _loadedProject.GetPropertyValue(PropertyNames.TargetFrameworks);

            if (RoslynString.IsNullOrEmpty(targetFrameworkValue) && !RoslynString.IsNullOrEmpty(targetFrameworksValue))
            {
                // This project has a <TargetFrameworks> property, but does not specify a <TargetFramework>.
                // In this case, we need to iterate through the <TargetFrameworks>, set <TargetFramework> with
                // each value, and build the project.

                var targetFrameworks = targetFrameworksValue.Split(';');

                if (!_loadedProject.GlobalProperties.TryGetValue(PropertyNames.TargetFramework, out var initialGlobalTargetFrameworkValue))
                    initialGlobalTargetFrameworkValue = null;

                var results = new FixedSizeArrayBuilder<ProjectFileInfo>(targetFrameworks.Length);
                foreach (var targetFramework in targetFrameworks)
                {
                    _loadedProject.SetGlobalProperty(PropertyNames.TargetFramework, targetFramework);
                    _loadedProject.ReevaluateIfNecessary();

                    var projectFileInfo = await BuildProjectFileInfoAsync(cancellationToken).ConfigureAwait(false);

                    results.Add(projectFileInfo);
                }

                if (initialGlobalTargetFrameworkValue is null)
                {
                    _loadedProject.RemoveGlobalProperty(PropertyNames.TargetFramework);
                }
                else
                {
                    _loadedProject.SetGlobalProperty(PropertyNames.TargetFramework, initialGlobalTargetFrameworkValue);
                }

                _loadedProject.ReevaluateIfNecessary();

                return results.MoveToImmutable();
            }
            else
            {
                var projectFileInfo = await BuildProjectFileInfoAsync(cancellationToken).ConfigureAwait(false);
                projectFileInfo ??= ProjectFileInfo.CreateEmpty(Language, _loadedProject?.FullPath);
                return [projectFileInfo];
            }
        }

        private async Task<ProjectFileInfo> BuildProjectFileInfoAsync(CancellationToken cancellationToken)
        {
            if (_loadedProject is null)
            {
                return ProjectFileInfo.CreateEmpty(Language, _loadedProject?.FullPath);
            }

            var project = await _buildManager.BuildProjectAsync(_loadedProject, Log, cancellationToken).ConfigureAwait(false);

            return project != null
                ? CreateProjectFileInfo(project)
                : ProjectFileInfo.CreateEmpty(Language, _loadedProject.FullPath);
        }

        private ProjectFileInfo CreateProjectFileInfo(MSB.Execution.ProjectInstance project)
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

            var docs = project.GetDocuments()
                .Where(IsNotTemporaryGeneratedFile)
                .Select(MakeDocumentFileInfo)
                .ToImmutableArray();

            var additionalDocs = project.GetAdditionalFiles()
                .Select(MakeNonSourceFileDocumentFileInfo)
                .ToImmutableArray();

            var analyzerConfigDocs = project.GetEditorConfigFiles()
                .Select(MakeNonSourceFileDocumentFileInfo)
                .ToImmutableArray();

            var packageReferences = project.GetPackageReferences();

            var projectCapabilities = project.GetItems(ItemNames.ProjectCapability).SelectAsArray(item => item.ToString());
            var contentFileInfo = GetContentFiles(project);

            return ProjectFileInfo.Create(
                Language,
                project.FullPath,
                outputFilePath,
                outputRefFilePath,
                intermediateOutputFilePath,
                defaultNamespace,
                targetFramework,
                targetFrameworkIdentifier,
                targetFrameworkVersion,
                projectAssetsFilePath,
                commandLineArgs,
                docs,
                additionalDocs,
                analyzerConfigDocs,
                project.GetProjectReferences().ToImmutableArray(),
                packageReferences,
                projectCapabilities,
                contentFileInfo);
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
            var commandLineArgs = GetCompilerCommandLineArgs(project)
                .Select(item => item.ItemSpec)
                .ToImmutableArray();

            if (commandLineArgs.Length == 0)
            {
                // We didn't get any command-line args, which likely means that the build
                // was not successful. In that case, try to read the command-line args from
                // the ProjectInstance that we have. This is a best effort to provide something
                // meaningful for the user, though it will likely be incomplete.
                commandLineArgs = ReadCommandLineArgs(project);
            }

            return commandLineArgs;
        }

        protected static bool IsNotTemporaryGeneratedFile(MSB.Framework.ITaskItem item)
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
                var folders = relativePath == null ? [] : relativePath.Split(PathUtilities.DirectorySeparatorChar, PathUtilities.AltDirectorySeparatorChar).ToImmutableArray();
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

        protected bool IsDocumentGenerated(MSB.Framework.ITaskItem documentItem)
        {
            if (_documents == null)
            {
                _documents = new Dictionary<string, MSB.Evaluation.ProjectItem>();
                if (_loadedProject is null)
                {
                    return false;
                }

                foreach (var item in _loadedProject.GetItems(ItemNames.Compile))
                {
                    _documents[GetAbsolutePathRelativeToProject(item.EvaluatedInclude)] = item;
                }
            }

            return !_documents.ContainsKey(GetAbsolutePathRelativeToProject(documentItem.ItemSpec));
        }

        protected static string GetDocumentLogicalPath(MSB.Framework.ITaskItem documentItem, string projectDirectory)
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

        public void AddDocument(string filePath, string? logicalPath = null)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

            Dictionary<string, string>? metadata = null;
            if (logicalPath != null && relativePath != logicalPath)
            {
                metadata = new Dictionary<string, string>
                {
                    { MetadataNames.Link, logicalPath }
                };

                relativePath = filePath; // link to full path
            }

            _loadedProject.AddItem(ItemNames.Compile, relativePath, metadata);
        }

        public void RemoveDocument(string filePath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

            var items = _loadedProject.GetItems(ItemNames.Compile);
            var item = items.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                               || PathUtilities.PathsEqual(it.EvaluatedInclude, filePath));
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        public void AddMetadataReference(string metadataReferenceIdentity, ImmutableArray<string> aliases, string? hintPath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var metadata = new Dictionary<string, string>();
            if (!aliases.IsEmpty)
                metadata.Add(MetadataNames.Aliases, string.Join(",", aliases));

            if (hintPath is not null)
                metadata.Add(MetadataNames.HintPath, hintPath);

            _loadedProject.AddItem(ItemNames.Reference, metadataReferenceIdentity, metadata);
        }

        public void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var item = FindReferenceItem(shortAssemblyName, fullAssemblyName, filePath);
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        private MSB.Evaluation.ProjectItem FindReferenceItem(string shortAssemblyName, string fullAssemblyName, string filePath)
        {
            Contract.ThrowIfNull(_loadedProject, "The project was not loaded.");

            var references = _loadedProject.GetItems(ItemNames.Reference);
            MSB.Evaluation.ProjectItem? item = null;

            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // check for short name match
            item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, shortAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);
            if (item is not null)
                return item;

            // check for full name match
            item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, fullAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);
            if (item is not null)
                return item;

            // check for file path match
            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, filePath);

            item = references.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, filePath)
                                                    || PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                    || PathUtilities.PathsEqual(GetHintPath(it), filePath)
                                                    || PathUtilities.PathsEqual(GetHintPath(it), relativePath));

            if (item is not null)
                return item;

            var partialName = shortAssemblyName + ",";
            var items = references.Where(it => it.EvaluatedInclude.StartsWith(partialName, StringComparison.OrdinalIgnoreCase)).ToList();
            if (items.Count == 1)
            {
                return items[0];
            }

            throw new InvalidOperationException($"Unable to find reference item '{shortAssemblyName}'");
        }

        private static string GetHintPath(MSB.Evaluation.ProjectItem item)
            => item.Metadata.FirstOrDefault(m => string.Equals(m.Name, MetadataNames.HintPath, StringComparison.OrdinalIgnoreCase))?.EvaluatedValue ?? string.Empty;

        public void AddProjectReference(string projectName, ProjectFileReference reference)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var metadata = new Dictionary<string, string>
            {
                { MetadataNames.Name, projectName }
            };

            if (!reference.Aliases.IsEmpty)
            {
                metadata.Add(MetadataNames.Aliases, string.Join(",", reference.Aliases));
            }

            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, reference.Path);
            _loadedProject.AddItem(ItemNames.ProjectReference, relativePath, metadata);
        }

        public void RemoveProjectReference(string projectName, string projectFilePath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var item = FindProjectReferenceItem(projectName, projectFilePath);
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        private MSB.Evaluation.ProjectItem? FindProjectReferenceItem(string projectName, string projectFilePath)
        {
            if (_loadedProject is null)
            {
                return null;
            }

            var references = _loadedProject.GetItems(ItemNames.ProjectReference);
            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, projectFilePath);

            MSB.Evaluation.ProjectItem? item = null;

            // find by project file path
            item = references.First(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                       || PathUtilities.PathsEqual(it.EvaluatedInclude, projectFilePath));

            // try to find by project name
            item ??= references.First(it => string.Compare(projectName, it.GetMetadataValue(MetadataNames.Name), StringComparison.OrdinalIgnoreCase) == 0);

            return item;
        }

        public void AddAnalyzerReference(string fullPath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, fullPath);
            _loadedProject.AddItem(ItemNames.Analyzer, relativePath);
        }

        public void RemoveAnalyzerReference(string fullPath)
        {
            if (_loadedProject is null)
            {
                return;
            }

            var relativePath = PathUtilities.GetRelativePath(_loadedProject.DirectoryPath, fullPath);

            var analyzers = _loadedProject.GetItems(ItemNames.Analyzer);
            var item = analyzers.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                    || PathUtilities.PathsEqual(it.EvaluatedInclude, fullPath));
            if (item != null)
            {
                _loadedProject.RemoveItem(item);
            }
        }

        public void Save()
        {
            if (_loadedProject is null)
            {
                return;
            }

            _loadedProject.Save();
        }
    }
}
