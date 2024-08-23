// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Workspaces.ProjectSystem;

internal sealed partial class ProjectSystemProjectFactory
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

    /// <summary>
    /// Stores the latest state of the project system factory.
    /// Access to this is synchronized via <see cref="_gate"/>
    /// </summary>
    private ProjectUpdateState _projectUpdateState = ProjectUpdateState.Empty;

    public Workspace Workspace { get; }
    public IAsynchronousOperationListener WorkspaceListener { get; }
    public IFileChangeWatcher FileChangeWatcher { get; }

    public FileWatchedReferenceFactory<PortableExecutableReference> FileWatchedPortableExecutableReferenceFactory { get; }
    public FileWatchedReferenceFactory<AnalyzerFileReference> FileWatchedAnalyzerReferenceFactory { get; }

    public SolutionServices SolutionServices => this.Workspace.Services.SolutionServices;

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
        FileChangeWatcher = fileChangeWatcher;

        _onDocumentsAddedMaybeAsync = onDocumentsAddedMaybeAsync;
        _onProjectRemoved = onProjectRemoved;

        WorkspaceListener = this.SolutionServices.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>().GetListener();

        FileWatchedPortableExecutableReferenceFactory = new(fileChangeWatcher);
        FileWatchedPortableExecutableReferenceFactory.ReferenceChanged += this.StartRefreshingMetadataReferencesForFile;

        FileWatchedAnalyzerReferenceFactory = new(fileChangeWatcher);
        FileWatchedAnalyzerReferenceFactory.ReferenceChanged += this.StartRefreshingAnalyzerReferenceForFile;
    }

    public FileTextLoader CreateFileTextLoader(string fullPath)
        => new WorkspaceFileTextLoader(this.SolutionServices, fullPath, defaultEncoding: null);

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
                compilationOutputInfo: new(creationInfo.CompilationOutputAssemblyFilePath),
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

                        using var _ = ArrayBuilder<ProjectInfo>.GetInstance(out var projectInfos);
                        projectInfos.AddRange(solutionInfo.Projects);
                        newSolution = newSolution.AddProjects(projectInfos);

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

        // Set this value early after solution is created so it is available to Razor.  This will get updated
        // when the command line is set, but we want a non-null value to be available as soon as possible.
        //
        // Set the property in a batch; if we set the property directly we'll be taking a synchronous lock here and
        // potentially block up thread pool threads. Doing this in a batch means the global lock will be acquired asynchronously.
        var disposableBatchScope = await project.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using var _ = disposableBatchScope.ConfigureAwait(false);
        project.CompilationOutputAssemblyFilePath = creationInfo.CompilationOutputAssemblyFilePath;

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
    /// Applies a single operation to the workspace that also needs to update the <see cref="_projectUpdateState"/>.
    /// <paramref name="action"/> should be a call to one of the protected Workspace.On* methods.
    /// </summary>
    public void ApplyChangeToWorkspaceWithProjectUpdateState(Func<Workspace, ProjectUpdateState, ProjectUpdateState> action)
    {
        using (_gate.DisposableWait())
        {
            var projectUpdateState = action(Workspace, _projectUpdateState);
            ApplyProjectUpdateState(projectUpdateState);
        }
    }

    /// <summary>
    /// Applies a solution transformation to the workspace and triggers workspace changed event for specified <paramref name="projectId"/>.
    /// The transformation shall only update the project of the solution with the specified <paramref name="projectId"/>.
    /// 
    /// The <paramref name="solutionTransformation"/> function must be safe to be attempted multiple times (and not update local state).
    /// </summary>
    public void ApplyChangeToWorkspace(ProjectId projectId, Func<CodeAnalysis.Solution, CodeAnalysis.Solution> solutionTransformation)
    {
        using (_gate.DisposableWait())
        {
            Workspace.SetCurrentSolution(solutionTransformation, WorkspaceChangeKind.ProjectChanged, projectId);
        }
    }

    /// <inheritdoc cref="ApplyBatchChangeToWorkspaceAsync(Func{SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState}, Action{ProjectUpdateState}?)"/>
    public void ApplyBatchChangeToWorkspace(Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState> mutation, Action<ProjectUpdateState>? onAfterUpdateAlways)
    {
        ApplyBatchChangeToWorkspaceMaybeAsync(useAsync: false, mutation, onAfterUpdateAlways).VerifyCompleted();
    }

    /// <inheritdoc cref="ApplyBatchChangeToWorkspaceAsync(Func{SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState}, Action{ProjectUpdateState}?)"/>
    public Task ApplyBatchChangeToWorkspaceAsync(Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState> mutation, Action<ProjectUpdateState>? onAfterUpdateAlways)
    {
        return ApplyBatchChangeToWorkspaceMaybeAsync(useAsync: true, mutation, onAfterUpdateAlways);
    }

    /// <inheritdoc cref="ApplyBatchChangeToWorkspaceAsync(Func{SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState}, Action{ProjectUpdateState}?)"/>
    public async Task ApplyBatchChangeToWorkspaceMaybeAsync(bool useAsync, Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState> mutation, Action<ProjectUpdateState>? onAfterUpdateAlways)
    {
        using (useAsync ? await _gate.DisposableWaitAsync().ConfigureAwait(false) : _gate.DisposableWait())
        {
            await ApplyBatchChangeToWorkspaceMaybe_NoLockAsync(useAsync, mutation, onAfterUpdateAlways).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Applies a change to the workspace that can do any number of project changes.
    /// The mutation action must be safe to attempt multiple times, in case there are interceding solution changes.
    /// If outside changes need to run under the global lock and run only once, they should use the <paramref name="onAfterUpdateAlways"/> action.
    /// <paramref name="onAfterUpdateAlways"/> will always run even if the transformation applied no changes.
    /// </summary>
    /// <remarks>This is needed to synchronize with <see cref="ApplyChangeToWorkspace(Action{Workspace})" /> to avoid any races. This
    /// method could be moved down to the core Workspace layer and then could use the synchronization lock there.</remarks>
    public async Task ApplyBatchChangeToWorkspaceMaybe_NoLockAsync(bool useAsync, Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState> mutation, Action<ProjectUpdateState>? onAfterUpdateAlways)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        // We need the data from the accumulator across the lambda callbacks to SetCurrentSolutionAsync, so declare
        // it here. It will be assigned in `transformation:` below (which may happen multiple times if the
        // transformation needs to rerun).  Once the transformation succeeds and is applied, the
        // 'onBeforeUpdate/onAfterUpdate' callbacks will be called, and can use the last assigned value in
        // `transformation`.
        SolutionChangeAccumulator solutionChanges = null!;
        ProjectUpdateState projectUpdateState = null!;

        var (didUpdate, newSolution) = await Workspace.SetCurrentSolutionAsync(
            useAsync,
            transformation: oldSolution =>
            {
                solutionChanges = new SolutionChangeAccumulator(oldSolution);

                // Use the _projectUpdateState here to ensure retries run with the original state.
                projectUpdateState = mutation(solutionChanges, _projectUpdateState);

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

        // Now that the project update has actually applied, we can apply the results of it.
        // For example saving the state and updating file watchers for added/removed references.
        //
        // Importantly this is not done inside the SetCurrentSolution onAfterUpdate as that
        // will only run *if* the transformation resulted in a changed solution, but this
        // must run regardless (it is possible we update maps, but did not end up actually changing the sln object) in the transformation.
        ApplyProjectUpdateState(projectUpdateState);
        onAfterUpdateAlways?.Invoke(projectUpdateState);
    }

    private void ApplyBatchChangeToWorkspace_NoLock(
        Func<SolutionChangeAccumulator, ProjectUpdateState, ProjectUpdateState> mutation, Action<ProjectUpdateState>? onAfterUpdateAlways)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        ApplyBatchChangeToWorkspaceMaybe_NoLockAsync(useAsync: false, mutation, onAfterUpdateAlways).VerifyCompleted();
    }

    private static ProjectUpdateState GetReferenceInformation(ProjectId projectId, ProjectUpdateState projectUpdateState, out ProjectReferenceInformation projectReference)
    {
        if (projectUpdateState.ProjectReferenceInfos.TryGetValue(projectId, out var referenceInfo))
        {
            projectReference = referenceInfo;
            return projectUpdateState;
        }
        else
        {
            projectReference = new ProjectReferenceInformation([], []);
            return projectUpdateState with
            {
                ProjectReferenceInfos = projectUpdateState.ProjectReferenceInfos.Add(projectId, projectReference)
            };
        }
    }

    /// <summary>
    /// Removes the project from the various maps this type maintains; it's still up to the caller to actually remove
    /// the project in one way or another.
    /// </summary>
    internal void RemoveProjectFromTrackingMaps_NoLock(ProjectId projectId)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        // This is set in the transformation function, but needs to be used by the onAfterUpdateAlways callback
        // so we define it here outside of the lambda.
        Project project = null!;

        ApplyBatchChangeToWorkspace_NoLock((solutionChanges, projectUpdateState) =>
        {
            project = Workspace.CurrentSolution.GetRequiredProject(projectId);

            if (projectUpdateState.ProjectReferenceInfos.TryGetValue(projectId, out var projectReferenceInfo))
            {
                // If we still had any output paths, we'll want to remove them to cause conversion back to metadata references.
                // The call below implicitly is modifying the collection we've fetched, so we'll make a copy.
                foreach (var outputPath in projectReferenceInfo.OutputPaths.ToList())
                {
                    projectUpdateState = RemoveProjectOutputPath_NoLock(solutionChanges, projectId, outputPath, projectUpdateState, SolutionClosing, SolutionServices);
                }

                projectUpdateState = projectUpdateState with
                {
                    ProjectReferenceInfos = projectUpdateState.ProjectReferenceInfos.Remove(projectId)
                };
            }

            return projectUpdateState;
        }, onAfterUpdateAlways: (projectUpdateState) =>
        {
            // This is called once after the above transformation is successfully applied.

            ImmutableInterlocked.TryRemove<ProjectId, string?>(ref _projectToMaxSupportedLangVersionMap, projectId, out _);
            ImmutableInterlocked.TryRemove(ref _projectToDependencyNodeTargetIdentifier, projectId, out _);

            _onProjectRemoved?.Invoke(project);
        });
    }

    internal void ApplyProjectUpdateState(ProjectUpdateState projectUpdateState)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        // Remove file watchers for any references we're no longer watching.
        foreach (var reference in projectUpdateState.RemovedMetadataReferences)
            FileWatchedPortableExecutableReferenceFactory.StopWatchingReference(reference.FilePath!, referenceToTrack: reference);

        // Add file watchers for any references we are now watching.
        foreach (var reference in projectUpdateState.AddedMetadataReferences)
            FileWatchedPortableExecutableReferenceFactory.StartWatchingReference(reference.FilePath!);

        // Remove file watchers for any references we're no longer watching.
        foreach (var referenceFullPath in projectUpdateState.RemovedAnalyzerReferences)
            FileWatchedAnalyzerReferenceFactory.StopWatchingReference(referenceFullPath, referenceToTrack: null);

        // Add file watchers for any references we are now watching.
        foreach (var referenceFullPath in projectUpdateState.AddedAnalyzerReferences)
            FileWatchedAnalyzerReferenceFactory.StartWatchingReference(referenceFullPath);

        // Clear the state from the this update in preparation for the next.
        projectUpdateState = projectUpdateState.ClearIncrementalState();
        _projectUpdateState = projectUpdateState;
    }

    internal void RemoveSolution_NoLock()
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        // At this point, we should have had RemoveProjectFromTrackingMaps_NoLock called for everything else, so it's just the solution itself
        // to clean up
        Contract.ThrowIfFalse(_projectUpdateState.ProjectReferenceInfos.Count == 0);
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

    public static ProjectUpdateState AddProjectOutputPath_NoLock(
        SolutionChangeAccumulator solutionChanges,
        ProjectId projectId,
        string outputPath,
        ProjectUpdateState projectUpdateState,
        SolutionServices solutionServices)
    {
        projectUpdateState = GetReferenceInformation(projectId, projectUpdateState, out var projectReferenceInformation);
        projectUpdateState = projectUpdateState.WithProjectReferenceInfo(projectId, projectReferenceInformation with
        {
            OutputPaths = projectReferenceInformation.OutputPaths.Add(outputPath)
        });

        projectUpdateState = projectUpdateState.WithProjectOutputPath(outputPath, projectId);

        var projectsForOutputPath = projectUpdateState.ProjectsByOutputPath[outputPath];
        var distinctProjectsForOutputPath = projectsForOutputPath.Distinct().ToList();

        // If we have exactly one, then we're definitely good to convert
        if (projectsForOutputPath.Count() == 1)
        {
            projectUpdateState = ConvertMetadataReferencesToProjectReferences_NoLock(solutionChanges, projectId, outputPath, projectUpdateState);
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
                    projectUpdateState = ConvertProjectReferencesToMetadataReferences_NoLock(solutionChanges, otherProjectId, outputPath, projectUpdateState, solutionServices);
                }
            }
        }

        return projectUpdateState;
    }

    /// <summary>
    /// Attempts to convert all metadata references to <paramref name="outputPath"/> to a project reference to <paramref
    /// name="projectIdToReference"/>.
    /// </summary>
    /// <param name="projectIdToReference">The <see cref="ProjectId"/> of the project that could be referenced in place
    /// of the output path.</param>
    /// <param name="outputPath">The output path to replace.</param>
    [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/31306",
        Constraint = "Avoid calling " + nameof(CodeAnalysis.Solution.GetProject) + " to avoid realizing all projects.")]
    private static ProjectUpdateState ConvertMetadataReferencesToProjectReferences_NoLock(
        SolutionChangeAccumulator solutionChanges,
        ProjectId projectIdToReference,
        string outputPath,
        ProjectUpdateState projectUpdateState)
    {
        foreach (var projectIdToRetarget in solutionChanges.Solution.ProjectIds)
        {
            if (CanConvertMetadataReferenceToProjectReference(solutionChanges.Solution, projectIdToRetarget, referencedProjectId: projectIdToReference))
            {
                // PERF: call GetRequiredProjectState instead of GetRequiredProject, otherwise creating a new project
                // might force all Project instances to get created.
                var projectState = solutionChanges.Solution.GetRequiredProjectState(projectIdToRetarget);
                foreach (var reference in projectState.MetadataReferences.OfType<PortableExecutableReference>())
                {
                    if (string.Equals(reference.FilePath, outputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        projectUpdateState = projectUpdateState.WithIncrementalMetadataReferenceRemoved(reference);

                        var projectReference = new ProjectReference(projectIdToReference, reference.Properties.Aliases, reference.Properties.EmbedInteropTypes);
                        var newSolution = solutionChanges.Solution
                            .RemoveMetadataReference(projectIdToRetarget, reference)
                            .AddProjectReference(projectIdToRetarget, projectReference);

                        solutionChanges.UpdateSolutionForProjectAction(projectIdToRetarget, newSolution);

                        projectUpdateState = GetReferenceInformation(projectIdToRetarget, projectUpdateState, out var projectInfo);
                        projectUpdateState = projectUpdateState.WithProjectReferenceInfo(projectIdToRetarget,
                            projectInfo.WithConvertedProjectReference(reference.FilePath!, projectReference));

                        // We have converted one, but you could have more than one reference with different aliases that
                        // we need to convert, so we'll keep going
                    }
                }
            }
        }

        return projectUpdateState;
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
    private static ProjectUpdateState ConvertProjectReferencesToMetadataReferences_NoLock(
        SolutionChangeAccumulator solutionChanges,
        ProjectId projectId,
        string outputPath,
        ProjectUpdateState projectUpdateState,
        SolutionServices solutionServices)
    {
        foreach (var projectIdToRetarget in solutionChanges.Solution.ProjectIds)
        {
            projectUpdateState = GetReferenceInformation(projectIdToRetarget, projectUpdateState, out var referenceInfo);

            // Update ConvertedProjectReferences in place to avoid duplicate list allocations
            for (var i = 0; i < referenceInfo.ConvertedProjectReferences.Count(); i++)
            {
                var convertedReference = referenceInfo.ConvertedProjectReferences[i];

                if (string.Equals(convertedReference.path, outputPath, StringComparison.OrdinalIgnoreCase) &&
                    convertedReference.ProjectReference.ProjectId == projectId)
                {
                    var metadataReference = CreateMetadataReference_NoLock(
                        convertedReference.path,
                        new MetadataReferenceProperties(
                            aliases: convertedReference.ProjectReference.Aliases,
                            embedInteropTypes: convertedReference.ProjectReference.EmbedInteropTypes),
                        solutionServices);
                    projectUpdateState = projectUpdateState.WithIncrementalMetadataReferenceAdded(metadataReference);

                    var newSolution = solutionChanges.Solution.RemoveProjectReference(projectIdToRetarget, convertedReference.ProjectReference)
                                                              .AddMetadataReference(projectIdToRetarget, metadataReference);

                    solutionChanges.UpdateSolutionForProjectAction(projectIdToRetarget, newSolution);

                    referenceInfo = referenceInfo with
                    {
                        ConvertedProjectReferences = referenceInfo.ConvertedProjectReferences.RemoveAt(i)
                    };
                    projectUpdateState = projectUpdateState.WithProjectReferenceInfo(projectIdToRetarget, referenceInfo);

                    // We have converted one, but you could have more than one reference with different aliases
                    // that we need to convert, so we'll keep going. Make sure to decrement the index so we don't
                    // skip any items.
                    i--;
                }
            }
        }

        return projectUpdateState;
    }

    /// <summary>
    /// Converts a metadata reference to a project reference if possible.
    /// This must be safe to run multiple times for the same reference as it is called
    /// during a workspace update (which will attempt to apply the update multiple times).
    /// </summary>
    public static ProjectUpdateState TryCreateConvertedProjectReference_NoLock(
        ProjectId referencingProject,
        string path,
        MetadataReferenceProperties properties,
        ProjectUpdateState projectUpdateState,
        Solution currentSolution,
        out ProjectReference? projectReference)
    {
        if (projectUpdateState.ProjectsByOutputPath.TryGetValue(path, out var ids) && ids.Distinct().Count() == 1)
        {
            var projectIdToReference = ids.First();

            if (CanConvertMetadataReferenceToProjectReference(currentSolution, referencingProject, projectIdToReference))
            {
                projectReference = new ProjectReference(
                    projectIdToReference,
                    aliases: properties.Aliases,
                    embedInteropTypes: properties.EmbedInteropTypes);

                projectUpdateState = GetReferenceInformation(referencingProject, projectUpdateState, out var projectReferenceInfo);
                projectUpdateState = projectUpdateState.WithProjectReferenceInfo(referencingProject, projectReferenceInfo.WithConvertedProjectReference(path, projectReference));
                return projectUpdateState;
            }
            else
            {
                projectReference = null;
                return projectUpdateState;
            }
        }
        else
        {
            projectReference = null;
            return projectUpdateState;
        }
    }

    /// <summary>
    /// Tries to convert a metadata reference to remove to a project reference.
    /// </summary>
    public static ProjectUpdateState TryRemoveConvertedProjectReference_NoLock(
        ProjectId referencingProject,
        string path,
        MetadataReferenceProperties properties,
        ProjectUpdateState projectUpdateState,
        out ProjectReference? projectReference)
    {
        projectUpdateState = GetReferenceInformation(referencingProject, projectUpdateState, out var projectReferenceInformation);
        foreach (var convertedProject in projectReferenceInformation.ConvertedProjectReferences)
        {
            if (convertedProject.path == path &&
                convertedProject.ProjectReference.EmbedInteropTypes == properties.EmbedInteropTypes &&
                convertedProject.ProjectReference.Aliases.SequenceEqual(properties.Aliases))
            {
                projectUpdateState = projectUpdateState.WithProjectReferenceInfo(referencingProject, projectReferenceInformation with
                {
                    ConvertedProjectReferences = projectReferenceInformation.ConvertedProjectReferences.Remove(convertedProject)
                });
                projectReference = convertedProject.ProjectReference;
                return projectUpdateState;
            }
        }

        projectReference = null;
        return projectUpdateState;
    }

    public static ProjectUpdateState RemoveProjectOutputPath_NoLock(
        SolutionChangeAccumulator solutionChanges,
        ProjectId projectId,
        string outputPath,
        ProjectUpdateState projectUpdateState,
        bool solutionClosing,
        SolutionServices solutionServices)
    {
        projectUpdateState = GetReferenceInformation(projectId, projectUpdateState, out var projectReferenceInformation);
        if (!projectReferenceInformation.OutputPaths.Contains(outputPath))
        {
            throw new ArgumentException($"Project does not contain output path '{outputPath}'", nameof(outputPath));
        }

        projectUpdateState = projectUpdateState.WithProjectReferenceInfo(projectId, projectReferenceInformation with
        {
            OutputPaths = projectReferenceInformation.OutputPaths.Remove(outputPath)
        });

        projectUpdateState = projectUpdateState.RemoveProjectOutputPath(outputPath, projectId);

        // When a project is closed, we may need to convert project references to metadata references (or vice
        // versa). Failure to convert the references could leave a project in the workspace with a project
        // reference to a project which is not open.
        //
        // For the specific case where the entire solution is closing, we do not need to update the state for
        // remaining projects as each project closes, because we know those projects will be closed without
        // further use. Avoiding reference conversion when the solution is closing improves performance for both
        // IDE close scenarios and solution reload scenarios that occur after complex branch switches.
        if (!solutionClosing)
        {
            if (projectUpdateState.ProjectsByOutputPath.TryGetValue(outputPath, out var remainingProjectsForOutputPath))
            {
                var distinctRemainingProjects = remainingProjectsForOutputPath.Distinct();
                if (distinctRemainingProjects.Count() == 1)
                {
                    // We had more than one project outputting to the same path. Now we're back down to one
                    // so we can reference that one again
                    projectUpdateState = ConvertMetadataReferencesToProjectReferences_NoLock(solutionChanges, distinctRemainingProjects.Single(), outputPath, projectUpdateState);
                }
            }
            else
            {
                // No projects left, we need to convert back to metadata references
                projectUpdateState = ConvertProjectReferencesToMetadataReferences_NoLock(solutionChanges, projectId, outputPath, projectUpdateState, solutionServices);
            }
        }

        return projectUpdateState;
    }

    /// <summary>
    /// Gets or creates a PortableExecutableReference instance for the given file path and properties.
    /// Calls to this are expected to be serialized by the caller.
    /// </summary>
    public static PortableExecutableReference CreateMetadataReference_NoLock(
        string fullFilePath, MetadataReferenceProperties properties, SolutionServices solutionServices)
    {
        return solutionServices.GetRequiredService<IMetadataService>().GetReference(fullFilePath, properties);
    }

    private void StartRefreshingMetadataReferencesForFile(object? sender, string fullFilePath)
        => StartRefreshingReferencesForFile(
            fullFilePath,
            getReferences: static project => project.MetadataReferences.OfType<PortableExecutableReference>(),
            getFilePath: static reference => reference.FilePath!,
            createNewReference: static (@this, reference) => CreateMetadataReference_NoLock(reference.FilePath!, reference.Properties, @this.SolutionServices),
            update: static (solution, projectId, projectUpdateState, oldReference, newReference) =>
            {
                var newSolution = solution
                    .RemoveMetadataReference(projectId, oldReference)
                    .AddMetadataReference(projectId, newReference);
                var newProjectUpdateState = projectUpdateState
                    .WithIncrementalMetadataReferenceRemoved(oldReference)
                    .WithIncrementalMetadataReferenceAdded(newReference);
                return (newSolution, newProjectUpdateState);
            });

    private void StartRefreshingAnalyzerReferenceForFile(object? sender, string fullFilePath)
        => StartRefreshingReferencesForFile(
            fullFilePath,
            getReferences: static project => project.AnalyzerReferences.Select(r => r.FullPath!),
            getFilePath: static filePath => filePath,
            createNewReference: static (@this, filePath) => filePath,
            update: static (solution, projectId, projectUpdateState, oldAnalyzerFilePath, newAnalyzerFilePath) =>
            {
                // Note: we're passing in the same path for the analyzers to remove/add.  That's exactly the intent
                // here.  We're updating an existing analyzer in place. The call to UpdateProjectAnalyzerReferences will
                // preserve all the other analyzers (with a different path), remove the one with this path, make a new
                // analyzer for this path, and then created an isolated ALC to load them all in.
                Contract.ThrowIfTrue(oldAnalyzerFilePath != newAnalyzerFilePath);

                var (newSolution, newProjectUpdateState) = ProjectSystemProject.UpdateProjectAnalyzerReferences(
                    solution, projectId, projectUpdateState, [oldAnalyzerFilePath], [newAnalyzerFilePath]);
                return (newSolution, newProjectUpdateState);
            });

    /// <summary>
    /// Core helper that handles refreshing the references we have for a particular <see
    /// cref="PortableExecutableReference"/> or <see cref="AnalyzerFileReference"/>.
    /// </summary>
    private void StartRefreshingReferencesForFile<TReference>(
        string fullFilePath,
        Func<Project, IEnumerable<TReference>> getReferences,
        Func<TReference, string> getFilePath,
        Func<ProjectSystemProjectFactory, TReference, TReference> createNewReference,
        Func<Solution, ProjectId, ProjectUpdateState, TReference, TReference, (Solution newSolution, ProjectUpdateState newProjectUpdateState)> update)
        where TReference : class
    {
        var asyncToken = WorkspaceListener.BeginAsyncOperation(nameof(StartRefreshingReferencesForFile));

        var task = StartRefreshingReferencesForFileAsync();
        task.CompletesAsyncOperation(asyncToken);

        return;

        async Task StartRefreshingReferencesForFileAsync()
        {
            await ApplyBatchChangeToWorkspaceAsync((solutionChanges, projectUpdateState) =>
            {
                // Access the current update state under the workspace lock.
                foreach (var project in Workspace.CurrentSolution.Projects)
                {
                    // Loop to find each reference with the given path. It's possible that there might be multiple
                    // references of the same path; the project system could conceivably add the same reference multiple
                    // times but with different aliases. It's also possible we might not find the path at all: when we
                    // receive the file changed event, we aren't checking if the file is still in the workspace at that
                    // time; it's possible it might have already been removed.
                    foreach (var reference in getReferences(project))
                    {
                        if (getFilePath(reference) == fullFilePath)
                        {
                            var newSolution = solutionChanges.Solution;
                            (newSolution, projectUpdateState) = update(
                                newSolution, project.Id, projectUpdateState, reference, createNewReference(this, reference));

                            solutionChanges.UpdateSolutionForProjectAction(project.Id, newSolution);
                        }
                    }
                }

                return projectUpdateState;
            }, onAfterUpdateAlways: null).ConfigureAwait(false);
        }
    }

    internal Task RaiseOnDocumentsAddedMaybeAsync(bool useAsync, ImmutableArray<string> filePaths)
    {
        return _onDocumentsAddedMaybeAsync(useAsync, filePaths);
    }
}
