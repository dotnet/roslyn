// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild
{
    public partial class MSBuildProjectLoader
    {
        private partial class Worker
        {
            private readonly struct ResolvedReferences
            {
                public ImmutableHashSet<ProjectReference> ProjectReferences { get; }
                public ImmutableArray<MetadataReference> MetadataReferences { get; }

                public ResolvedReferences(ImmutableHashSet<ProjectReference> projectReferences, ImmutableArray<MetadataReference> metadataReferences)
                {
                    ProjectReferences = projectReferences;
                    MetadataReferences = metadataReferences;
                }
            }

            /// <summary>
            /// This type helps produces lists of metadata and project references. Initially, it contains a list of metadata references.
            /// As project references are added, the metadata references that match those project references are removed.
            /// </summary>
            private class ResolvedReferencesBuilder
            {
                /// <summary>
                /// The full list of <see cref="MetadataReference"/>s.
                /// </summary>
                private readonly ImmutableArray<MetadataReference> _metadataReferences;

                /// <summary>
                /// A map of every metadata reference file paths to a set of indices whether than file path
                /// exists in the list. It is expected that there may be multiple metadata references for the
                /// same file path in the case where multiple extern aliases are provided.
                /// </summary>
                private readonly ImmutableDictionary<string, HashSet<int>> _pathToIndicesMap;

                /// <summary>
                /// A set of indices into <see cref="_metadataReferences"/> that are to be removed.
                /// </summary>
                private readonly HashSet<int> _indicesToRemove;

                private readonly ImmutableHashSet<ProjectReference>.Builder _projectReferences;

                public ResolvedReferencesBuilder(IEnumerable<MetadataReference> metadataReferences)
                {
                    _metadataReferences = metadataReferences.ToImmutableArray();
                    _pathToIndicesMap = CreatePathToIndexMap(_metadataReferences);
                    _indicesToRemove = new HashSet<int>();
                    _projectReferences = ImmutableHashSet.CreateBuilder<ProjectReference>();
                }

                private static ImmutableDictionary<string, HashSet<int>> CreatePathToIndexMap(ImmutableArray<MetadataReference> metadataReferences)
                {
                    var builder = ImmutableDictionary.CreateBuilder<string, HashSet<int>>(PathUtilities.Comparer);

                    for (var index = 0; index < metadataReferences.Length; index++)
                    {
                        var filePath = GetFilePath(metadataReferences[index]);
                        if (filePath != null)
                        {
                            builder.MultiAdd(filePath, index);
                        }
                    }

                    return builder.ToImmutable();
                }

                private static string? GetFilePath(MetadataReference metadataReference)
                {
                    return metadataReference switch
                    {
                        PortableExecutableReference portableExecutableReference => portableExecutableReference.FilePath,
                        UnresolvedMetadataReference unresolvedMetadataReference => unresolvedMetadataReference.Reference,
                        _ => null,
                    };
                }

                public void AddProjectReference(ProjectReference projectReference)
                {
                    _projectReferences.Add(projectReference);
                }

                public void SwapMetadataReferenceForProjectReference(ProjectReference projectReference, params string?[] possibleMetadataReferencePaths)
                {
                    foreach (var path in possibleMetadataReferencePaths)
                    {
                        if (path != null)
                        {
                            Remove(path);
                        }
                    }

                    AddProjectReference(projectReference);
                }

                /// <summary>
                /// Returns true if a metadata reference with the given file path is contained within this list.
                /// </summary>
                public bool Contains(string? filePath)
                    => filePath != null
                    && _pathToIndicesMap.ContainsKey(filePath);

                /// <summary>
                /// Removes the metadata reference with the given file path from this list.
                /// </summary>
                public void Remove(string filePath)
                {
                    if (filePath != null && _pathToIndicesMap.TryGetValue(filePath, out var indices))
                    {
                        _indicesToRemove.AddRange(indices);
                    }
                }

                public ProjectInfo? SelectProjectInfoByOutput(IEnumerable<ProjectInfo> projectInfos)
                {
                    foreach (var projectInfo in projectInfos)
                    {
                        var outputFilePath = projectInfo.OutputFilePath;
                        var outputRefFilePath = projectInfo.OutputRefFilePath;
                        if (outputFilePath != null &&
                            outputRefFilePath != null &&
                            (Contains(outputFilePath) || Contains(outputRefFilePath)))
                        {
                            return projectInfo;
                        }
                    }

                    return null;
                }

                public ImmutableArray<UnresolvedMetadataReference> GetUnresolvedMetadataReferences()
                {
                    var builder = ImmutableArray.CreateBuilder<UnresolvedMetadataReference>();

                    foreach (var metadataReference in GetMetadataReferences())
                    {
                        if (metadataReference is UnresolvedMetadataReference unresolvedMetadataReference)
                        {
                            builder.Add(unresolvedMetadataReference);
                        }
                    }

                    return builder.ToImmutable();
                }

                private ImmutableArray<MetadataReference> GetMetadataReferences()
                {
                    var builder = ImmutableArray.CreateBuilder<MetadataReference>();

                    // used to eliminate duplicates
                    var _ = PooledHashSet<MetadataReference>.GetInstance(out var set);

                    for (var index = 0; index < _metadataReferences.Length; index++)
                    {
                        var reference = _metadataReferences[index];
                        if (!_indicesToRemove.Contains(index) && set.Add(reference))
                        {
                            builder.Add(reference);
                        }
                    }

                    return builder.ToImmutable();
                }

                private ImmutableHashSet<ProjectReference> GetProjectReferences()
                    => _projectReferences.ToImmutable();

                public ResolvedReferences ToResolvedReferences()
                    => new(GetProjectReferences(), GetMetadataReferences());
            }

            private async Task<ResolvedReferences> ResolveReferencesAsync(ProjectId id, ProjectFileInfo projectFileInfo, CommandLineArguments commandLineArgs, CancellationToken cancellationToken)
            {
                // First, gather all of the metadata references from the command-line arguments.
                var resolvedMetadataReferences = commandLineArgs.ResolveMetadataReferences(
                    new WorkspaceMetadataFileReferenceResolver(
                        metadataService: _solutionServices.GetRequiredService<IMetadataService>(),
                        pathResolver: new RelativePathResolver(commandLineArgs.ReferencePaths, commandLineArgs.BaseDirectory)));

                var builder = new ResolvedReferencesBuilder(resolvedMetadataReferences);

                var projectDirectory = Path.GetDirectoryName(projectFileInfo.FilePath);
                RoslynDebug.AssertNotNull(projectDirectory);

                // Next, iterate through all project references in the file and create project references.
                foreach (var projectFileReference in projectFileInfo.ProjectReferences)
                {
                    var aliases = projectFileReference.Aliases;

                    if (_pathResolver.TryGetAbsoluteProjectPath(projectFileReference.Path, baseDirectory: projectDirectory, _discoveredProjectOptions.OnPathFailure, out var projectReferencePath))
                    {
                        // The easiest case is to add a reference to a project we already know about.
                        if (TryAddReferenceToKnownProject(id, projectReferencePath, aliases, builder))
                        {
                            continue;
                        }

                        // If we don't know how to load a project (that is, it's not a language we support), we can still
                        // attempt to verify that its output exists on disk and is included in our set of metadata references.
                        // If it is, we'll just leave it in place.
                        if (!IsProjectLoadable(projectReferencePath) &&
                            await VerifyUnloadableProjectOutputExistsAsync(projectReferencePath, builder, cancellationToken).ConfigureAwait(false))
                        {
                            continue;
                        }

                        // If metadata is preferred, see if the project reference's output exists on disk and is included
                        // in our metadata references. If it is, don't create a project reference; we'll just use the metadata.
                        if (_preferMetadataForReferencesOfDiscoveredProjects &&
                            await VerifyProjectOutputExistsAsync(projectReferencePath, builder, cancellationToken).ConfigureAwait(false))
                        {
                            continue;
                        }

                        // Finally, we'll try to load and reference the project.
                        if (await TryLoadAndAddReferenceAsync(id, projectReferencePath, aliases, builder, cancellationToken).ConfigureAwait(false))
                        {
                            continue;
                        }
                    }

                    // We weren't able to handle this project reference, so add it without further processing.
                    var unknownProjectId = _projectMap.GetOrCreateProjectId(projectFileReference.Path);
                    var newProjectReference = CreateProjectReference(from: id, to: unknownProjectId, aliases);
                    builder.AddProjectReference(newProjectReference);
                }

                // Are there still any unresolved metadata references? If so, remove them and report diagnostics.
                foreach (var unresolvedMetadataReference in builder.GetUnresolvedMetadataReferences())
                {
                    var filePath = unresolvedMetadataReference.Reference;

                    builder.Remove(filePath);

                    _diagnosticReporter.Report(new ProjectDiagnostic(
                        WorkspaceDiagnosticKind.Warning,
                        string.Format(WorkspaceMSBuildResources.Unresolved_metadata_reference_removed_from_project_0, filePath),
                        id));
                }

                return builder.ToResolvedReferences();
            }

            private async Task<bool> TryLoadAndAddReferenceAsync(ProjectId id, string projectReferencePath, ImmutableArray<string> aliases, ResolvedReferencesBuilder builder, CancellationToken cancellationToken)
            {
                var projectReferenceInfos = await LoadProjectInfosFromPathAsync(projectReferencePath, _discoveredProjectOptions, cancellationToken).ConfigureAwait(false);

                if (projectReferenceInfos.IsEmpty)
                {
                    return false;
                }

                // Find the project reference info whose output we have a metadata reference for.
                ProjectInfo? projectReferenceInfo = null;
                foreach (var info in projectReferenceInfos)
                {
                    var outputFilePath = info.OutputFilePath;
                    var outputRefFilePath = info.OutputRefFilePath;
                    if (outputFilePath != null &&
                        outputRefFilePath != null &&
                        (builder.Contains(outputFilePath) || builder.Contains(outputRefFilePath)))
                    {
                        projectReferenceInfo = info;
                        break;
                    }
                }

                if (projectReferenceInfo is null)
                {
                    // We didn't find the project reference info that matches any of our metadata references.
                    // In this case, we'll go ahead and use the first project reference info that was found,
                    // but report a warning because this likely means that either a metadata reference path
                    // or a project output path is incorrect.

                    projectReferenceInfo = projectReferenceInfos[0];

                    _diagnosticReporter.Report(new ProjectDiagnostic(
                        WorkspaceDiagnosticKind.Warning,
                        string.Format(WorkspaceMSBuildResources.Found_project_reference_without_a_matching_metadata_reference_0, projectReferencePath),
                        id));
                }

                if (!ProjectReferenceExists(to: id, from: projectReferenceInfo))
                {
                    var newProjectReference = CreateProjectReference(from: id, to: projectReferenceInfo.Id, aliases);
                    builder.SwapMetadataReferenceForProjectReference(newProjectReference, projectReferenceInfo.OutputRefFilePath, projectReferenceInfo.OutputFilePath);
                }
                else
                {
                    // This project already has a reference on us. Don't introduce a circularity by referencing it.
                    // However, if the project's output doesn't exist on disk, we need to remove from our list of
                    // metadata references to avoid failures later. Essentially, the concern here is that the metadata
                    // reference is an UnresolvedMetadataReference, which will throw when we try to create a
                    // Compilation with it.

                    var outputRefFilePath = projectReferenceInfo.OutputRefFilePath;
                    if (outputRefFilePath != null && !File.Exists(outputRefFilePath))
                    {
                        builder.Remove(outputRefFilePath);
                    }

                    var outputFilePath = projectReferenceInfo.OutputFilePath;
                    if (outputFilePath != null && !File.Exists(outputFilePath))
                    {
                        builder.Remove(outputFilePath);
                    }
                }

                // Note that we return true even if we don't actually add a reference due to a circularity because,
                // in that case, we've still handled everything.
                return true;
            }

            private bool IsProjectLoadable(string projectPath)
                => _projectFileLoaderRegistry.TryGetLoaderFromProjectPath(projectPath, DiagnosticReportingMode.Ignore, out _);

            private async Task<bool> VerifyUnloadableProjectOutputExistsAsync(string projectPath, ResolvedReferencesBuilder builder, CancellationToken cancellationToken)
            {
                var outputFilePath = await _buildManager.TryGetOutputFilePathAsync(projectPath, cancellationToken).ConfigureAwait(false);
                return outputFilePath != null
                    && builder.Contains(outputFilePath)
                    && File.Exists(outputFilePath);
            }

            private async Task<bool> VerifyProjectOutputExistsAsync(string projectPath, ResolvedReferencesBuilder builder, CancellationToken cancellationToken)
            {
                // Note: Load the project, but don't report failures.
                var projectFileInfos = await LoadProjectFileInfosAsync(projectPath, DiagnosticReportingOptions.IgnoreAll, cancellationToken).ConfigureAwait(false);

                foreach (var projectFileInfo in projectFileInfos)
                {
                    var outputFilePath = projectFileInfo.OutputFilePath;
                    var outputRefFilePath = projectFileInfo.OutputRefFilePath;

                    if ((builder.Contains(outputFilePath) && File.Exists(outputFilePath)) ||
                        (builder.Contains(outputRefFilePath) && File.Exists(outputRefFilePath)))
                    {
                        return true;
                    }
                }

                return false;
            }

            private ProjectReference CreateProjectReference(ProjectId from, ProjectId to, ImmutableArray<string> aliases)
            {
                var newReference = new ProjectReference(to, aliases);
                _projectIdToProjectReferencesMap.MultiAdd(from, newReference);
                return newReference;
            }

            private bool ProjectReferenceExists(ProjectId to, ProjectId from)
                => _projectIdToProjectReferencesMap.TryGetValue(from, out var references)
                && references.Contains(pr => pr.ProjectId == to);

            private static bool ProjectReferenceExists(ProjectId to, ProjectInfo from)
                => from.ProjectReferences.Any(pr => pr.ProjectId == to);

            private bool TryAddReferenceToKnownProject(
                ProjectId id,
                string projectReferencePath,
                ImmutableArray<string> aliases,
                ResolvedReferencesBuilder builder)
            {
                if (_projectMap.TryGetIdsByProjectPath(projectReferencePath, out var projectReferenceIds))
                {
                    foreach (var projectReferenceId in projectReferenceIds)
                    {
                        // Don't add a reference if the project already has a reference on us. Otherwise, it will cause a circularity.
                        if (ProjectReferenceExists(to: id, from: projectReferenceId))
                        {
                            return false;
                        }

                        var outputRefFilePath = _projectMap.GetOutputRefFilePathById(projectReferenceId);
                        var outputFilePath = _projectMap.GetOutputFilePathById(projectReferenceId);

                        if (builder.Contains(outputRefFilePath) ||
                            builder.Contains(outputFilePath))
                        {
                            var newProjectReference = CreateProjectReference(from: id, to: projectReferenceId, aliases);
                            builder.SwapMetadataReferenceForProjectReference(newProjectReference, outputRefFilePath, outputFilePath);
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}
