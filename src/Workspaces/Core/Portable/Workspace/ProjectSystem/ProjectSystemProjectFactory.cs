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
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed class ProjectSystemProjectFactory
{
    /// <summary>
    /// The main gate to synchronize updates to this solution.
    /// </summary>
    /// <remarks>
    /// See the Readme.md in this directory for further comments about threading in this area.
    /// </remarks>
    // TODO: we should be able to get rid of this gate in favor of just calling the various workspace methods that acquire the Workspace's
    // serialization lock and then allow us to update our own state under that lock.
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

    public Workspace Workspace { get; }
    public IAsynchronousOperationListener WorkspaceListener { get; }
    public IFileChangeWatcher FileChangeWatcher { get; }
    public FileWatchedPortableExecutableReferenceFactory FileWatchedReferenceFactory { get; }

    private readonly Func<bool, ImmutableArray<string>, Task> _onDocumentsAddedMaybeAsync;
    private readonly Action<Project> _onProjectRemoved;

    /// <summary>
    /// A set of documents that were added by <see cref="ProjectSystemProject.AddSourceTextContainer"/>, and aren't otherwise
    /// tracked for opening/closing.
    /// </summary>
    public ImmutableHashSet<DocumentId> DocumentsNotFromFiles { get; private set; } = [];

    /// <remarks>Should be updated with <see cref="ImmutableInterlocked"/>.</remarks>
    private ImmutableDictionary<ProjectId, string?> _projectToMaxSupportedLangVersionMap = ImmutableDictionary<ProjectId, string?>.Empty;

    /// <remarks>Should be updated with <see cref="ImmutableInterlocked"/>.</remarks>
    private ImmutableDictionary<ProjectId, string> _projectToDependencyNodeTargetIdentifier = ImmutableDictionary<ProjectId, string>.Empty;

    /// <summary>
    /// Set by the host if the solution is currently closing; this can be used to optimize some things there.
    /// </summary>
    public bool SolutionClosing { get; set; }

    /// <summary>
    /// The current path to the solution. Currently this is only used to update the solution path when the first project is added -- we don't have a concept
    /// of the solution path changing in the middle while a bunch of projects are loaded.
    /// </summary>
    public string? SolutionPath { get; set; }
    public Guid SolutionTelemetryId { get; set; }

    public ProjectSystemProjectFactory(Workspace workspace, IFileChangeWatcher fileChangeWatcher, Func<bool, ImmutableArray<string>, Task> onDocumentsAddedMaybeAsync, Action<Project> onProjectRemoved)
    {
        Workspace = workspace;
        WorkspaceListener = workspace.Services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>().GetListener();

        FileChangeWatcher = fileChangeWatcher;
        FileWatchedReferenceFactory = new FileWatchedPortableExecutableReferenceFactory(workspace.Services.SolutionServices, fileChangeWatcher);
        FileWatchedReferenceFactory.ReferenceChanged += this.StartRefreshingMetadataReferencesForFile;

        _onDocumentsAddedMaybeAsync = onDocumentsAddedMaybeAsync;
        _onProjectRemoved = onProjectRemoved;
    }

    public FileTextLoader CreateFileTextLoader(string fullPath)
        => new WorkspaceFileTextLoader(this.Workspace.Services.SolutionServices, fullPath, defaultEncoding: null);

    public async Task<ProjectSystemProject> CreateAndAddToWorkspaceAsync(string projectSystemName, string language, ProjectSystemProjectCreationInfo creationInfo, ProjectSystemHostInfo hostInfo)
    {
        var projectId = ProjectId.CreateNewId(projectSystemName);
        var assemblyName = creationInfo.AssemblyName ?? projectSystemName;

        // We will use the project system name as the default display name of the project
        var project = new ProjectSystemProject(
            this,
            hostInfo,
            projectId,
            displayName: projectSystemName,
            language,
            assemblyName,
            creationInfo.CompilationOptions,
            creationInfo.FilePath,
            creationInfo.ParseOptions);

        var versionStamp = creationInfo.FilePath != null
            ? VersionStamp.Create(File.GetLastWriteTimeUtc(creationInfo.FilePath))
            : VersionStamp.Create();

        var projectInfo = ProjectInfo.Create(
            new ProjectInfo.ProjectAttributes(
                projectId,
                versionStamp,
                name: projectSystemName,
                assemblyName,
                language,
                compilationOutputFilePaths: default, // will be updated when command line is set
                SourceHashAlgorithms.Default, // will be updated when command line is set
                filePath: creationInfo.FilePath,
                telemetryId: creationInfo.TelemetryId),
            compilationOptions: creationInfo.CompilationOptions,
            parseOptions: creationInfo.ParseOptions);

        await ApplyChangeToWorkspaceAsync(w =>
        {
            // We call the synchronous SetCurrentSolution which is fine here since we've already acquired our outer lock so this will
            // never block. But once we remove the ProjectSystemProjectFactory lock in favor of everybody calling the newer overloads of
            // SetCurrentSolution, this should become async again.
            w.SetCurrentSolution(
                oldSolution =>
                {
                    // If we don't have any projects and this is our first project being added, then we'll create a
                    // new SolutionId and count this as the solution being added so that event is raised.
                    if (oldSolution.ProjectIds.Count == 0)
                    {
                        var solutionInfo = SolutionInfo.Create(
                            SolutionId.CreateNewId(SolutionPath),
                            VersionStamp.Create(),
                            SolutionPath,
                            projects: [projectInfo],
                            analyzerReferences: w.CurrentSolution.AnalyzerReferences).WithTelemetryId(SolutionTelemetryId);
                        var newSolution = w.CreateSolution(solutionInfo);

                        foreach (var project in solutionInfo.Projects)
                            newSolution = newSolution.AddProject(project);

                        return newSolution;
                    }
                    else
                    {
                        return oldSolution.AddProject(projectInfo);
                    }
                },
                (oldSolution, newSolution) =>
                {
                    return oldSolution.ProjectIds.Count == 0
                        ? (WorkspaceChangeKind.SolutionAdded, projectId: null, documentId: null)
                        : (WorkspaceChangeKind.ProjectAdded, projectId, documentId: null);
                },
                onBeforeUpdate: null,
                onAfterUpdate: null);
        }).ConfigureAwait(false);

        return project;
    }

    public string? TryGetDependencyNodeTargetIdentifier(ProjectId projectId)
    {
        // This doesn't take a lock since _projectToDependencyNodeTargetIdentifier is immutable
        _projectToDependencyNodeTargetIdentifier.TryGetValue(projectId, out var identifier);
        return identifier;
    }

    public string? TryGetMaxSupportedLanguageVersion(ProjectId projectId)
    {
        // This doesn't take a lock since _projectToMaxSupportedLangVersionMap is immutable
        _projectToMaxSupportedLangVersionMap.TryGetValue(projectId, out var identifier);
        return identifier;
    }

    internal void AddDocumentToDocumentsNotFromFiles_NoLock(DocumentId documentId)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        DocumentsNotFromFiles = DocumentsNotFromFiles.Add(documentId);
    }

    internal void RemoveDocumentToDocumentsNotFromFiles_NoLock(DocumentId documentId)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);
        DocumentsNotFromFiles = DocumentsNotFromFiles.Remove(documentId);
    }
    /// <summary>
    /// Applies a single operation to the workspace. <paramref name="action"/> should be a call to one of the protected Workspace.On* methods.
    /// </summary>
    public void ApplyChangeToWorkspace(Action<Workspace> action)
    {
        using (_gate.DisposableWait())
        {
            action(Workspace);
        }
    }

    /// <summary>
    /// Applies a single operation to the workspace. <paramref name="action"/> should be a call to one of the protected Workspace.On* methods.
    /// </summary>
    public async ValueTask ApplyChangeToWorkspaceAsync(Action<Workspace> action, CancellationToken cancellationToken = default)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
        {
            action(Workspace);
        }
    }

    /// <summary>
    /// Applies a single operation to the workspace. <paramref name="action"/> should be a call to one of the protected Workspace.On* methods.
    /// </summary>
    public async ValueTask ApplyChangeToWorkspaceMaybeAsync(bool useAsync, Action<Workspace> action)
    {
        using (useAsync ? await _gate.DisposableWaitAsync().ConfigureAwait(false) : _gate.DisposableWait())
        {
            action(Workspace);
        }
    }

    /// <summary>
    /// Applies a solution transformation to the workspace and triggers workspace changed event for specified <paramref name="projectId"/>.
    /// The transformation shall only update the project of the solution with the specified <paramref name="projectId"/>.
    /// </summary>
    public void ApplyChangeToWorkspace(ProjectId projectId, Func<CodeAnalysis.Solution, CodeAnalysis.Solution> solutionTransformation)
    {
        using (_gate.DisposableWait())
        {
            Workspace.SetCurrentSolution(solutionTransformation, WorkspaceChangeKind.ProjectChanged, projectId);
        }
    }

    /// <inheritdoc cref="ApplyBatchChangeToWorkspaceMaybeAsync(bool, Action{SolutionChangeAccumulator})"/>
    public void ApplyBatchChangeToWorkspace(Action<SolutionChangeAccumulator> mutation)
    {
        ApplyBatchChangeToWorkspaceMaybeAsync(useAsync: false, mutation).VerifyCompleted();
    }

    /// <inheritdoc cref="ApplyBatchChangeToWorkspaceMaybeAsync(bool, Action{SolutionChangeAccumulator})"/>
    public Task ApplyBatchChangeToWorkspaceAsync(Action<SolutionChangeAccumulator> mutation)
    {
        return ApplyBatchChangeToWorkspaceMaybeAsync(useAsync: true, mutation);
    }

    /// <summary>
    /// Applies a change to the workspace that can do any number of project changes.
    /// </summary>
    /// <remarks>This is needed to synchronize with <see cref="ApplyChangeToWorkspace(Action{Workspace})" /> to avoid any races. This
    /// method could be moved down to the core Workspace layer and then could use the synchronization lock there.</remarks>
    public async Task ApplyBatchChangeToWorkspaceMaybeAsync(bool useAsync, Action<SolutionChangeAccumulator> mutation)
    {
        using (useAsync ? await _gate.DisposableWaitAsync().ConfigureAwait(false) : _gate.DisposableWait())
        {
            // We need the data from the accumulator across the lambda callbacks to SetCurrentSolutionAsync, so declare
            // it here. It will be assigned in `transformation:` below (which may happen multiple times if the
            // transformation needs to rerun).  Once the transformation succeeds and is applied, the
            // 'onBeforeUpdate/onAfterUpdate' callbacks will be called, and can use the last assigned value in
            // `transformation`.
            SolutionChangeAccumulator solutionChanges = null!;

            await Workspace.SetCurrentSolutionAsync(
                useAsync,
                transformation: oldSolution =>
                {
                    solutionChanges = new SolutionChangeAccumulator(oldSolution);
                    mutation(solutionChanges);

                    // Note: If the accumulator showed no changes it will return oldSolution.  This ensures that
                    // SetCurrentSolutionAsync bails out immediately and no further work is done.
                    return solutionChanges.Solution;
                },
                changeKind: (_, _) => (solutionChanges.WorkspaceChangeKind, solutionChanges.WorkspaceChangeProjectId, solutionChanges.WorkspaceChangeDocumentId),
                onBeforeUpdate: (_, _) =>
                {
                    // Clear out mutable state not associated with the solution snapshot (for example, which documents are
                    // currently open).
                    foreach (var documentId in solutionChanges.DocumentIdsRemoved)
                        Workspace.ClearDocumentData(documentId);
                },
                onAfterUpdate: null,
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    private void ApplyBatchChangeToWorkspace_NoLock(SolutionChangeAccumulator solutionChanges)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        if (!solutionChanges.HasChange)
            return;

        Workspace.SetCurrentSolution(
            _ => solutionChanges.Solution,
            solutionChanges.WorkspaceChangeKind,
            solutionChanges.WorkspaceChangeProjectId,
            solutionChanges.WorkspaceChangeDocumentId,
            onBeforeUpdate: (_, _) =>
            {
                // Clear out mutable state not associated with the solution snapshot (for example, which documents are
                // currently open).
                foreach (var documentId in solutionChanges.DocumentIdsRemoved)
                    Workspace.ClearDocumentData(documentId);
            });
    }

    private readonly Dictionary<ProjectId, ProjectReferenceInformation> _projectReferenceInfoMap = [];

    private ProjectReferenceInformation GetReferenceInfo_NoLock(ProjectId projectId)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        return _projectReferenceInfoMap.GetOrAdd(projectId, _ => new ProjectReferenceInformation());
    }

    /// <summary>
    /// Removes the project from the various maps this type maintains; it's still up to the caller to actually remove
    /// the project in one way or another.
    /// </summary>
    internal void RemoveProjectFromTrackingMaps_NoLock(ProjectId projectId)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        var project = Workspace.CurrentSolution.GetRequiredProject(projectId);

        if (_projectReferenceInfoMap.TryGetValue(projectId, out var projectReferenceInfo))
        {
            // If we still had any output paths, we'll want to remove them to cause conversion back to metadata references.
            // The call below implicitly is modifying the collection we've fetched, so we'll make a copy.
            var solutionChanges = new SolutionChangeAccumulator(Workspace.CurrentSolution);

            foreach (var outputPath in projectReferenceInfo.OutputPaths.ToList())
            {
                RemoveProjectOutputPath_NoLock(solutionChanges, projectId, outputPath);
            }

            ApplyBatchChangeToWorkspace_NoLock(solutionChanges);

            _projectReferenceInfoMap.Remove(projectId);
        }

        ImmutableInterlocked.TryRemove<ProjectId, string?>(ref _projectToMaxSupportedLangVersionMap, projectId, out _);
        ImmutableInterlocked.TryRemove(ref _projectToDependencyNodeTargetIdentifier, projectId, out _);

        _onProjectRemoved?.Invoke(project);
    }

    internal void RemoveSolution_NoLock()
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        // At this point, we should have had RemoveProjectFromTrackingMaps_NoLock called for everything else, so it's just the solution itself
        // to clean up
        Contract.ThrowIfFalse(_projectReferenceInfoMap.Count == 0);
        Contract.ThrowIfFalse(_projectToMaxSupportedLangVersionMap.Count == 0);
        Contract.ThrowIfFalse(_projectToDependencyNodeTargetIdentifier.Count == 0);

        // Create a new empty solution and set this; we will reuse the same SolutionId and path since components
        // still may have persistence information they still need to look up by that location; we also keep the
        // existing analyzer references around since those are host-level analyzers that were loaded asynchronously.

        Workspace.SetCurrentSolution(
            solution => Workspace.CreateSolution(
                SolutionInfo.Create(
                    SolutionId.CreateNewId(),
                    VersionStamp.Create(),
                    analyzerReferences: solution.AnalyzerReferences)),
            WorkspaceChangeKind.SolutionRemoved,
            onBeforeUpdate: (_, _) =>
            {
                Workspace.ClearOpenDocuments();
            });
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/54137", AllowLocks = false)]
    internal void SetMaxLanguageVersion(ProjectId projectId, string? maxLanguageVersion)
    {
        ImmutableInterlocked.Update(
            ref _projectToMaxSupportedLangVersionMap,
            static (map, arg) => map.SetItem(arg.projectId, arg.maxLanguageVersion),
            (projectId, maxLanguageVersion));
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/54135", AllowLocks = false)]
    internal void SetDependencyNodeTargetIdentifier(ProjectId projectId, string targetIdentifier)
    {
        ImmutableInterlocked.Update(
            ref _projectToDependencyNodeTargetIdentifier,
            static (map, arg) => map.SetItem(arg.projectId, arg.targetIdentifier),
            (projectId, targetIdentifier));
    }

    private sealed class ProjectReferenceInformation
    {
        public readonly List<string> OutputPaths = [];
        public readonly List<(string path, ProjectReference projectReference)> ConvertedProjectReferences = [];
    }

    /// <summary>
    /// A multimap from an output path to the project outputting to it. Ideally, this shouldn't ever
    /// actually be a true multimap, since we shouldn't have two projects outputting to the same path, but
    /// any bug by a project adding the wrong output path means we could end up with some duplication.
    /// In that case, we'll temporarily have two until (hopefully) somebody removes it.
    /// </summary>
    private readonly Dictionary<string, List<ProjectId>> _projectsByOutputPath = new(StringComparer.OrdinalIgnoreCase);

    public void AddProjectOutputPath_NoLock(SolutionChangeAccumulator solutionChanges, ProjectId projectId, string outputPath)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        var projectReferenceInformation = GetReferenceInfo_NoLock(projectId);

        projectReferenceInformation.OutputPaths.Add(outputPath);
        _projectsByOutputPath.MultiAdd(outputPath, projectId);

        var projectsForOutputPath = _projectsByOutputPath[outputPath];
        var distinctProjectsForOutputPath = projectsForOutputPath.Distinct().ToList();

        // If we have exactly one, then we're definitely good to convert
        if (projectsForOutputPath.Count == 1)
        {
            ConvertMetadataReferencesToProjectReferences_NoLock(solutionChanges, projectId, outputPath);
        }
        else if (distinctProjectsForOutputPath.Count == 1)
        {
            // The same project has multiple output paths that are the same. Any project would have already been converted
            // by the prior add, so nothing further to do
        }
        else
        {
            // We have more than one project outputting to the same path. This shouldn't happen but we'll convert back
            // because now we don't know which project to reference.
            foreach (var otherProjectId in projectsForOutputPath)
            {
                // We know that since we're adding a path to projectId and we're here that we couldn't have already
                // had a converted reference to us, instead we need to convert things that are pointing to the project
                // we're colliding with
                if (otherProjectId != projectId)
                {
                    ConvertProjectReferencesToMetadataReferences_NoLock(solutionChanges, otherProjectId, outputPath);
                }
            }
        }
    }

    /// <summary>
    /// Attempts to convert all metadata references to <paramref name="outputPath"/> to a project reference to <paramref name="projectIdToReference"/>.
    /// </summary>
    /// <param name="projectIdToReference">The <see cref="ProjectId"/> of the project that could be referenced in place of the output path.</param>
    /// <param name="outputPath">The output path to replace.</param>
    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/31306",
        Constraint = "Avoid calling " + nameof(CodeAnalysis.Solution.GetProject) + " to avoid realizing all projects.")]
    private void ConvertMetadataReferencesToProjectReferences_NoLock(SolutionChangeAccumulator solutionChanges, ProjectId projectIdToReference, string outputPath)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        foreach (var projectIdToRetarget in solutionChanges.Solution.ProjectIds)
        {
            if (CanConvertMetadataReferenceToProjectReference(solutionChanges.Solution, projectIdToRetarget, referencedProjectId: projectIdToReference))
            {
                // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
                // Project instances to get created.
                foreach (PortableExecutableReference reference in solutionChanges.Solution.GetProjectState(projectIdToRetarget)!.MetadataReferences)
                {
                    if (string.Equals(reference.FilePath, outputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        FileWatchedReferenceFactory.StopWatchingReference(reference);

                        var projectReference = new ProjectReference(projectIdToReference, reference.Properties.Aliases, reference.Properties.EmbedInteropTypes);
                        var newSolution = solutionChanges.Solution.RemoveMetadataReference(projectIdToRetarget, reference)
                                                                  .AddProjectReference(projectIdToRetarget, projectReference);

                        solutionChanges.UpdateSolutionForProjectAction(projectIdToRetarget, newSolution);

                        GetReferenceInfo_NoLock(projectIdToRetarget).ConvertedProjectReferences.Add(
                            (reference.FilePath!, projectReference));

                        // We have converted one, but you could have more than one reference with different aliases
                        // that we need to convert, so we'll keep going
                    }
                }
            }
        }
    }

    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/31306",
        Constraint = "Avoid calling " + nameof(CodeAnalysis.Solution.GetProject) + " to avoid realizing all projects.")]
    private static bool CanConvertMetadataReferenceToProjectReference(Solution solution, ProjectId projectIdWithMetadataReference, ProjectId referencedProjectId)
    {
        // We can never make a project reference ourselves. This isn't a meaningful scenario, but if somebody does this by accident
        // we do want to throw exceptions.
        if (projectIdWithMetadataReference == referencedProjectId)
        {
            return false;
        }

        // PERF: call GetProjectState instead of GetProject, otherwise creating a new project might force all
        // Project instances to get created.
        var projectWithMetadataReference = solution.GetProjectState(projectIdWithMetadataReference);
        var referencedProject = solution.GetProjectState(referencedProjectId);

        Contract.ThrowIfNull(projectWithMetadataReference);
        Contract.ThrowIfNull(referencedProject);

        // We don't want to convert a metadata reference to a project reference if the project being referenced isn't something
        // we can create a Compilation for. For example, if we have a C# project, and it's referencing a F# project via a metadata reference
        // everything would be fine if we left it a metadata reference. Converting it to a project reference means we couldn't create a Compilation
        // anymore in the IDE, since the C# compilation would need to reference an F# compilation. F# projects referencing other F# projects though
        // do expect this to work, and so we'll always allow references through of the same language.
        if (projectWithMetadataReference.Language != referencedProject.Language)
        {
            if (projectWithMetadataReference.LanguageServices.GetService<ICompilationFactoryService>() != null &&
                referencedProject.LanguageServices.GetService<ICompilationFactoryService>() == null)
            {
                // We're referencing something that we can't create a compilation from something that can, so keep the metadata reference
                return false;
            }
        }

        // If this is going to cause a circular reference, also disallow it
        if (solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(referencedProjectId).Contains(projectIdWithMetadataReference))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds all projects that had a project reference to <paramref name="projectId"/> and convert it back to a metadata reference.
    /// </summary>
    /// <param name="projectId">The <see cref="ProjectId"/> of the project being referenced.</param>
    /// <param name="outputPath">The output path of the given project to remove the link to.</param>
    [PerformanceSensitive(
        "https://github.com/dotnet/roslyn/issues/37616",
        Constraint = "Update ConvertedProjectReferences in place to avoid duplicate list allocations.")]
    private void ConvertProjectReferencesToMetadataReferences_NoLock(SolutionChangeAccumulator solutionChanges, ProjectId projectId, string outputPath)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        foreach (var projectIdToRetarget in solutionChanges.Solution.ProjectIds)
        {
            var referenceInfo = GetReferenceInfo_NoLock(projectIdToRetarget);

            // Update ConvertedProjectReferences in place to avoid duplicate list allocations
            for (var i = 0; i < referenceInfo.ConvertedProjectReferences.Count; i++)
            {
                var convertedReference = referenceInfo.ConvertedProjectReferences[i];

                if (string.Equals(convertedReference.path, outputPath, StringComparison.OrdinalIgnoreCase) &&
                    convertedReference.projectReference.ProjectId == projectId)
                {
                    var metadataReference =
                        FileWatchedReferenceFactory.CreateReferenceAndStartWatchingFile(
                            convertedReference.path,
                            new MetadataReferenceProperties(
                                aliases: convertedReference.projectReference.Aliases,
                                embedInteropTypes: convertedReference.projectReference.EmbedInteropTypes));

                    var newSolution = solutionChanges.Solution.RemoveProjectReference(projectIdToRetarget, convertedReference.projectReference)
                                                              .AddMetadataReference(projectIdToRetarget, metadataReference);

                    solutionChanges.UpdateSolutionForProjectAction(projectIdToRetarget, newSolution);

                    referenceInfo.ConvertedProjectReferences.RemoveAt(i);

                    // We have converted one, but you could have more than one reference with different aliases
                    // that we need to convert, so we'll keep going. Make sure to decrement the index so we don't
                    // skip any items.
                    i--;
                }
            }
        }
    }

    public ProjectReference? TryCreateConvertedProjectReference_NoLock(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
    {
        // Any conversion to or from project references must be done under the global workspace lock,
        // since that needs to be coordinated with updating all projects simultaneously.
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        if (_projectsByOutputPath.TryGetValue(path, out var ids) && ids.Distinct().Count() == 1)
        {
            var projectIdToReference = ids.First();

            if (CanConvertMetadataReferenceToProjectReference(Workspace.CurrentSolution, referencingProject, projectIdToReference))
            {
                var projectReference = new ProjectReference(
                    projectIdToReference,
                    aliases: properties.Aliases,
                    embedInteropTypes: properties.EmbedInteropTypes);

                GetReferenceInfo_NoLock(referencingProject).ConvertedProjectReferences.Add((path, projectReference));

                return projectReference;
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public ProjectReference? TryRemoveConvertedProjectReference_NoLock(ProjectId referencingProject, string path, MetadataReferenceProperties properties)
    {
        // Any conversion to or from project references must be done under the global workspace lock,
        // since that needs to be coordinated with updating all projects simultaneously.
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        var projectReferenceInformation = GetReferenceInfo_NoLock(referencingProject);
        foreach (var convertedProject in projectReferenceInformation.ConvertedProjectReferences)
        {
            if (convertedProject.path == path &&
                convertedProject.projectReference.EmbedInteropTypes == properties.EmbedInteropTypes &&
                convertedProject.projectReference.Aliases.SequenceEqual(properties.Aliases))
            {
                projectReferenceInformation.ConvertedProjectReferences.Remove(convertedProject);
                return convertedProject.projectReference;
            }
        }

        return null;
    }

    public void RemoveProjectOutputPath_NoLock(SolutionChangeAccumulator solutionChanges, ProjectId projectId, string outputPath)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        var projectReferenceInformation = GetReferenceInfo_NoLock(projectId);
        if (!projectReferenceInformation.OutputPaths.Contains(outputPath))
        {
            throw new ArgumentException($"Project does not contain output path '{outputPath}'", nameof(outputPath));
        }

        projectReferenceInformation.OutputPaths.Remove(outputPath);
        _projectsByOutputPath.MultiRemove(outputPath, projectId);

        // When a project is closed, we may need to convert project references to metadata references (or vice
        // versa). Failure to convert the references could leave a project in the workspace with a project
        // reference to a project which is not open.
        //
        // For the specific case where the entire solution is closing, we do not need to update the state for
        // remaining projects as each project closes, because we know those projects will be closed without
        // further use. Avoiding reference conversion when the solution is closing improves performance for both
        // IDE close scenarios and solution reload scenarios that occur after complex branch switches.
        if (!SolutionClosing)
        {
            if (_projectsByOutputPath.TryGetValue(outputPath, out var remainingProjectsForOutputPath))
            {
                var distinctRemainingProjects = remainingProjectsForOutputPath.Distinct();
                if (distinctRemainingProjects.Count() == 1)
                {
                    // We had more than one project outputting to the same path. Now we're back down to one
                    // so we can reference that one again
                    ConvertMetadataReferencesToProjectReferences_NoLock(solutionChanges, distinctRemainingProjects.Single(), outputPath);
                }
            }
            else
            {
                // No projects left, we need to convert back to metadata references
                ConvertProjectReferencesToMetadataReferences_NoLock(solutionChanges, projectId, outputPath);
            }
        }
    }

#pragma warning disable VSTHRD100 // Avoid async void methods
    private async void StartRefreshingMetadataReferencesForFile(object? sender, string fullFilePath)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        using var asyncToken = WorkspaceListener.BeginAsyncOperation(nameof(StartRefreshingMetadataReferencesForFile));

        await ApplyBatchChangeToWorkspaceAsync(solutionChanges =>
        {
            foreach (var project in Workspace.CurrentSolution.Projects)
            {
                // Loop to find each reference with the given path. It's possible that there might be multiple references of the same path;
                // the project system could concievably add the same reference multiple times but with different aliases. It's also possible
                // we might not find the path at all: when we receive the file changed event, we aren't checking if the file is still
                // in the workspace at that time; it's possible it might have already been removed.
                foreach (var portableExecutableReference in project.MetadataReferences.OfType<PortableExecutableReference>())
                {
                    if (portableExecutableReference.FilePath == fullFilePath)
                    {
                        FileWatchedReferenceFactory.StopWatchingReference(portableExecutableReference);

                        var newPortableExecutableReference =
                            FileWatchedReferenceFactory.CreateReferenceAndStartWatchingFile(
                                portableExecutableReference.FilePath,
                                portableExecutableReference.Properties);

                        var newSolution = solutionChanges.Solution.RemoveMetadataReference(project.Id, portableExecutableReference)
                                                                    .AddMetadataReference(project.Id, newPortableExecutableReference);

                        solutionChanges.UpdateSolutionForProjectAction(project.Id, newSolution);

                    }
                }
            }
        }).ConfigureAwait(false);
    }

    internal Task RaiseOnDocumentsAddedMaybeAsync(bool useAsync, ImmutableArray<string> filePaths)
    {
        return _onDocumentsAddedMaybeAsync(useAsync, filePaths);
    }
}
