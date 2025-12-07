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
using Microsoft.Build.Graph;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.HotReload.Internal;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.CodeAnalysis.ExternalAccess.HotReload.Api;

internal sealed class HotReloadMSBuildWorkspace : Workspace
{
    private readonly ILogger _logger;
    private readonly MSBuildProjectLoader _loader;

    private int _solutionUpdateId;
    private ProjectGraphFileInfoProvider? _projectGraphFileInfoProvider;

    public HotReloadMSBuildWorkspace(ILogger logger)
        : base(MSBuildMefHostServices.DefaultServices, WorkspaceKind.MSBuild)
    {
#pragma warning disable CS0618 // https://github.com/dotnet/sdk/issues/49725
        WorkspaceFailed += (_sender, diag) =>
        {
            // Report both Warning and Failure as warnings.
            // MSBuildProjectLoader reports Failures for cases where we can safely continue loading projects
            // (e.g. non-C#/VB project is ignored).
            // https://github.com/dotnet/roslyn/issues/75170
            logger.LogWarning($"msbuild: {diag.Diagnostic}");
        };
#pragma warning restore CS0618

        _logger = logger;
        _loader = new MSBuildProjectLoader(this);
    }

    public void UpdateGraph(ProjectGraph graph)
    {
        _projectGraphFileInfoProvider = new ProjectGraphFileInfoProvider(graph, _loader.ProjectFileExtensionRegistry);
    }

    public async Task UpdateProjectConeAsync(string projectPath, string baseDirectory, CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(_projectGraphFileInfoProvider);

        _logger.LogInformation("Loading projects ...");

        var stopwatch = Stopwatch.StartNew();
        var oldSolution = CurrentSolution;

        var projectMap = ProjectMap.Create();

        ImmutableArray<ProjectInfo> projectInfos;
        try
        {
            projectInfos = await _loader.LoadInfosAsync(
                [projectPath],
                baseDirectory,
                _projectGraphFileInfoProvider,
                projectMap,
                progress: null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // TODO: workaround for https://github.com/dotnet/roslyn/issues/75956
            projectInfos = [];
        }

        var oldProjectIdsByPath = oldSolution.Projects.ToDictionary(keySelector: static p => (p.FilePath!, p.Name), elementSelector: static p => p.Id);

        // Map new project id to the corresponding old one based on file path and project name (includes TFM), if it exists, and null for added projects.
        // Deleted projects won't be included in this map.
        var projectIdMap = projectInfos.ToDictionary(
            keySelector: static info => info.Id,
            elementSelector: info => oldProjectIdsByPath.TryGetValue((info.FilePath!, info.Name), out var oldProjectId) ? oldProjectId : null);

        var newSolution = oldSolution;

        foreach (var newProjectInfo in projectInfos)
        {
            Debug.Assert(newProjectInfo.FilePath != null);

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
                .WithAnalyzerConfigDocuments(MapDocuments(oldProjectId, newProjectInfo.AnalyzerConfigDocuments))
                .WithCompilationOutputInfo(newProjectInfo.CompilationOutputInfo));
        }

        await UpdateSolutionAsync(newSolution, operationDisplayName: "project update", cancellationToken).ConfigureAwait(false);
        UpdateReferencesAfterAdd();

        _logger.LogInformation("Projects loaded in {Time}s.", stopwatch.Elapsed.TotalSeconds.ToString("0.0"));

        ProjectReference MapProjectReference(ProjectReference pr)
            // Only C# and VB projects are loaded by the MSBuildProjectLoader, so some references might be missing.
            // When a new project is added along with a new project reference the old project id is also null.
            => new(projectIdMap.TryGetValue(pr.ProjectId, out var oldProjectId) && oldProjectId != null ? oldProjectId : pr.ProjectId, pr.Aliases, pr.EmbedInteropTypes);

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

    public async ValueTask UpdateFileContentAsync(IEnumerable<(string path, HotReloadFileChangeKind change)> changedFiles, CancellationToken cancellationToken)
    {
        var updatedSolution = CurrentSolution;

        var documentsToRemove = new List<DocumentId>();

        foreach (var (path, change) in changedFiles)
        {
            // when a file is added we reevaluate the project:
            Debug.Assert(change != HotReloadFileChangeKind.Add);

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
                    _ => throw new InvalidOperationException()
                };
            }
        }

        updatedSolution = RemoveDocuments(updatedSolution, documentsToRemove);

        await UpdateSolutionAsync(updatedSolution, operationDisplayName: "document update", cancellationToken).ConfigureAwait(false);
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
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

    private Task UpdateSolutionAsync(Solution newSolution, string operationDisplayName, CancellationToken cancellationToken)
        => ReportSolutionFilesAsync(SetCurrentSolution(newSolution), Interlocked.Increment(ref _solutionUpdateId), operationDisplayName, cancellationToken);

    private async Task ReportSolutionFilesAsync(Solution solution, int updateId, string operationDisplayName, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Solution after {Operation}: v{Version}", operationDisplayName, updateId);

        if (!_logger.IsEnabled(LogLevel.Trace))
        {
            return;
        }

        foreach (var project in solution.Projects)
        {
            _logger.LogDebug("  Project: {Path}", project.FilePath);

            foreach (var document in project.Documents)
            {
                await InspectDocumentAsync(document, "Document").ConfigureAwait(false);
            }

            foreach (var document in project.AdditionalDocuments)
            {
                await InspectDocumentAsync(document, "Additional").ConfigureAwait(false);
            }

            foreach (var document in project.AnalyzerConfigDocuments)
            {
                await InspectDocumentAsync(document, "Config").ConfigureAwait(false);
            }
        }

        async ValueTask InspectDocumentAsync(TextDocument document, string kind)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("    {Kind}: {FilePath} [{Checksum}]", kind, document.FilePath, Convert.ToBase64String(text.GetChecksum().ToArray()));
        }
    }
}
