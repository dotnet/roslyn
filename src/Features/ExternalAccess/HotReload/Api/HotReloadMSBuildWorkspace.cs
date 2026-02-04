// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias BuildHost;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

internal sealed partial class HotReloadMSBuildWorkspace : Workspace
{
    private readonly ILogger _logger;
    private readonly MSBuildProjectLoader _loader;
    private readonly ProjectFileInfoProvider _projectGraphFileInfoProvider;

    public HotReloadMSBuildWorkspace(ILogger logger, Func<string, (ImmutableArray<MSB.Execution.ProjectInstance> instances, MSB.Evaluation.Project? project)> getBuildProjects)
        : base(MSBuildMefHostServices.DefaultServices, WorkspaceKind.MSBuild)
    {
        RegisterWorkspaceFailedHandler(args =>
        {
            // Report both Warning and Failure as warnings.
            // MSBuildProjectLoader reports Failures for cases where we can safely continue loading projects
            // (e.g. non-C#/VB project is ignored).
            // https://github.com/dotnet/roslyn/issues/75170
            logger.LogWarning($"msbuild: {args.Diagnostic}");
        });

        _logger = logger;
        _loader = new MSBuildProjectLoader(this);
        _projectGraphFileInfoProvider = new ProjectFileInfoProvider(getBuildProjects, _loader.ProjectFileExtensionRegistry);
    }

    // TODO: remove
    public ValueTask<Solution> UpdateProjectConeAsync(string projectPath, CancellationToken cancellationToken)
        => UpdateProjectGraphAsync([projectPath], cancellationToken);

    /// <summary>
    /// Updates all projects in the workspace whose file paths are specified in <paramref name="projectPaths"/> and all their transitive dependencies.
    /// </summary>
    public async ValueTask<Solution> UpdateProjectGraphAsync(ImmutableArray<string> projectPaths, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(projectPaths.All(Path.IsPathFullyQualified));

        var projectMap = ProjectMap.Create();

        var projectInfos = await _loader.LoadInfosAsync(
            projectPaths,
            _projectGraphFileInfoProvider,
            projectMap,
            progress: null,
            cancellationToken).ConfigureAwait(false);

        return UpdateSolution(projectInfos);
    }

    // internal for testing
    internal Solution UpdateSolution(ImmutableArray<ProjectInfo> projectInfos)
    {
        var oldSolution = CurrentSolution;
        var oldProjectIdsByPath = oldSolution.Projects.ToDictionary(keySelector: static p => (p.FilePath!, p.Name), elementSelector: static p => p.Id);

        // Map new project id to the corresponding old one based on file path and project name (includes TFM), if it exists, and null for added projects.
        // Deleted projects won't be included in this map.
        var projectIdMap = projectInfos.ToDictionary(
            keySelector: static info => info.Id,
            elementSelector: info => oldProjectIdsByPath.TryGetValue((info.FilePath!, info.Name), out var oldProjectId) ? oldProjectId : null);

        var newSolution = oldSolution;

        foreach (var newProjectInfo in projectInfos)
        {
            Contract.ThrowIfNull(newProjectInfo.FilePath);

            var oldProjectId = projectIdMap[newProjectInfo.Id];
            if (oldProjectId == null)
            {
                newSolution = newSolution.AddProject(newProjectInfo);
                continue;
            }

            newSolution = newSolution.WithProjectInfo(ProjectInfo.Create(
                oldProjectId,
                newProjectInfo.Version,
                newProjectInfo.Name,
                newProjectInfo.AssemblyName,
                newProjectInfo.Language,
                newProjectInfo.FilePath,
                newProjectInfo.OutputFilePath,
                newProjectInfo.CompilationOptions,
                newProjectInfo.ParseOptions,
                MapDocuments(oldProjectId, newProjectInfo.Documents),
                newProjectInfo.ProjectReferences.Select(MapProjectReference),
                newProjectInfo.MetadataReferences,
                newProjectInfo.AnalyzerReferences,
                MapDocuments(oldProjectId, newProjectInfo.AdditionalDocuments),
                isSubmission: false,
                hostObjectType: null,
                outputRefFilePath: newProjectInfo.OutputRefFilePath)
                .WithChecksumAlgorithm(newProjectInfo.ChecksumAlgorithm)
                .WithAnalyzerConfigDocuments(MapDocuments(oldProjectId, newProjectInfo.AnalyzerConfigDocuments))
                .WithCompilationOutputInfo(newProjectInfo.CompilationOutputInfo));
        }

        var result = SetCurrentSolution(newSolution);
        UpdateReferencesAfterAdd();

        return result;

        ProjectReference MapProjectReference(ProjectReference pr)
        {
            // Only C# and VB projects are loaded by the MSBuildProjectLoader, so some references might be missing.
            // When a new project is added along with a new project reference the old project id is also null.
            return new(
                projectId: projectIdMap.TryGetValue(pr.ProjectId, out var oldProjectId) && oldProjectId != null ? oldProjectId : pr.ProjectId,
                aliases: pr.Aliases,
                embedInteropTypes: pr.EmbedInteropTypes);
        }

        ImmutableArray<DocumentInfo> MapDocuments(ProjectId mappedProjectId, IReadOnlyList<DocumentInfo> documents)
            => documents.Select(docInfo =>
            {
                // TODO: can there be multiple documents of the same path in the project?

                // Map to a document of the same path. If there isn't one (a new document is added to the project),
                // create a new document id with the mapped project id.
                var mappedDocumentId = oldSolution.GetDocumentIdsWithFilePath(docInfo.FilePath).FirstOrDefault(id => id.ProjectId == mappedProjectId)
                    ?? DocumentId.CreateNewId(mappedProjectId);

                return docInfo.WithId(mappedDocumentId);
            }).ToImmutableArray();
    }

    public async ValueTask<Solution> UpdateFileContentAsync(IEnumerable<(string path, HotReloadFileChangeKind change)> changedFiles, CancellationToken cancellationToken)
    {
        var updatedSolution = CurrentSolution;

        var documentsToRemove = new List<DocumentId>();

        foreach (var (path, change) in changedFiles)
        {
            // when a file is added we reevaluate the project:
            Contract.ThrowIfTrue(change == HotReloadFileChangeKind.Add);

            var documentIds = updatedSolution.GetDocumentIdsWithFilePath(path);
            if (change == HotReloadFileChangeKind.Delete)
            {
                documentsToRemove.AddRange(documentIds);
                continue;
            }

            foreach (var documentId in documentIds)
            {
                var textDocument = updatedSolution.GetDocument(documentId)
                    ?? updatedSolution.GetAdditionalDocument(documentId)
                    ?? updatedSolution.GetAnalyzerConfigDocument(documentId);

                if (textDocument == null)
                {
                    _logger.LogDebug("Could not find document with path '{FilePath}' in the workspace.", path);
                    continue;
                }

                var project = updatedSolution.GetProject(documentId.ProjectId);
                Debug.Assert(project?.FilePath != null);

                var oldText = await textDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                Debug.Assert(oldText.Encoding != null);

                var newText = await GetSourceTextAsync(path, oldText.Encoding, oldText.ChecksumAlgorithm, cancellationToken).ConfigureAwait(false);

                _logger.LogDebug("Updating document text of '{FilePath}'.", path);

                updatedSolution = textDocument switch
                {
                    Document document => document.WithText(newText).Project.Solution,
                    AdditionalDocument ad => updatedSolution.WithAdditionalDocumentText(textDocument.Id, newText, PreservationMode.PreserveValue),
                    AnalyzerConfigDocument acd => updatedSolution.WithAnalyzerConfigDocumentText(textDocument.Id, newText, PreservationMode.PreserveValue),
                    _ => throw ExceptionUtilities.UnexpectedValue(textDocument),
                };
            }
        }

        updatedSolution = RemoveDocuments(updatedSolution, documentsToRemove);

        return SetCurrentSolution(updatedSolution);
    }

    private static Solution RemoveDocuments(Solution solution, IEnumerable<DocumentId> ids)
        => solution
        .RemoveDocuments([.. ids.Where(id => solution.GetDocument(id) != null)])
        .RemoveAdditionalDocuments([.. ids.Where(id => solution.GetAdditionalDocument(id) != null)])
        .RemoveAnalyzerConfigDocuments([.. ids.Where(id => solution.GetAnalyzerConfigDocument(id) != null)]);

    private static async ValueTask<SourceText> GetSourceTextAsync(string filePath, Encoding encoding, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
    {
        var zeroLengthRetryPerformed = false;
        for (var attemptIndex = 0; attemptIndex < 6; attemptIndex++)
        {
            try
            {
                // File.OpenRead opens the file with FileShare.Read. This may prevent IDEs from saving file
                // contents to disk
                SourceText sourceText;
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    sourceText = SourceText.From(stream, encoding, checksumAlgorithm);
                }

                if (!zeroLengthRetryPerformed && sourceText.Length == 0)
                {
                    zeroLengthRetryPerformed = true;

                    // VSCode (on Windows) will sometimes perform two separate writes when updating a file on disk.
                    // In the first update, it clears the file contents, and in the second, it writes the intended
                    // content.
                    // It's atypical that a file being watched for hot reload would be empty. We'll use this as a
                    // hueristic to identify this case and perform an additional retry reading the file after a delay.
                    await Task.Delay(20, cancellationToken).ConfigureAwait(false);

                    using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    sourceText = SourceText.From(stream, encoding, checksumAlgorithm);
                }

                return sourceText;
            }
            catch (IOException) when (attemptIndex < 5)
            {
                await Task.Delay(20 * (attemptIndex + 1), cancellationToken).ConfigureAwait(false);
            }
        }

        throw ExceptionUtilities.Unreachable();
    }
}
